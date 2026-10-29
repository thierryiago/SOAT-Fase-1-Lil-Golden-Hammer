# Cria (ou atualiza) o Secret "dockerhub-pull", usado pelo cluster para puxar as imagens do repositório
# privado do Docker Hub. Nenhum valor é versionado nem impresso.
# Uso:
#   $env:DOCKERHUB_USERNAME = "thierryiago27"
#   $env:DOCKERHUB_TOKEN    = "<access token de leitura do Docker Hub>"
#   .\k8s\scripts\create-pull-secret.ps1
param(
    [string]$Namespace = "oficina",
    [string]$Registry = "https://index.docker.io/v1/"
)

$ErrorActionPreference = "Stop"
$root = Resolve-Path (Join-Path $PSScriptRoot "../..")
$secretName = "dockerhub-pull"

$user = $env:DOCKERHUB_USERNAME
$token = $env:DOCKERHUB_TOKEN
if (-not $user -or -not $token) {
    throw "Defina DOCKERHUB_USERNAME e DOCKERHUB_TOKEN (Access Token do Docker Hub, escopo de leitura basta)."
}

kubectl apply -f (Join-Path $root "k8s/base/namespace.yaml") | Out-Null
if ($LASTEXITCODE -ne 0) { throw "Falha ao aplicar o Namespace." }

# Monta o .dockerconfigjson num arquivo temporário: --docker-password exporia o token na lista de processos.
$auth = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes("${user}:${token}"))
$config = @{ auths = @{ $Registry = @{ username = $user; password = $token; auth = $auth } } } | ConvertTo-Json -Depth 4 -Compress
$tmp = Join-Path ([IO.Path]::GetTempPath()) ("dockerhub-pull-" + [guid]::NewGuid() + ".json")
try {
    [IO.File]::WriteAllText($tmp, $config, (New-Object Text.UTF8Encoding $false))
    kubectl -n $Namespace create secret generic $secretName --type=kubernetes.io/dockerconfigjson `
        "--from-file=.dockerconfigjson=$tmp" --dry-run=client -o yaml | kubectl apply -f - | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Falha ao aplicar o Secret $secretName." }
}
finally {
    Remove-Item -Force $tmp -ErrorAction SilentlyContinue
}

Write-Host "Secret '$secretName' aplicado em '$Namespace' para o registry $Registry."
