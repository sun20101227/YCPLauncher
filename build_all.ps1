$ErrorActionPreference = "Stop"

Write-Host "1. Building YCPLauncher (Multi-File, Framework-Dependent)..."
dotnet publish .\YCPLauncher\YCPLauncher.csproj -c Release -r win-x64 --self-contained false -o .\YCPLauncher\dist

Write-Host "2. Building YCPUninstaller (Single-File, Framework-Dependent)..."
dotnet publish .\YCPUninstaller\YCPUninstaller.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o .\YCPLauncher\dist

Write-Host "3. Creating payload.zip..."
$zipPath = ".\YCPInstaller\payload.zip"
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Compress-Archive -Path ".\YCPLauncher\dist\*" -DestinationPath $zipPath -Force

Write-Host "4. Building YCPInstaller (Single-File, Self-Contained)..."
dotnet publish .\YCPInstaller\YCPInstaller.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o .\SetupOutput

Write-Host ""
Write-Host "===== Build Complete! =====" -ForegroundColor Green
Write-Host "Installer : .\SetupOutput\YCPInstaller.exe"
Write-Host "App files : .\YCPLauncher\dist\"
