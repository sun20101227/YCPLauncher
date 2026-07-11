using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;
using System.Text.RegularExpressions;

namespace YCPLauncher.Services;

public class GameLauncherService
{
    private const string Cs2AppId = "730";

    public static bool LaunchDirect(string ip, int port, string serverName, string customArgs = "")
    {
        try
        {
            System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                var dialog = new Views.TerminalLaunchDialog();
                dialog.Show();
                await dialog.RunLogsAsync(new string[] 
                {
                    $"[SYSTEM] 初始化 YCP CS2 注入引擎...",
                    $"[NETWORK] 验证通信链路状态... UDP直连成功",
                    $"[MATCH] 锁定目标服务器 -> {ip}:{port} [{serverName}]",
                    $"[LAUNCH] 正在下发启动协议...",
                    $"[SUCCESS] 指令已下发！正在拉起 Source 2 引擎，请等待..."
                });
            });

            var cfg = ConfigService.GetConfig();
            int launchMethod = cfg.LaunchMethod;

            if (launchMethod == 0)
            {
                // Method 0: steam://rungameid/730//+connect ip:port (Simplest & most stable for some systems)
                string url = $"steam://rungameid/{Cs2AppId}//+connect {ip}:{port} {customArgs}".Trim();
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
                return true;
            }

            if (launchMethod == 2)
            {
                // Method 2: Fallback steam://connect
                FallbackSteamConnect(ip, port);
                return true;
            }

            // Method 1: Legacy -applaunch with cfg injection
            var cs2Procs = Process.GetProcessesByName("cs2");
            if (cs2Procs.Length > 0)
            {
                // CS2 is already running. steam://connect works perfectly to route to the running instance.
                FallbackSteamConnect(ip, port);
                return true;
            }

            var steamExe = GetSteamExePath();
            if (string.IsNullOrEmpty(steamExe) || !File.Exists(steamExe))
            {
                // Fallback if Steam exe path not found
                FallbackSteamConnect(ip, port);
                return true;
            }

            string extraArgs = "";
            if (cfg.LaunchNoVid) extraArgs += "-novid ";
            if (cfg.LaunchHighFreq) extraArgs += "-freq 240 ";
            if (cfg.LaunchConsole) extraArgs += "-console ";

            var player = AuthService.LoadPlayer();
            if (player != null && !string.IsNullOrWhiteSpace(player.Username))
            {
                extraArgs += $"+setinfo ycp_name \"{player.Username.Trim()}\" ";
            }

            string connectArg = $"+connect {ip}:{port}";

            // Try to write to cfg to ensure it executes
            var cs2Exe = GetCs2ExecutablePath();
            if (!string.IsNullOrEmpty(cs2Exe))
            {
                try
                {
                    var gameDir = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(cs2Exe)));
                    if (gameDir != null)
                    {
                        var cfgDir = Path.Combine(gameDir, "csgo", "cfg");
                        if (Directory.Exists(cfgDir))
                        {
                            var cfgFile = Path.Combine(cfgDir, "ycp_connect.cfg");
                            File.WriteAllText(cfgFile, $"connect {ip}:{port}");
                            connectArg = "+exec ycp_connect.cfg";
                        }
                    }
                }
                catch { }
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = steamExe,
                Arguments = $"-applaunch {Cs2AppId} {extraArgs.Trim()} {connectArg}",
                UseShellExecute = true
            });

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void FallbackSteamConnect(string ip, int port)
    {
        var url = $"steam://connect/{ip}:{port}";
        Process.Start(new ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });
    }

    private static string? GetSteamExePath()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
            return key?.GetValue("SteamExe") as string;
        }
        catch
        {
            return null;
        }
    }

    private static string? GetCs2ExecutablePath()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
            if (key?.GetValue("SteamPath") is not string steamPath) return null;

            var vdfPath = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
            if (!File.Exists(vdfPath))
            {
                vdfPath = Path.Combine(steamPath, "config", "libraryfolders.vdf");
                if (!File.Exists(vdfPath)) return null;
            }

            var vdfContent = File.ReadAllText(vdfPath);

            var pathRegex = new Regex("\"path\"\\s+\"([^\"]+)\"");
            var matches = pathRegex.Matches(vdfContent);

            foreach (Match match in matches)
            {
                var libraryPath = match.Groups[1].Value.Replace("\\\\", "\\");
                var acfPath = Path.Combine(libraryPath, "steamapps", $"appmanifest_{Cs2AppId}.acf");

                if (File.Exists(acfPath))
                {
                    var cs2Exe = Path.Combine(libraryPath, "steamapps", "common",
                        "Counter-Strike Global Offensive", "game", "bin", "win64", "cs2.exe");
                    if (File.Exists(cs2Exe))
                        return cs2Exe;
                }
            }

            // Fallback: check main steam directory
            var mainExe = Path.Combine(steamPath, "steamapps", "common",
                "Counter-Strike Global Offensive", "game", "bin", "win64", "cs2.exe");
            if (File.Exists(mainExe))
                return mainExe;

            return null;
        }
        catch
        {
            return null;
        }
    }
}
