using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;
using System.Text.RegularExpressions;

namespace YCPLauncher.Services;

public class GameLauncherService
{
    private const string Cs2AppId = "730";

    /// <summary>
    /// Launches CS2 and connects to the specified server.
    /// 
    /// Strategy:
    /// 1. If CS2 is already running → use cs2.exe -hijack +connect
    /// 2. If CS2 is NOT running → create a ycp_connect.cfg and launch via steam.exe -applaunch +exec to guarantee execution when engine is ready
    /// </summary>
    public static bool LaunchDirect(string ip, int port, string serverName)
    {
        try
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                Views.LaunchLoadingDialog.ShowAndAutoClose(8000);
            });

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

            // CS2 is not running. Launch it with user-selected parameters AND +connect.
            var cfg = ConfigService.GetConfig();
            string extraArgs = "";
            if (cfg.LaunchNoVid) extraArgs += "-novid ";
            if (cfg.LaunchHighFreq) extraArgs += "-freq 240 ";
            if (cfg.LaunchConsole) extraArgs += "-console ";

            Process.Start(new ProcessStartInfo
            {
                FileName = steamExe,
                Arguments = $"-applaunch {Cs2AppId} {extraArgs.Trim()} +connect {ip}:{port}",
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
