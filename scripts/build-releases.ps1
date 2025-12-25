# Build releases for all platforms
# This script creates self-contained releases for Windows, Linux, and macOS

param(
    [string]$Version = "1.2.0"
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Building Pathfinder Save Parser v$Version" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Create releases directory
$ReleasesDir = "releases"
if (Test-Path $ReleasesDir) {
    Remove-Item -Recurse -Force $ReleasesDir
}
New-Item -ItemType Directory -Path $ReleasesDir | Out-Null

# Platform configurations
$platforms = @(
    @{Name="Windows x64"; RID="win-x64"; FileName="PathfinderSaveParser-$Version-win-x64.zip"},
    @{Name="Linux x64"; RID="linux-x64"; FileName="PathfinderSaveParser-$Version-linux-x64.tar.gz"},
    @{Name="macOS x64 (Intel)"; RID="osx-x64"; FileName="PathfinderSaveParser-$Version-osx-x64.tar.gz"},
    @{Name="macOS ARM64 (Apple Silicon)"; RID="osx-arm64"; FileName="PathfinderSaveParser-$Version-osx-arm64.tar.gz"}
)

foreach ($platform in $platforms) {
    Write-Host "Building for $($platform.Name)..." -ForegroundColor Yellow
    
    $outputPath = Join-Path $ReleasesDir $platform.RID
    
    # Build with self-contained deployment
    dotnet publish PathfinderSaveParser/PathfinderSaveParser.csproj `
        -c Release `
        -r $platform.RID `
        --self-contained true `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -o $outputPath
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build failed for $($platform.Name)" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "  Packaging..." -ForegroundColor Gray
    
    # Package based on platform
    $archivePath = Join-Path $ReleasesDir $platform.FileName
    
    if ($platform.RID -like "win-*") {
        # Create ZIP for Windows
        Compress-Archive -Path "$outputPath/*" -DestinationPath $archivePath -Force
    } else {
        # Create TAR.GZ for Linux/macOS
        Push-Location $outputPath
        tar -czf "../$($platform.FileName)" *
        Pop-Location
    }
    
    # Clean up build output directory
    Remove-Item -Recurse -Force $outputPath
    
    $size = (Get-Item $archivePath).Length / 1MB
    Write-Host "  Created: $($platform.FileName) ($([math]::Round($size, 2)) MB)" -ForegroundColor Green
    Write-Host ""
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "All releases built successfully!" -ForegroundColor Green
Write-Host "Location: $ReleasesDir/" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Release files:" -ForegroundColor Yellow
Get-ChildItem $ReleasesDir -Filter "PathfinderSaveParser-*" | ForEach-Object {
    $size = $_.Length / 1MB
    Write-Host "  $($_.Name) - $([math]::Round($size, 2)) MB"
}
Write-Host ""
Write-Host "Ready to upload to GitHub Releases!" -ForegroundColor Green
