# setup_tools.ps1
# Setup local portable tools (Go 1.22.5 & Node.js 20.15.0)

$ErrorActionPreference = "Stop"

$workspaceDir = $PSScriptRoot
if ([string]::IsNullOrEmpty($workspaceDir)) {
    $workspaceDir = Get-Location
}
$toolsDir = Join-Path $workspaceDir "tools"

if (-not (Test-Path $toolsDir)) {
    New-Item -ItemType Directory -Path $toolsDir | Out-Null
    Write-Host "Created tools directory at $toolsDir" -ForegroundColor Green
}

# 1. Download & Setup Go
$goDestDir = Join-Path $toolsDir "go"
if (-not (Test-Path (Join-Path $goDestDir "bin\go.exe"))) {
    $goUrl = "https://go.dev/dl/go1.22.5.windows-amd64.zip"
    $goZip = Join-Path $toolsDir "go.zip"
    
    if (Test-Path $goZip) {
        Remove-Item $goZip -Force -ErrorAction SilentlyContinue
    }
    
    Write-Host "Downloading Go 1.22.5 zip from $goUrl..." -ForegroundColor Cyan
    Invoke-WebRequest -Uri $goUrl -OutFile $goZip -UseBasicParsing
    
    Write-Host "Extracting Go archive..." -ForegroundColor Cyan
    Expand-Archive -Path $goZip -DestinationPath $toolsDir -Force
    
    Remove-Item $goZip -Force -ErrorAction SilentlyContinue
    Write-Host "Go successfully setup at $goDestDir" -ForegroundColor Green
} else {
    Write-Host "Go is already installed locally." -ForegroundColor Yellow
}

# 2. Download & Setup Node.js
$nodeDestDir = Join-Path $toolsDir "node"
if (-not (Test-Path (Join-Path $nodeDestDir "node.exe"))) {
    $nodeUrl = "https://nodejs.org/dist/v20.15.0/node-v20.15.0-win-x64.zip"
    $nodeZip = Join-Path $toolsDir "node.zip"
    
    if (Test-Path $nodeZip) {
        Remove-Item $nodeZip -Force -ErrorAction SilentlyContinue
    }
    
    Write-Host "Downloading Node.js v20.15.0 zip from $nodeUrl..." -ForegroundColor Cyan
    Invoke-WebRequest -Uri $nodeUrl -OutFile $nodeZip -UseBasicParsing
    
    Write-Host "Extracting Node.js archive..." -ForegroundColor Cyan
    Expand-Archive -Path $nodeZip -DestinationPath $toolsDir -Force
    
    # Node zip extracts to "node-v20.15.0-win-x64", rename it to "node"
    $extractedFolder = Join-Path $toolsDir "node-v20.15.0-win-x64"
    if (Test-Path $extractedFolder) {
        if (Test-Path $nodeDestDir) {
            Remove-Item $nodeDestDir -Recurse -Force -ErrorAction SilentlyContinue
        }
        Rename-Item -Path $extractedFolder -NewName "node" -Force
    }
    
    Remove-Item $nodeZip -Force -ErrorAction SilentlyContinue
    Write-Host "Node.js successfully setup at $nodeDestDir" -ForegroundColor Green
} else {
    Write-Host "Node.js is already installed locally." -ForegroundColor Yellow
}

# 3. Create process environment helper
$envScript = @"
`$workspaceDir = '$workspaceDir'
`$toolsDir = Join-Path `$workspaceDir "tools"
`$goBin = Join-Path `$toolsDir "go\bin"
`$nodeBin = Join-Path `$toolsDir "node"

`$env:GOROOT = Join-Path `$toolsDir "go"
`$env:PATH = "`$goBin;`$nodeBin;`$env:PATH"

Write-Host "Environment configured for this session." -ForegroundColor Green
Write-Host "Go: " -NoNewline; go version
Write-Host "Node: " -NoNewline; node -v
Write-Host "NPM: " -NoNewline; npm -v
"@

$envScriptPath = Join-Path $workspaceDir "load_env.ps1"
Set-Content -Path $envScriptPath -Value $envScript -Force
Write-Host "Created environment helper script at $envScriptPath" -ForegroundColor Green

Write-Host "`nTools installation complete! Run '. .\load_env.ps1' to activate the environment in this terminal session." -ForegroundColor Green
