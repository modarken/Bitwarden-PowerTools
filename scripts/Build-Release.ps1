<#
.SYNOPSIS
    Builds and packages Bitwarden AutoType for local testing with Velopack.

.DESCRIPTION
    This script:
    1. Publishes the application in Release mode
    2. Packages it with Velopack
    3. Optionally generates delta packages if previous releases exist
    
.PARAMETER Version
    The version number to package (e.g., "1.2.0"). 
    Defaults to version in csproj.

.PARAMETER NoDelta
    Skip delta package generation even if previous releases exist.

.PARAMETER Install
    Install the packaged application after building.

.EXAMPLE
    .\Build-Release.ps1
    
.EXAMPLE
    .\Build-Release.ps1 -Version "1.3.0" -Install
#>

param(
    [string]$Version,
    [switch]$NoDelta,
    [switch]$Install
)

$ErrorActionPreference = "Stop"

# Configuration
$ProjectPath = "Bitwarden.AutoType\Bitwarden.AutoType.Desktop\Bitwarden.AutoType.Desktop.csproj"
$AppId = "Bitwarden.AutoType"
$ExeName = "Bitwarden.AutoType.Desktop.exe"
$IconPath = "Bitwarden.AutoType\Bitwarden.AutoType.Desktop\Bitwarden.AutoType.ico"
$PublishDir = ".\publish"
$ReleasesDir = ".\releases"
$PrevReleasesDir = ".\releases-prev"

# Navigate to repo root
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptDir
Set-Location $repoRoot

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Bitwarden AutoType - Release Builder" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Get version from csproj if not specified
if (-not $Version) {
    $csproj = Get-Content $ProjectPath -Raw
    if ($csproj -match '<Version>(.*?)</Version>') {
        $Version = $matches[1]
    } else {
        $Version = "1.0.0"
    }
}

Write-Host "Version: $Version" -ForegroundColor Yellow
Write-Host ""

# Check if vpk is installed
Write-Host "Checking Velopack CLI..." -ForegroundColor Gray
try {
    $null = Get-Command vpk -ErrorAction Stop
    Write-Host "  Velopack CLI found" -ForegroundColor Green
} catch {
    Write-Host "  Installing Velopack CLI..." -ForegroundColor Yellow
    dotnet tool install -g vpk
}

# Clean previous build
Write-Host ""
Write-Host "Cleaning previous build..." -ForegroundColor Gray
if (Test-Path $PublishDir) { Remove-Item $PublishDir -Recurse -Force }
if (Test-Path $ReleasesDir) { 
    # Move to prev for delta generation
    if (Test-Path $PrevReleasesDir) { Remove-Item $PrevReleasesDir -Recurse -Force }
    Rename-Item $ReleasesDir $PrevReleasesDir
}
New-Item -ItemType Directory -Force -Path $PublishDir | Out-Null
New-Item -ItemType Directory -Force -Path $ReleasesDir | Out-Null

# Update version in csproj
Write-Host ""
Write-Host "Updating version in project file..." -ForegroundColor Gray
$content = Get-Content $ProjectPath -Raw
$content = $content -replace '<Version>.*?</Version>', "<Version>$Version</Version>"
$content = $content -replace '<AssemblyVersion>.*?</AssemblyVersion>', "<AssemblyVersion>$Version.0</AssemblyVersion>"
$content = $content -replace '<FileVersion>.*?</FileVersion>', "<FileVersion>$Version.0</FileVersion>"
$content = $content -replace '<InformationalVersion>.*?</InformationalVersion>', "<InformationalVersion>$Version</InformationalVersion>"
Set-Content $ProjectPath $content
Write-Host "  Version set to $Version" -ForegroundColor Green

# Restore
Write-Host ""
Write-Host "Restoring dependencies..." -ForegroundColor Gray
dotnet restore
if ($LASTEXITCODE -ne 0) { throw "Restore failed" }

# Build
Write-Host ""
Write-Host "Building Release configuration..." -ForegroundColor Gray
dotnet build --configuration Release --no-restore
if ($LASTEXITCODE -ne 0) { throw "Build failed" }

# Publish
Write-Host ""
Write-Host "Publishing self-contained application..." -ForegroundColor Gray
dotnet publish $ProjectPath `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    --output $PublishDir `
    -p:PublishSingleFile=false `
    -p:IncludeNativeLibrariesForSelfExtract=true

if ($LASTEXITCODE -ne 0) { throw "Publish failed" }
Write-Host "  Published to $PublishDir" -ForegroundColor Green

# Package with Velopack
Write-Host ""
Write-Host "Packaging with Velopack..." -ForegroundColor Gray

$vpkArgs = @(
    "pack"
    "--packId", $AppId
    "--packVersion", $Version
    "--packDir", $PublishDir
    "--mainExe", $ExeName
    "--outputDir", $ReleasesDir
    "--shortcuts", "StartMenuRoot,Startup"
)

# Add icon if exists
if (Test-Path $IconPath) {
    $vpkArgs += "--icon"
    $vpkArgs += $IconPath
}

# Add delta if previous releases exist and not disabled
if (-not $NoDelta -and (Test-Path "$PrevReleasesDir\*.nupkg")) {
    Write-Host "  Including delta from previous release" -ForegroundColor Gray
    $vpkArgs += "--delta"
    $vpkArgs += $PrevReleasesDir
}

& vpk @vpkArgs

if ($LASTEXITCODE -ne 0) { throw "Velopack packaging failed" }

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  Build Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Output files:" -ForegroundColor Yellow
Get-ChildItem $ReleasesDir | ForEach-Object {
    $size = "{0:N2} MB" -f ($_.Length / 1MB)
    Write-Host "  $($_.Name) ($size)" -ForegroundColor White
}

# Install if requested
if ($Install) {
    Write-Host ""
    Write-Host "Installing application..." -ForegroundColor Yellow
    $setup = Get-ChildItem "$ReleasesDir\*Setup.exe" | Select-Object -First 1
    if ($setup) {
        Start-Process $setup.FullName -Wait
        Write-Host "  Installation complete!" -ForegroundColor Green
    } else {
        Write-Host "  Setup.exe not found" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "To release to GitHub:" -ForegroundColor Cyan
Write-Host "  git tag v$Version" -ForegroundColor White
Write-Host "  git push origin v$Version" -ForegroundColor White
Write-Host ""
Write-Host "Or manually upload files from $ReleasesDir to a GitHub release." -ForegroundColor Gray
