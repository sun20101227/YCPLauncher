$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repoRoot = Split-Path -Parent $PSScriptRoot
[xml]$buildProps = Get-Content -Raw (Join-Path $repoRoot "Directory.Build.props")
$version = [string]$buildProps.Project.PropertyGroup.Version
$tag = "v$version"
$installer = Join-Path $repoRoot "artifacts\installer\YCPInstaller.exe"
$portable = Join-Path $repoRoot "artifacts\YCPLauncher_Portable_v$version.zip"
$plugin = Join-Path $repoRoot "artifacts\YCPNameSyncPlugin_v$version.zip"
$notes = Join-Path $repoRoot "docs\release-notes\$version.md"
$repo = "sun20101227/YCPLauncher"
$releaseAssets = @($installer, $portable, $plugin)

if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    throw "未安装 GitHub CLI（gh）。请先安装并执行 gh auth login。"
}
foreach ($asset in $releaseAssets) {
    if (-not (Test-Path -LiteralPath $asset)) {
        throw "找不到发布文件：$asset。请先运行 scripts\build.ps1 和 scripts\package-plugin.ps1。"
    }
}
if (-not (Test-Path -LiteralPath $notes)) {
    throw "找不到发布说明：$notes。"
}

gh auth status
if ($LASTEXITCODE -ne 0) { throw "GitHub CLI 尚未登录。" }

gh release create $tag @releaseAssets `
    --repo $repo `
    --title "YCP Launcher $tag" `
    --notes-file $notes
if ($LASTEXITCODE -ne 0) { throw "GitHub Release 发布失败。" }

Write-Host "GitHub Release 发布完成：$tag" -ForegroundColor Green
