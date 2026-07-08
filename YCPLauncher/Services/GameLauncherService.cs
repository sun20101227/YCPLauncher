using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace YCPLauncher.Services;

public class GameLauncherService
{
    private const string Cs2AppId = "730";

    /// <summary>
    /// Launches CS2 and connects to the specified server.
    /// 
    /// Strategy:
    /// 1. If CS2 is already running → use cs2.exe -hijack +connect to inject connect command
    /// 2. If CS2 is NOT running → launch via steam.exe -applaunch, then wait for process and hijack
    /// </summary>
    public static bool LaunchDirect(string ip, int port, string serverName)
    {
        try
        {
            var cs2Exe = GetCs2ExecutablePath();
            if (string.IsNullOrEmpty(cs2Exe) || !File.Exists(cs2Exe))
            {
                // cs2.exe not found, fall back to steam://connect which at least opens something
                FallbackSteamConnect(ip, port);
                return true;
            }

            var cfg = ConfigService.GetConfig();
            string extraArgs = "";
            if (cfg.LaunchNoVid) extraArgs += "-novid ";
            if (cfg.LaunchHighFreq) extraArgs += "-freq 240 ";
            if (cfg.LaunchConsole) extraArgs += "-console ";

            var cs2Procs = Process.GetProcessesByName("cs2");
            if (cs2Procs.Length > 0)
            {
                // CS2 already running → hijack and connect
                HijackAndConnect(cs2Exe, ip, port);
            }
            else
            {
                // CS2 not running → launch it via Steam, then hijack once it's up
                LaunchAndConnect(cs2Exe, ip, port, extraArgs.Trim());
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// CS2 is already running. Use -hijack to inject +connect into the running instance.
    /// </summary>
    private static void HijackAndConnect(string cs2Exe, string ip, int port)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = cs2Exe,
            Arguments = $"-hijack +connect {ip}:{port}",
            UseShellExecute = false,
            CreateNoWindow = true
        });
    }

    /// <summary>
    /// Launch CS2 via Steam, then wait for cs2.exe to appear and inject the connect command.
    /// </summary>
    private static void LaunchAndConnect(string cs2Exe, string ip, int port, string extraArgs)
    {
        // Launch the game first via steam.exe -applaunch
        var steamExe = GetSteamExePath();
        if (!string.IsNullOrEmpty(steamExe) && File.Exists(steamExe))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = steamExe,
                Arguments = $"-applaunch {Cs2AppId} {extraArgs}",
                UseShellExecute = true
            });
        }
        else
        {
            // Fallback: open steam store page which at least triggers the game
            FallbackSteamConnect(ip, port);
            return;
        }

        // Wait for CS2 process to start, then hijack with +connect
        // Run in background so we don't block the UI
        Task.Run(async () =>
        {
            // Poll for cs2.exe up to 90 seconds
            for (int i = 0; i < 90; i++)
            {
                await Task.Delay(1000);
                var procs = Process.GetProcessesByName("cs2");
                if (procs.Length > 0)
                {
                    // Give it 5 more seconds to fully initialize before hijacking
                    await Task.Delay(5000);
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = cs2Exe,
                            Arguments = $"-hijack +connect {ip}:{port}",
                            UseShellExecute = false,
                            CreateNoWindow = true
                        });
                    }
                    catch { /* Hijack attempt failed, game will be on main menu */ }
                    return;
                }
            }
        });
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

            // Parse all library folder paths from VDF
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
