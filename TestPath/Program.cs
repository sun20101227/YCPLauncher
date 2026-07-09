using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;
using System.Text.RegularExpressions;

class Program
{
    private const string Cs2AppId = "730";

    static void Main()
    {
        var steamExe = GetSteamExePath();
        Console.WriteLine($"SteamExe: {steamExe}");

        var cs2Exe = GetCs2ExecutablePath();
        Console.WriteLine($"Cs2Exe: {cs2Exe}");

        if (!string.IsNullOrEmpty(cs2Exe))
        {
            var gameDir = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(cs2Exe)));
            var cfgDir = Path.Combine(gameDir, "csgo", "cfg");
            Console.WriteLine($"cfgDir: {cfgDir}");
            Console.WriteLine($"Directory exists: {Directory.Exists(cfgDir)}");
        }
    }

    private static string? GetSteamExePath()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
            return key?.GetValue("SteamExe") as string;
        }
        catch { return null; }
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
                    if (File.Exists(cs2Exe)) return cs2Exe;
                }
            }

            var mainExe = Path.Combine(steamPath, "steamapps", "common",
                "Counter-Strike Global Offensive", "game", "bin", "win64", "cs2.exe");
            if (File.Exists(mainExe)) return mainExe;

            return null;
        }
        catch { return null; }
    }
}
