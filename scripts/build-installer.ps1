param(
    [string]$Version,
    [ValidateSet("patch", "minor", "major", "none")]
    [string]$Bump = "patch",
    [switch]$FrameworkDependent
)

$ErrorActionPreference = "Stop"

$RepoRoot = Split-Path -Parent $PSScriptRoot
$PropsPath = Join-Path $RepoRoot "Directory.Build.props"
$ProjectPath = Join-Path $RepoRoot "IsoLaunch.App\IsoLaunch.App.csproj"
$PublishDir = Join-Path $RepoRoot "artifacts\publish\win-x64"
$OutputDir = Join-Path $RepoRoot "dist"
$IssPath = Join-Path $RepoRoot "installer\IsoLaunch.iss"

[xml]$propsXml = Get-Content -Path $PropsPath -Raw
$versionNode = $propsXml.SelectSingleNode("/Project/PropertyGroup/Version")
if (-not $versionNode) {
    throw "Could not find <Version> in $PropsPath"
}

if ([string]::IsNullOrWhiteSpace($Version)) {
    $current = [version]$versionNode.InnerText.Trim()
    $major = $current.Major
    $minor = $current.Minor
    $patch = if ($current.Build -ge 0) { $current.Build } else { 0 }

    switch ($Bump) {
        "major" { $major++; $minor = 0; $patch = 0 }
        "minor" { $minor++; $patch = 0 }
        "patch" { $patch++ }
        "none"  { }
    }

    $Version = "$major.$minor.$patch"
}

$versionNode.InnerText = $Version
$propsXml.Save($PropsPath)
Write-Host "Version set to $Version in Directory.Build.props" -ForegroundColor Yellow

Write-Host "Publishing IsoLaunch.App ($Version)..." -ForegroundColor Cyan
if (Test-Path $PublishDir) {
    Remove-Item -Recurse -Force $PublishDir
}

$publishArgs = @(
    "publish", $ProjectPath,
    "-c", "Release",
    "-r", "win-x64",
    "-p:Version=$Version",
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

Write-Host "Installer created in: $OutputDir (v$Version)" -ForegroundColor Green
