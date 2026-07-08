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
    /// Launches CS2 directly via executable, injecting connect arguments to avoid the Steam GUI popping up.
    /// Requires Steam to be running in the background.
    /// </summary>
    public static bool LaunchDirect(string ip, int port, string serverName)
    {
        // Always use Steam Protocol to ensure VAC verification passes.
        // Direct execution of cs2.exe blocks VAC from trusting the process.
        return LaunchViaSteamProtocol(ip, port, serverName);
    }

    private static bool LaunchViaSteamProtocol(string ip, int port, string serverName)
    {
        try
        {
            // The most reliable way to join a server in CS2 (works whether the game is running or not)
            // is to use the steam://connect/ protocol.
            var url = $"steam://connect/{ip}:{port}";
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
            return true;
        }
        catch
        {
            return false;
        }
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
            
            // Regex to find library folders and check if they contain 730
            // Very simplified parser for VDF format
            var pathRegex = new Regex("\"path\"\\s+\"([^\"]+)\"");
            var matches = pathRegex.Matches(vdfContent);
            
            foreach (Match match in matches)
            {
                var libraryPath = match.Groups[1].Value.Replace("\\\\", "\\");
                var acfPath = Path.Combine(libraryPath, "steamapps", $"appmanifest_{Cs2AppId}.acf");
                
                if (File.Exists(acfPath))
                {
                    // Found the library containing CS2
                    var cs2Exe = Path.Combine(libraryPath, "steamapps", "common", "Counter-Strike Global Offensive", "game", "bin", "win64", "cs2.exe");
                    if (File.Exists(cs2Exe))
                        return cs2Exe;
                }
            }
            
            // Fallback: check main steam directory
            var mainExe = Path.Combine(steamPath, "steamapps", "common", "Counter-Strike Global Offensive", "game", "bin", "win64", "cs2.exe");
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
