$workspaceDir = 'C:\Users\MinhHQ\.gemini\antigravity\scratch\onesecurity'
$toolsDir = Join-Path $workspaceDir "tools"
$goBin = Join-Path $toolsDir "go\bin"
$nodeBin = Join-Path $toolsDir "node"

$env:GOROOT = Join-Path $toolsDir "go"
$env:PATH = "$goBin;$nodeBin;$env:PATH"

Write-Host "Environment configured for this session." -ForegroundColor Green
Write-Host "Go: " -NoNewline; go version
Write-Host "Node: " -NoNewline; node -v
Write-Host "NPM: " -NoNewline; npm -v
