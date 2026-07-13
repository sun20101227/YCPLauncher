using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using YCPLauncher.Models;

namespace YCPLauncher.Services;

public class ConfigService
{
    private static AppConfig? _config;
    private static readonly JsonSerializerOptions SaveSerializerOptions = new()
    {
        WriteIndented = true
    };
    private static readonly string UserConfigPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "YCPLauncher",
        "config.json");

    public static bool AreAnimationsEnabled =>
        !GetConfig().ReduceAnimations &&
        SystemParameters.ClientAreaAnimation &&
        (RenderCapability.Tier >> 16) > 0;

    public static AppConfig GetConfig()
    {
        if (_config != null) return _config;

        try
        {
            var bundledConfigPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "config.json");
            var configPath = File.Exists(UserConfigPath)
                ? UserConfigPath
                : bundledConfigPath;
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
            var configDirectory = Path.GetDirectoryName(UserConfigPath);
            if (configDirectory != null)
                Directory.CreateDirectory(configDirectory);
            var json = JsonSerializer.Serialize(_config, SaveSerializerOptions);
            File.WriteAllText(UserConfigPath, json);
        }
        catch { }
    }
}
