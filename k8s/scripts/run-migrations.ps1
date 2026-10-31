# Recria o Job de migrations e espera o resultado. Sai com código != 0 (e imprime os logs) se a
# migração falhar ou estourar o tempo, para o deploy não seguir para a API.
# Uso: .\k8s\scripts\run-migrations.ps1 [-ImageTag <sha7|latest>]   (com KUBECONFIG apontando para o cluster)
#   -ImageTag: tag publicada pelo publish-images.yml, sem o prefixo "migrations-". Padrão: latest (última da main).
# Pré-requisitos: Secrets oficina-secrets (create-secrets.ps1) e dockerhub-pull (create-pull-secret.ps1).
param(
    [string]$Namespace = "oficina",
    [ValidatePattern('^[A-Za-z0-9._-]{1,100}$')]
    [string]$ImageTag = "latest",
    [int]$TimeoutSeconds = 300
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "deploy-common.ps1")
$root = Resolve-Path (Join-Path $PSScriptRoot "../..")
$job = "oficina-migrations"

Assert-RequiredSecrets -Namespace $Namespace -SecretsWithCreator @{
    "oficina-secrets" = "k8s/scripts/create-secrets.ps1"
    "dockerhub-pull"  = "k8s/scripts/create-pull-secret.ps1"
}

# Spec de Job é imutável: apaga o anterior (se houver) antes de aplicar de novo.
kubectl -n $Namespace delete job $job --ignore-not-found --wait=true | Out-Null
if ($LASTEXITCODE -ne 0) { throw "Falha ao remover o Job anterior." }

Invoke-OverlayApplyWithImageTag -RepositoryRoot $root -OverlayPathFromK8s "overlays/localstack/migrations" `
    -Component "migrations" -ImageTag $ImageTag

# O "kubectl wait" sozinho só detecta falha pelo timeout; por isso consulta o status do Job. Um pull que
# não sai do ImagePullBackOff nunca falha o Job, então também é tratado como falha.
$deadline = (Get-Date).AddSeconds($TimeoutSeconds)
$result = $null
$blockedReason = $null
$ErrorActionPreference = "Continue"
while ((Get-Date) -lt $deadline) {
    $status = kubectl -n $Namespace get job $job -o "jsonpath={.status.succeeded}/{.status.failed}/{.status.conditions[*].type}" 2>$null
    if ($status -match "Complete") { $result = "complete"; break }
    if ($status -match "Failed") { $result = "failed"; break }
    $blockedReason = Get-BlockedPodReason -Namespace $Namespace -Selector "job-name=$job"
    if ($blockedReason) { $result = "blocked"; break }
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
if ($result -eq "blocked") {
    [Console]::Error.WriteLine("O pod de migrations não consegue iniciar: $blockedReason")
    [Console]::Error.WriteLine("Confira se a tag existe no Docker Hub e se o Secret dockerhub-pull tem um token válido.")
    exit 1
}
if ($result -eq "failed") { [Console]::Error.WriteLine("A migração falhou (logs acima). O deploy deve ser interrompido."); exit 1 }
[Console]::Error.WriteLine("Tempo esgotado ($TimeoutSeconds s) esperando o Job de migrations."); exit 1
