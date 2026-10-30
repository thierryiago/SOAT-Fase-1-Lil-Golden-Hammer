# Roda o teste de ponta a ponta (k6) dentro do cluster e grava o relatório HTML e um resumo em Markdown.
# Sai com código != 0 se algum passo falhar, mas só depois de gravar o relatório.
# Uso: .\k8s\e2e\run-e2e.ps1 [-OutDir <pasta>]        (com KUBECONFIG apontando para o cluster)
# No GitHub Actions, o resumo também vai para $GITHUB_STEP_SUMMARY.
param(
    [string]$Namespace = "oficina",
    [string]$OutDir = (Join-Path ([IO.Path]::GetTempPath()) "oficina-e2e"),
    [int]$TimeoutSeconds = 300
)

$ErrorActionPreference = "Stop"
$dir = $PSScriptRoot
$job = "oficina-e2e"

kubectl -n $Namespace delete job $job --ignore-not-found --wait=true | Out-Null
if ($LASTEXITCODE -ne 0) { throw "Falha ao remover o Job anterior." }

kubectl -n $Namespace create configmap oficina-e2e --from-file="oficina-e2e.js=$(Join-Path $dir 'oficina-e2e.js')" --dry-run=client -o yaml | kubectl apply -f - | Out-Null
if ($LASTEXITCODE -ne 0) { throw "Falha ao criar o ConfigMap oficina-e2e." }

# Contexto do relatório: imagem em execução e, no Actions, o link da execução.
$apiImage = kubectl -n $Namespace get deployment oficina-api -o "jsonpath={.spec.template.spec.containers[0].image}"
$runUrl = ""
if ($env:GITHUB_RUN_ID) { $runUrl = "$env:GITHUB_SERVER_URL/$env:GITHUB_REPOSITORY/actions/runs/$env:GITHUB_RUN_ID" }

$manifest = Get-Content (Join-Path $dir "job.yaml") -Raw
foreach ($e in @{ API_IMAGE = "$apiImage"; RUN_URL = $runUrl }.GetEnumerator()) {
    $manifest = $manifest -replace "(name: $($e.Key)\s+value: )""[^""]*""", "`$1""$($e.Value)"""
}
$manifest | kubectl apply -f - | Out-Null
if ($LASTEXITCODE -ne 0) { throw "Falha ao aplicar o Job de ponta a ponta." }
Write-Host "Teste de ponta a ponta iniciado (imagem $apiImage)."

# Mesmo esquema do run-migrations.ps1: o "kubectl wait" sozinho só detecta falha pelo timeout.
$deadline = (Get-Date).AddSeconds($TimeoutSeconds)
$result = $null
$ErrorActionPreference = "Continue"
while ((Get-Date) -lt $deadline) {
    $status = kubectl -n $Namespace get job $job -o "jsonpath={.status.conditions[*].type}" 2>$null
    if ($status -match "Complete") { $result = "complete"; break }
    if ($status -match "Failed") { $result = "failed"; break }
    Start-Sleep -Seconds 3
}

$logs = @(kubectl -n $Namespace logs "job/$job" 2>&1 | ForEach-Object { "$_" })
$ErrorActionPreference = "Stop"

$begin = [array]::IndexOf($logs, "@@OFICINA-E2E-BEGIN@@")
$end = [array]::IndexOf($logs, "@@OFICINA-E2E-END@@")
if ($begin -lt 0 -or $end -le $begin) {
    $logs | ForEach-Object { Write-Host $_ }
    kubectl -n $Namespace get pods -l "job-name=$job" 2>&1 | ForEach-Object { Write-Host $_ }
    [Console]::Error.WriteLine("Relatório não encontrado na saída do Job (resultado: $result).")
    exit 1
}

# Logs do k6 fora do bloco do relatório (avisos, erros do script) continuam visíveis.
if ($begin -gt 0) { $logs[0..($begin - 1)] | Where-Object { $_ } | ForEach-Object { Write-Host $_ } }

$encoded = ($logs[($begin + 1)..($end - 1)] -join "").Trim()
$payload = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($encoded)) | ConvertFrom-Json

New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
$utf8 = New-Object Text.UTF8Encoding $false
$htmlPath = Join-Path $OutDir "relatorio-e2e.html"
$markdownPath = Join-Path $OutDir "resumo-e2e.md"
[IO.File]::WriteAllText($htmlPath, $payload.html, $utf8)
[IO.File]::WriteAllText($markdownPath, $payload.markdown, $utf8)
if ($env:GITHUB_STEP_SUMMARY) { [IO.File]::AppendAllText($env:GITHUB_STEP_SUMMARY, $payload.markdown + "`n", $utf8) }

Write-Host $payload.markdown
Write-Host ""
Write-Host "Relatório: $htmlPath"

if ($result -eq "complete") { exit 0 }
if ($result -eq "failed") { [Console]::Error.WriteLine("O teste de ponta a ponta falhou (detalhes no relatório)."); exit 1 }
[Console]::Error.WriteLine("Tempo esgotado ($TimeoutSeconds s) esperando o Job de ponta a ponta."); exit 1
