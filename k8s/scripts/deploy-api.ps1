# Aplica o overlay da API (API, Mailpit, ConfigMap, Service e HPA) com a tag de imagem escolhida e espera
# o rollout. Sai com código != 0 se um pod travar (ex.: imagem sem permissão de pull) ou estourar o tempo.
# Uso: .\k8s\scripts\deploy-api.ps1 [-ImageTag <sha7|latest>]   (rode depois do run-migrations.ps1)
#   -ImageTag: tag publicada pelo publish-images.yml, sem o prefixo "api-". Padrão: latest (última da main).
param(
    [string]$Namespace = "oficina",
    [ValidatePattern('^[A-Za-z0-9._-]{1,100}$')]
    [string]$ImageTag = "latest",
    [int]$TimeoutSeconds = 300
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "deploy-common.ps1")
$root = Resolve-Path (Join-Path $PSScriptRoot "../..")
$deployment = "oficina-api"

Assert-RequiredSecrets -Namespace $Namespace -SecretsWithCreator @{
    "oficina-secrets" = "k8s/scripts/create-secrets.ps1"
    "dockerhub-pull"  = "k8s/scripts/create-pull-secret.ps1"
}

Invoke-OverlayApplyWithImageTag -RepositoryRoot $root -OverlayPathFromK8s "overlays/localstack" `
    -Component "api" -ImageTag $ImageTag

# Com "latest" a spec não muda entre publicações, então o apply sozinho não troca os pods.
if ($ImageTag -eq "latest") {
    kubectl -n $Namespace rollout restart "deployment/$deployment" | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Falha ao reiniciar o Deployment $deployment." }
}

function Wait-DeploymentRollout {
    param([string]$Name, [string]$Selector)

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    $ErrorActionPreference = "Continue"
    while ((Get-Date) -lt $deadline) {
        kubectl -n $Namespace rollout status "deployment/$Name" --timeout=3s 2>$null | Out-Null
        if ($LASTEXITCODE -eq 0) { Write-Host "Deployment $Name pronto."; return }
        $blockedReason = Get-BlockedPodReason -Namespace $Namespace -Selector $Selector
        if ($blockedReason) {
            Write-PodDiagnostics -Namespace $Namespace -Selector $Selector
            [Console]::Error.WriteLine("O pod de $Name não consegue iniciar: $blockedReason")
            [Console]::Error.WriteLine("Confira se a tag existe no Docker Hub e se o Secret dockerhub-pull tem um token válido.")
            exit 1
        }
    }
    Write-PodDiagnostics -Namespace $Namespace -Selector $Selector
    [Console]::Error.WriteLine("Tempo esgotado ($TimeoutSeconds s) esperando o rollout de $Name.")
    exit 1
}

Wait-DeploymentRollout -Name $deployment -Selector "app.kubernetes.io/name=oficina-api"
Wait-DeploymentRollout -Name "mailpit" -Selector "app.kubernetes.io/name=mailpit"
