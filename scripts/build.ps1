$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repoRoot = Split-Path -Parent $PSScriptRoot
$artifacts = Join-Path $repoRoot "artifacts"
$appOutput = Join-Path $artifacts "app"
$portableOutput = Join-Path $artifacts "portable\YCPLauncher"
$installerOutput = Join-Path $artifacts "installer"
$payload = Join-Path $repoRoot "src\YCPInstaller\payload.zip"
[xml]$buildProps = Get-Content -Raw (Join-Path $repoRoot "Directory.Build.props")
$version = [string]$buildProps.Project.PropertyGroup.Version
$portableZip = Join-Path $artifacts "YCPLauncher_Portable_v$version.zip"

function Reset-Directory([string]$Path) {
    if (Test-Path -LiteralPath $Path) {
        Remove-Item -LiteralPath $Path -Recurse -Force
    }
    New-Item -ItemType Directory -Path $Path -Force | Out-Null
}

Reset-Directory $appOutput
Reset-Directory $portableOutput
Reset-Directory $installerOutput

Write-Host "1/5 发布免安装版 YCPLauncher..." -ForegroundColor Cyan
dotnet publish (Join-Path $repoRoot "src\YCPLauncher\YCPLauncher.csproj") `
    -c Release -r win-x64 --self-contained false -o $portableOutput
if ($LASTEXITCODE -ne 0) { throw "YCPLauncher 发布失败。" }

# The application targets win-x64. Remove the unused x86 LibVLC payload to
# reduce installer size, extraction time, and antivirus scanning overhead.
$unusedVlcArchitecture = Join-Path $portableOutput "libvlc\win-x86"
if (Test-Path -LiteralPath $unusedVlcArchitecture) {
    Remove-Item -LiteralPath $unusedVlcArchitecture -Recurse -Force
}

$portableReadme = @"
YCP Launcher Portable v$version
===============================

双击 YCPLauncher.exe 即可启动，无需安装。
请勿单独移动 EXE；动态壁纸和直播组件依赖同目录文件。
系统需要安装 .NET 8 Desktop Runtime (x64)。
"@
Set-Content -LiteralPath (Join-Path $portableOutput "便携版使用说明.txt") `
    -Value $portableReadme -Encoding UTF8

if (Test-Path -LiteralPath $portableZip) {
    Remove-Item -LiteralPath $portableZip -Force
}
Compress-Archive -Path (Join-Path $portableOutput "*") `
    -DestinationPath $portableZip -CompressionLevel Optimal

# The installer payload contains the same portable app plus the uninstaller.
Copy-Item -Path (Join-Path $portableOutput "*") -Destination $appOutput -Recurse -Force

Write-Host "2/5 发布 YCPUninstaller..." -ForegroundColor Cyan
dotnet publish (Join-Path $repoRoot "src\YCPUninstaller\YCPUninstaller.csproj") `
    -c Release -r win-x64 --self-contained false `
    -p:PublishSingleFile=true -o $appOutput
if ($LASTEXITCODE -ne 0) { throw "YCPUninstaller 发布失败。" }

Write-Host "3/5 生成安装负载..." -ForegroundColor Cyan
if (Test-Path -LiteralPath $payload) {
    Remove-Item -LiteralPath $payload -Force
}
Compress-Archive -Path (Join-Path $appOutput "*") -DestinationPath $payload -Force

Write-Host "4/5 发布 YCPInstaller..." -ForegroundColor Cyan
dotnet publish (Join-Path $repoRoot "src\YCPInstaller\YCPInstaller.csproj") `
    -c Release -r win-x64 --self-contained false `
    -p:PublishSingleFile=true -o $installerOutput
if ($LASTEXITCODE -ne 0) { throw "YCPInstaller 发布失败。" }

Write-Host "5/5 校验输出..." -ForegroundColor Cyan
if (-not (Test-Path -LiteralPath (Join-Path $portableOutput "YCPLauncher.exe"))) {
    throw "便携版缺少 YCPLauncher.exe。"
}
if (-not (Test-Path -LiteralPath (Join-Path $installerOutput "YCPInstaller.exe"))) {
    throw "安装包缺少 YCPInstaller.exe。"
}

Write-Host ""
Write-Host "构建完成" -ForegroundColor Green
Write-Host "安装程序: $(Join-Path $installerOutput 'YCPInstaller.exe')"
Write-Host "免安装 EXE: $(Join-Path $portableOutput 'YCPLauncher.exe')"
Write-Host "便携版 ZIP: $portableZip"
