# Exporta os diagramas Mermaid desta pasta para SVG e PNG (usados no PDF de entrega).
# Requer Node.js; o mermaid-cli é baixado pelo npx na primeira execução.
# Os .mmd são a fonte: ao alterar um diagrama, atualize também o bloco correspondente no README.md.
$ErrorActionPreference = "Stop"

foreach ($source in Get-ChildItem -Path $PSScriptRoot -Filter *.mmd) {
    $baseName = Join-Path $PSScriptRoot $source.BaseName
    npx -y @mermaid-js/mermaid-cli@11 -i $source.FullName -o "$baseName.svg" -b white
    if ($LASTEXITCODE -ne 0) { throw "Falha ao exportar $($source.Name) para SVG." }
    npx -y @mermaid-js/mermaid-cli@11 -i $source.FullName -o "$baseName.png" -b white -s 2
    if ($LASTEXITCODE -ne 0) { throw "Falha ao exportar $($source.Name) para PNG." }
}
