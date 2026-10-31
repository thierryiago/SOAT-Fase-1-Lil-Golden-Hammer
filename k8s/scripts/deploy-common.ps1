# Funções compartilhadas pelos scripts de deploy. Não é executado direto: carregue com
#   . (Join-Path $PSScriptRoot "deploy-common.ps1")

$script:ImageRepository = "docker.io/thierryiago27/fiap-oficina-image-docker-hub"

# As funções que consultam o cluster usam ErrorActionPreference Continue no próprio escopo: no Windows
# PowerShell 5.1, stderr redirecionado de um comando nativo vira exceção sob "Stop".

# Estados em que o kubelet fica tentando para sempre sem que o Job/Deployment chegue a falhar.
$script:BlockingWaitingReasons = @(
    "ImagePullBackOff",
    "ErrImageNeverPull",
    "InvalidImageName",
    "CreateContainerConfigError"
)

function Assert-RequiredSecrets {
    param(
        [string]$Namespace,
        [hashtable]$SecretsWithCreator
    )

    $ErrorActionPreference = "Continue"
    $missing = @()
    foreach ($secret in $SecretsWithCreator.Keys) {
        kubectl -n $Namespace get secret $secret -o name 2>$null | Out-Null
        if ($LASTEXITCODE -ne 0) { $missing += "  - $secret (crie com $($SecretsWithCreator[$secret]))" }
    }
    if ($missing.Count -gt 0) {
        throw "Secrets ausentes no namespace '$Namespace':`n$($missing -join "`n")"
    }
}

# Aplica um overlay de k8s/ trocando só a tag da imagem para "<Component>-<ImageTag>", sem alterar os arquivos
# versionados. O Kustomize não aceita caminho absoluto em resources, por isso o overlay temporário fica
# dentro de k8s/ (ignorado pelo git) e aponta para o overlay real por caminho relativo.
# O imagePullPolicy Always vem do overlay: "latest" é mutável e o nó não pode usar a cópia do cache.
function Invoke-OverlayApplyWithImageTag {
    param(
        [string]$RepositoryRoot,
        [string]$OverlayPathFromK8s,
        [string]$Component,
        [string]$ImageTag
    )

    $image = "${script:ImageRepository}:$Component-$ImageTag"
    Write-Host "Imagem: $image"

    $overrideDir = Join-Path $RepositoryRoot "k8s/.image-override-$([guid]::NewGuid().ToString('N'))"
    New-Item -ItemType Directory -Path $overrideDir | Out-Null
    try {
        $kustomization = @"
apiVersion: kustomize.config.k8s.io/v1beta1
kind: Kustomization
resources:
  - ../$OverlayPathFromK8s
images:
  - name: ${script:ImageRepository}
    newTag: $Component-$ImageTag
"@
        # Sem BOM: o Windows PowerShell 5.1 grava BOM com -Encoding utf8.
        [IO.File]::WriteAllText((Join-Path $overrideDir "kustomization.yaml"), $kustomization, (New-Object Text.UTF8Encoding $false))
        kubectl apply -k $overrideDir | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "Falha ao aplicar $OverlayPathFromK8s com a imagem $image." }
    }
    finally {
        Remove-Item -Recurse -Force $overrideDir -ErrorAction SilentlyContinue
    }
}

# Retorna "<pod>: <motivo> - <mensagem>" do primeiro container travado num estado sem saída, ou $null.
function Get-BlockedPodReason {
    param(
        [string]$Namespace,
        [string]$Selector
    )

    $ErrorActionPreference = "Continue"
    $pods = kubectl -n $Namespace get pods -l $Selector -o json 2>$null | ConvertFrom-Json
    if (-not $pods) { return $null }
    foreach ($pod in $pods.items) {
        foreach ($container in @($pod.status.containerStatuses)) {
            $waiting = $container.state.waiting
            if ($waiting -and ($script:BlockingWaitingReasons -contains $waiting.reason)) {
                return "$($pod.metadata.name): $($waiting.reason) - $($waiting.message)"
            }
        }
    }
    return $null
}

function Write-PodDiagnostics {
    param(
        [string]$Namespace,
        [string]$Selector
    )

    $ErrorActionPreference = "Continue"
    kubectl -n $Namespace get pods -l $Selector 2>&1 | ForEach-Object { Write-Host $_ }
    kubectl -n $Namespace get events --field-selector "involvedObject.kind=Pod" --sort-by=.lastTimestamp 2>&1 |
        Select-Object -Last 8 | ForEach-Object { Write-Host $_ }
}
