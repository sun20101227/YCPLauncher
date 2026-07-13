$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repoRoot = Split-Path -Parent $PSScriptRoot
[xml]$buildProps = Get-Content -Raw (Join-Path $repoRoot "Directory.Build.props")
$version = [string]$buildProps.Project.PropertyGroup.Version
$artifacts = Join-Path $repoRoot "artifacts\plugin"
$publishDir = Join-Path $artifacts "publish"
$packageRoot = Join-Path $artifacts "package"
$pluginDir = Join-Path $packageRoot "YCPNameSync"
$zipPath = Join-Path $repoRoot "artifacts\YCPNameSyncPlugin_v$version.zip"

if (Test-Path -LiteralPath $artifacts) {
    Remove-Item -LiteralPath $artifacts -Recurse -Force
}
New-Item -ItemType Directory -Path $pluginDir -Force | Out-Null

dotnet publish (Join-Path $repoRoot "plugins\YCPNameSync\YCPNameSync.csproj") `
    -c Release -o $publishDir
if ($LASTEXITCODE -ne 0) { throw "YCPNameSync 构建失败。" }

Copy-Item -LiteralPath (Join-Path $publishDir "YCPNameSync.dll") -Destination $pluginDir
Copy-Item -LiteralPath (Join-Path $repoRoot "plugins\YCPNameSync\README.txt") -Destination $pluginDir

if (Test-Path -LiteralPath $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}
Compress-Archive -Path (Join-Path $packageRoot "*") -DestinationPath $zipPath -Force

Write-Host "插件打包完成：$zipPath" -ForegroundColor Green
