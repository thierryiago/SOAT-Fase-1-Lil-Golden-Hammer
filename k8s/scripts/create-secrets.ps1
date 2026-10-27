# Cria (ou atualiza) o Secret "oficina-secrets" da API. Nenhum valor é versionado nem impresso.
# Uso:
#   .\k8s\scripts\create-secrets.ps1 -Overlay aws
#   .\k8s\scripts\create-secrets.ps1 -Overlay local
#   .\k8s\scripts\create-secrets.ps1 -Overlay aws -DryRun     # mostra o YAML com valores mascarados
#
# Origem de cada chave (a primeira que existir):
#   variável de ambiente -> valor já presente no Secret do cluster -> valor gerado (só no overlay local).
# No overlay aws, uma chave sem origem é erro (no CD vêm dos secrets do GitHub como variáveis).
#
#   ConnectionStrings__Database  aws:   terraform -chdir=infra output -raw db_connection_string
#                                local: Host=oficina-db com a senha em POSTGRES_PASSWORD
#   Jwt__SigningKey              variável Jwt__SigningKey (>= 32 bytes)
#   Webhooks__BudgetDecisionToken variável Webhooks__BudgetDecisionToken
#   Smtp__Username/Smtp__Password variáveis opcionais (omitidas se ausentes)
param(
    [Parameter(Mandatory)][ValidateSet("local", "aws")][string]$Overlay,
    [string]$Namespace = "oficina",
    # Substitui o host da connection string (o endpoint do RDS no LocalStack é "localhost" para o host,
    # não para o pod; o valor correto é definido na K8S-5).
    [string]$DbHost,
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
    $b64 = kubectl -n $Namespace get secret $secretName -o "jsonpath={.data.$key}" 2>$null
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrEmpty($b64)) { return $null }
    return [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($b64))
}

function Resolve-Value([string]$key, [string]$fromEnv, [scriptblock]$generate) {
    if ($fromEnv) { return $fromEnv }
    $existing = Get-ExistingValue $key
    if ($existing) { return $existing }
    if ($Overlay -eq "local" -and $generate) { return & $generate }
    return $null
}

$values = [ordered]@{}

# --- Connection string ---
if ($Overlay -eq "aws") {
    $conn = [Environment]::GetEnvironmentVariable("ConnectionStrings__Database")
    if (-not $conn) {
        $conn = terraform "-chdir=$root\infra" output -raw db_connection_string
        if ($LASTEXITCODE -ne 0 -or -not $conn) { throw "Não foi possível ler db_connection_string do Terraform (infra aplicada?)." }
    }
    if ($DbHost) { $conn = $conn -replace "Host=[^;]*", "Host=$DbHost" }
}
else {
    # Senha: variável -> a da connection string já existente (idempotência) -> gerada.
    $pg = $env:POSTGRES_PASSWORD
    if (-not $pg) {
        $existingConn = Get-ExistingValue "ConnectionStrings__Database"
        if ($existingConn -match "Password=([^;]*)") { $pg = $Matches[1] }
    }
    if (-not $pg) { $pg = (New-RandomSecret 18) -replace "[^A-Za-z0-9]", "x" }
    $db = if ($env:POSTGRES_DB) { $env:POSTGRES_DB } else { "oficina" }
    $user = if ($env:POSTGRES_USER) { $env:POSTGRES_USER } else { "oficina" }
    $host_ = if ($DbHost) { $DbHost } else { "oficina-db" }
    $conn = "Host=$host_;Port=5432;Database=$db;Username=$user;Password=$pg"
}
$values["ConnectionStrings__Database"] = $conn

# --- Chaves da aplicação ---
$jwt = Resolve-Value "Jwt__SigningKey" $env:Jwt__SigningKey { New-RandomSecret 48 }
$hook = Resolve-Value "Webhooks__BudgetDecisionToken" $env:Webhooks__BudgetDecisionToken { New-RandomSecret 32 }
if (-not $jwt) { throw "Jwt__SigningKey ausente: defina a variável de ambiente." }
if (-not $hook) { throw "Webhooks__BudgetDecisionToken ausente: defina a variável de ambiente." }
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
