using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using YCPLauncher.Models;

namespace YCPLauncher.Services;

public static class AuthService
{
    private static readonly string DataDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "YCPLauncher");

    private static readonly string TokenFile  = Path.Combine(DataDir, "auth.dat");
    private static readonly string PlayerFile = Path.Combine(DataDir, "player.json");
    private static readonly string UserFile   = Path.Combine(DataDir, "lastuser.txt");

    // ── Token ─────────────────────────────────────────────────────────────
    public static void SaveToken(string token)
    {
        try
        {
            Directory.CreateDirectory(DataDir);
            var data = ProtectedData.Protect(
                Encoding.UTF8.GetBytes(token), null, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(TokenFile, data);
        }
        catch { }
    }

    public static string? LoadToken()
    {
        try
        {
            if (!File.Exists(TokenFile)) return null;
            var data = ProtectedData.Unprotect(
                File.ReadAllBytes(TokenFile), null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(data);
        }
        catch { return null; }
    }

    public static void ClearToken()
    {
        try { if (File.Exists(TokenFile)) File.Delete(TokenFile); } catch { }
        try { if (File.Exists(PlayerFile)) File.Delete(PlayerFile); } catch { }
    }

    // ── PlayerInfo Cache ───────────────────────────────────────────────────
    public static void SavePlayer(PlayerInfo player)
    {
        try
        {
            Directory.CreateDirectory(DataDir);
            File.WriteAllText(PlayerFile,
                JsonSerializer.Serialize(player, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower }));
        }
        catch { }
    }

    public static PlayerInfo? LoadPlayer()
    {
        try
        {
            if (!File.Exists(PlayerFile)) return null;
            return JsonSerializer.Deserialize<PlayerInfo>(
                File.ReadAllText(PlayerFile),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch { return null; }
    }

    // ── Last Username (plaintext) ─────────────────────────────────────────
    public static void SaveLastUsername(string username)
    {
        try { Directory.CreateDirectory(DataDir); File.WriteAllText(UserFile, username); }
        catch { }
    }

    public static string? LoadLastUsername()
    {
        try { return File.Exists(UserFile) ? File.ReadAllText(UserFile).Trim() : null; }
        catch { return null; }
    }
}
