# Carga opcional de dados de demonstração (seed-data.sql). Rode depois das migrations; é idempotente.
# Uso: .\k8s\scripts\run-seed.ps1
param(
    [string]$Namespace = "oficina",
    [int]$TimeoutSeconds = 120
)

$ErrorActionPreference = "Stop"
$root = Resolve-Path (Join-Path $PSScriptRoot "../..")
$job = "oficina-seed"

kubectl -n $Namespace delete job $job --ignore-not-found --wait=true | Out-Null

# O arquivo fica fora da raiz do Kustomize, então o ConfigMap é criado direto do arquivo.
kubectl -n $Namespace create configmap oficina-seed --from-file="seed-data.sql=$(Join-Path $root 'seed-data.sql')" --dry-run=client -o yaml | kubectl apply -f - | Out-Null
if ($LASTEXITCODE -ne 0) { throw "Falha ao criar o ConfigMap oficina-seed." }

kubectl apply -k (Join-Path $root "k8s/overlays/localstack/seed") | Out-Null
if ($LASTEXITCODE -ne 0) { throw "Falha ao aplicar o Job de seed." }

$deadline = (Get-Date).AddSeconds($TimeoutSeconds)
$result = $null
$ErrorActionPreference = "Continue"
while ((Get-Date) -lt $deadline) {
    $status = kubectl -n $Namespace get job $job -o "jsonpath={.status.conditions[*].type}" 2>$null
    if ($status -match "Complete") { $result = "complete"; break }
    if ($status -match "Failed") { $result = "failed"; break }
    Start-Sleep -Seconds 3
}
$ErrorActionPreference = "Stop"

kubectl -n $Namespace logs "job/$job" 2>&1 | ForEach-Object { Write-Host $_ }
if ($result -ne "complete") { Write-Error "Seed não concluído ($result)."; exit 1 }
Write-Host "Seed aplicado."
