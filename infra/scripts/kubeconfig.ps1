# Gera um kubeconfig para o cluster EKS criado pelo LocalStack (k3d).
# Uso: . .\infra\scripts\kubeconfig.ps1   (com ponto, para que KUBECONFIG valha na sessão atual)
param(
    [string]$ClusterName = "oficina-local",
    # GetFolderPath funciona no Windows e no Linux (no runner do GitHub, $env:USERPROFILE não existe).
    [string]$OutFile = (Join-Path ([Environment]::GetFolderPath("UserProfile")) "kubeconfig-oficina")
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
    # O k3s nomeia cluster, usuário e contexto como "default"; ferramentas como o Headlamp identificam
    # o cluster por esse nome e confundem este com outros (ou com uma execução anterior, de outra porta).
    ForEach-Object { $_ -replace "^(\s*-?\s*(name|cluster|user|current-context):\s*)default\s*$", "`$1$ClusterName" } |
    Set-Content -Encoding ascii $OutFile

$env:KUBECONFIG = $OutFile
Write-Host "KUBECONFIG=$OutFile"
