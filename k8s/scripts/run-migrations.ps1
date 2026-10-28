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

kubectl -n $Namespace logs "job/$job" --all-containers=true 2>&1 | ForEach-Object { Write-Host $_ }

switch ($result) {
    "complete" { Write-Host "Migrations aplicadas com sucesso."; return }
    "failed" { Write-Error "A migração falhou (logs acima). O deploy deve ser interrompido."; exit 1 }
    default { Write-Error "Tempo esgotado ($TimeoutSeconds s) esperando o Job de migrations."; exit 1 }
}
