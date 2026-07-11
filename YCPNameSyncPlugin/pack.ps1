$pluginDir = 'c:\Users\ronfu\Desktop\cient\YCPNameSyncPlugin\pkg\YCPNameSync'
New-Item -ItemType Directory -Force -Path $pluginDir | Out-Null

Copy-Item 'c:\Users\ronfu\Desktop\cient\YCPNameSyncPlugin\build_out\YCPNameSync.dll' $pluginDir -Force

$readme = @"
YCP Name Sync Plugin v1.1.0
============================
Installation:
  game/csgo/addons/counterstrikesharp/plugins/YCPNameSync/YCPNameSync.dll

ycp_team values:
  1 = Spectator / Host
  2 = Terrorist (T)
  3 = Counter-Terrorist (CT)
  4 = CT Coach  (coach ct)
  5 = T Coach   (coach t)
"@
Set-Content -Path (Join-Path $pluginDir 'README.txt') -Value $readme -Encoding UTF8

$zipPath = 'c:\Users\ronfu\Desktop\cient\YCPNameSyncPlugin.zip'
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Compress-Archive -Path 'c:\Users\ronfu\Desktop\cient\YCPNameSyncPlugin\pkg\*' -DestinationPath $zipPath -Force
Write-Host "Done: $zipPath"
Get-Item $zipPath | Select-Object Name, Length
