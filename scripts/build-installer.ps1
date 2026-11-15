param(
    [string]$Version = "1.0.0",
    [switch]$FrameworkDependent
)

$ErrorActionPreference = "Stop"

$RepoRoot = Split-Path -Parent $PSScriptRoot
$ProjectPath = Join-Path $RepoRoot "IsoLaunch.App\IsoLaunch.App.csproj"
$PublishDir = Join-Path $RepoRoot "artifacts\publish\win-x64"
$OutputDir = Join-Path $RepoRoot "dist"
$IssPath = Join-Path $RepoRoot "installer\IsoLaunch.iss"

Write-Host "Publishing IsoLaunch.App ($Version)..." -ForegroundColor Cyan
if (Test-Path $PublishDir) {
    Remove-Item -Recurse -Force $PublishDir
}

$publishArgs = @(
    "publish", $ProjectPath,
    "-c", "Release",
    "-r", "win-x64",
    "-o", $PublishDir
)

if ($FrameworkDependent) {
    $publishArgs += "--no-self-contained"
} else {
    $publishArgs += "--self-contained", "true"
}

& dotnet @publishArgs
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE"
}

$isccCandidates = @(
    (Get-Command "iscc" -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source -ErrorAction SilentlyContinue),
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe",
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
) | Where-Object { $_ -and (Test-Path $_) }

$iscc = $isccCandidates | Select-Object -First 1
if (-not $iscc) {
    throw "Inno Setup Compiler (ISCC.exe) not found. Install it via: winget install JRSoftware.InnoSetup"
}

New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

Write-Host "Compiling Windows installer with Inno Setup..." -ForegroundColor Cyan
& $iscc "/DAppVersion=$Version" "/DPublishDir=$PublishDir" "/DOutputDir=$OutputDir" $IssPath
if ($LASTEXITCODE -ne 0) {
    throw "ISCC failed with exit code $LASTEXITCODE"
}

Write-Host "Installer created in: $OutputDir" -ForegroundColor Green
