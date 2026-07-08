using System.IO;
using System.Text.Json;
using YCPLauncher.Models;

namespace YCPLauncher.Services;

public class ConfigService
{
    private static AppConfig? _config;

    public static AppConfig GetConfig()
    {
        if (_config != null) return _config;

        try
        {
            var exeDir = AppDomain.CurrentDomain.BaseDirectory;
            var configPath = Path.Combine(exeDir, "config.json");
            if (File.Exists(configPath))
            {
                var json = File.ReadAllText(configPath);
                _config = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
            }
            else
            {
                _config = new AppConfig();
            }
        }
        catch
        {
            _config = new AppConfig();
        }

        return _config;
    }

    public static void SaveConfig()
    {
        if (_config == null) return;
        try
        {
            var exeDir = AppDomain.CurrentDomain.BaseDirectory;
            var configPath = Path.Combine(exeDir, "config.json");
            var json = JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(configPath, json);
        }
        catch { }
    }
}
