# Cria (ou atualiza) o Secret "oficina-secrets" da API no cluster do LocalStack. Nenhum valor é versionado nem impresso.
# Uso:
#   .\k8s\scripts\create-secrets.ps1
#   .\k8s\scripts\create-secrets.ps1 -DryRun     # mostra as chaves com valores mascarados
#
# Origem de cada chave (a primeira que existir):
#   variável de ambiente -> valor já presente no Secret do cluster -> valor gerado.
# Assim o script é idempotente: rodar de novo não troca chaves já criadas.
#
#   ConnectionStrings__Database   variável de ambiente ou terraform -chdir=infra output -raw db_connection_string
#   Jwt__SigningKey               variável Jwt__SigningKey (>= 32 bytes)
#   Webhooks__BudgetDecisionToken variável Webhooks__BudgetDecisionToken
#   Smtp__Username/Smtp__Password variáveis opcionais (omitidas se ausentes)
param(
    [string]$Namespace = "oficina",
    # Host do RDS visto de dentro do pod. O endpoint do output do Terraform é "localhost" (o do host);
    # dentro do pod isso é o próprio pod. host.k3d.internal é injetado pelo k3d no DNS do cluster e
    # aponta para o host Docker, onde o LocalStack publica a porta do banco (faixa 4510-4559).
    # Só vale para a connection string vinda do Terraform; uma ConnectionStrings__Database definida
    # em variável de ambiente é usada como está (a menos que -DbHost seja passado).
    [string]$DbHost = "host.k3d.internal",
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"
$root = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$secretName = "oficina-secrets"

function New-RandomSecret([int]$bytes = 32) {
    $buffer = New-Object byte[] $bytes
    [System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($buffer)
    return [Convert]::ToBase64String($buffer)
}

# Valor atual no cluster, para manter a idempotência (senha do banco e chaves não mudam a cada execução).
function Get-ExistingValue([string]$key) {
    if ($DryRun) { return $null }
    # Na primeira execução o namespace/Secret ainda não existe; no PowerShell 5.1 o stderr do kubectl
    # viraria exceção com "Stop", então o erro é tratado aqui pelo $LASTEXITCODE.
    $ErrorActionPreference = "Continue"
    $b64 = kubectl -n $Namespace get secret $secretName -o "jsonpath={.data.$key}" 2>$null
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrEmpty($b64)) { return $null }
    return [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($b64))
}

function Resolve-Value([string]$key, [string]$fromEnv, [scriptblock]$generate) {
    if ($fromEnv) { return $fromEnv }
    $existing = Get-ExistingValue $key
    if ($existing) { return $existing }
    if ($generate) { return & $generate }
    return $null
}

$values = [ordered]@{}

# --- Connection string ---
$conn = [Environment]::GetEnvironmentVariable("ConnectionStrings__Database")
$replaceHost = $PSBoundParameters.ContainsKey("DbHost")
if (-not $conn) {
    $conn = terraform "-chdir=$root\infra" output -raw db_connection_string
    if ($LASTEXITCODE -ne 0 -or -not $conn) { throw "Não foi possível ler db_connection_string do Terraform (infra aplicada?)." }
    $replaceHost = $true
}
if ($replaceHost) { $conn = $conn -replace "Host=[^;]*", "Host=$DbHost" }
$values["ConnectionStrings__Database"] = $conn

# --- Chaves da aplicação ---
$jwt = Resolve-Value "Jwt__SigningKey" $env:Jwt__SigningKey { New-RandomSecret 48 }
$hook = Resolve-Value "Webhooks__BudgetDecisionToken" $env:Webhooks__BudgetDecisionToken { New-RandomSecret 32 }
if ([Text.Encoding]::UTF8.GetByteCount($jwt) -lt 32) { throw "Jwt__SigningKey deve ter pelo menos 32 bytes." }
$values["Jwt__SigningKey"] = $jwt
$values["Webhooks__BudgetDecisionToken"] = $hook

# --- SMTP opcional (só com provedor real) ---
if ($env:Smtp__Username) { $values["Smtp__Username"] = $env:Smtp__Username }
if ($env:Smtp__Password) { $values["Smtp__Password"] = $env:Smtp__Password }

if ($DryRun) {
    Write-Host "# DryRun: Secret '$secretName' no namespace '$Namespace' (valores mascarados)"
    foreach ($k in $values.Keys) { Write-Host ("  {0}: ******** ({1} caracteres)" -f $k, $values[$k].Length) }
    return
}

# Garante o Namespace e cria/atualiza o Secret sem passar valores pela linha de comando.
kubectl apply -k "$root\k8s\base" | Out-Null
if ($LASTEXITCODE -ne 0) { throw "Falha ao aplicar o Namespace." }

$args_ = @("-n", $Namespace, "create", "secret", "generic", $secretName, "--dry-run=client", "-o", "yaml")
$tmp = New-Item -ItemType Directory -Path (Join-Path ([IO.Path]::GetTempPath()) ("oficina-secrets-" + [guid]::NewGuid()))
try {
    foreach ($k in $values.Keys) {
        [IO.File]::WriteAllText((Join-Path $tmp $k), [string]$values[$k], (New-Object Text.UTF8Encoding $false))
        $args_ += "--from-file=$k=$(Join-Path $tmp $k)"
    }
    kubectl @args_ | kubectl apply -f - | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Falha ao aplicar o Secret." }
}
finally {
    Remove-Item -Recurse -Force $tmp
}

Write-Host "Secret '$secretName' aplicado em '$Namespace' com as chaves: $($values.Keys -join ', ')"
