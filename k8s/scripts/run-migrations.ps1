# Recria o Job de migrations e espera o resultado. Sai com código != 0 (e imprime os logs) se a
# migração falhar ou estourar o tempo, para o deploy não seguir para a API.
# Uso: .\k8s\scripts\run-migrations.ps1            (com KUBECONFIG apontando para o cluster)
# Pré-requisitos: Secret oficina-secrets (create-secrets.ps1) e a imagem oficina-migrations nos nós.
param(
    [string]$Namespace = "oficina",
    [int]$TimeoutSeconds = 300
)

$ErrorActionPreference = "Stop"
$root = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$job = "oficina-migrations"

# Spec de Job é imutável: apaga o anterior (se houver) antes de aplicar de novo.
kubectl -n $Namespace delete job $job --ignore-not-found --wait=true | Out-Null
if ($LASTEXITCODE -ne 0) { throw "Falha ao remover o Job anterior." }

kubectl apply -k "$root\k8s\overlays\localstack\migrations" | Out-Null
if ($LASTEXITCODE -ne 0) { throw "Falha ao aplicar o Job de migrations." }

# O "kubectl wait" sozinho só detecta falha pelo timeout; por isso consulta o status do Job.
$deadline = (Get-Date).AddSeconds($TimeoutSeconds)
$result = $null
$ErrorActionPreference = "Continue"
while ((Get-Date) -lt $deadline) {
    $status = kubectl -n $Namespace get job $job -o "jsonpath={.status.succeeded}/{.status.failed}/{.status.conditions[*].type}" 2>$null
    if ($status -match "Complete") { $result = "complete"; break }
    if ($status -match "Failed") { $result = "failed"; break }
    Start-Sleep -Seconds 3
}
$ErrorActionPreference = "Stop"

# Logs só existem se o pod chegou a iniciar; sem eles, mostra o estado e os eventos (ex.: imagem ausente).
$ErrorActionPreference = "Continue"
$logs = kubectl -n $Namespace logs "job/$job" --all-containers=true 2>&1
if ($LASTEXITCODE -eq 0) {
    $logs | ForEach-Object { Write-Host $_ }
}
elseif ($result -ne "complete") {
    kubectl -n $Namespace get pods -l "job-name=$job" 2>&1 | ForEach-Object { Write-Host $_ }
    kubectl -n $Namespace get events --field-selector "involvedObject.kind=Pod" --sort-by=.lastTimestamp 2>&1 |
        Select-Object -Last 8 | ForEach-Object { Write-Host $_ }
}

if ($result -eq "complete") { Write-Host "Migrations aplicadas com sucesso."; exit 0 }
if ($result -eq "failed") { [Console]::Error.WriteLine("A migração falhou (logs acima). O deploy deve ser interrompido."); exit 1 }
[Console]::Error.WriteLine("Tempo esgotado ($TimeoutSeconds s) esperando o Job de migrations."); exit 1
