# Roda o teste de carga (k6) dentro do cluster e mostra o resultado. Para ver o HPA reagir, abra antes
# em outros terminais:  kubectl -n oficina get hpa oficina-api -w   e   kubectl -n oficina get pods -w
# Uso: .\k8s\loadtest\run-loadtest.ps1 [-TargetVus 30] [-RampUp 1m] [-Hold 3m] [-RampDown 30s]
param(
    [string]$Namespace = "oficina",
    [int]$TargetVus = 30,
    [string]$RampUp = "1m",
    [string]$Hold = "3m",
    [string]$RampDown = "30s"
)

$ErrorActionPreference = "Stop"
$dir = $PSScriptRoot
$job = "oficina-loadtest"

kubectl -n $Namespace delete job $job --ignore-not-found --wait=true | Out-Null

kubectl -n $Namespace create configmap oficina-loadtest --from-file="oficina.js=$(Join-Path $dir 'oficina.js')" --dry-run=client -o yaml | kubectl apply -f - | Out-Null
if ($LASTEXITCODE -ne 0) { throw "Falha ao criar o ConfigMap oficina-loadtest." }

# Valores do teste entram no manifest antes de aplicar (o arquivo versionado guarda os padrões).
$manifest = Get-Content (Join-Path $dir "job.yaml") -Raw
foreach ($e in @{ TARGET_VUS = "$TargetVus"; RAMP_UP = $RampUp; HOLD = $Hold; RAMP_DOWN = $RampDown }.GetEnumerator()) {
    $manifest = $manifest -replace "(name: $($e.Key)\s+value: )""[^""]*""", "`$1""$($e.Value)"""
}
$manifest | kubectl apply -f - | Out-Null
if ($LASTEXITCODE -ne 0) { throw "Falha ao aplicar o Job de carga." }

Write-Host "Teste de carga iniciado (VUs=$TargetVus, subida=$RampUp, patamar=$Hold, descida=$RampDown)."
kubectl -n $Namespace wait --for=condition=ready pod -l job-name=$job --timeout=180s | Out-Null
kubectl -n $Namespace logs -f "job/$job"
$ErrorActionPreference = "Continue"
$status = kubectl -n $Namespace get job $job -o "jsonpath={.status.conditions[*].type}"
if ($status -notmatch "Complete") { [Console]::Error.WriteLine("O teste de carga falhou ou não passou nos thresholds."); exit 1 }
