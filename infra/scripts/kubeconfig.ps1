# Gera um kubeconfig para o cluster EKS criado pelo LocalStack (k3d).
# Uso: . .\infra\scripts\kubeconfig.ps1   (com ponto, para que KUBECONFIG valha na sessão atual)
param(
    [string]$ClusterName = "oficina-local",
    [string]$OutFile = (Join-Path $env:USERPROFILE "kubeconfig-oficina")
)

$server = docker ps --filter "name=k3d-$ClusterName.*server-0" --format "{{.Names}}" | Select-Object -First 1
$lb = docker ps --filter "name=k3d-$ClusterName.*serverlb" --format "{{.Names}}" | Select-Object -First 1

if (-not $server -or -not $lb) {
    Write-Error "Cluster '$ClusterName' não encontrado. Rode 'terraform -chdir=infra apply' e confira se o LocalStack tem o socket do Docker montado."
    return
}

$port = (docker port $lb 6443/tcp | Select-Object -First 1).Split(":")[-1]

docker exec $server cat /etc/rancher/k3s/k3s.yaml |
    ForEach-Object { $_ -replace "server: .*", "server: https://127.0.0.1:$port" } |
    Set-Content -Encoding ascii $OutFile

$env:KUBECONFIG = $OutFile
Write-Host "KUBECONFIG=$OutFile"
