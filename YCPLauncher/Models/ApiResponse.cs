using System.Text.Json.Serialization;

namespace YCPLauncher.Models;

public class LoginResponse
{
    [JsonPropertyName("token")]
    public string? Token { get; set; }

    [JsonPropertyName("player")]
    public PlayerInfo? Player { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

public class ServersResponse
{
    [JsonPropertyName("servers")]
    public List<ServerInfo>? Servers { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

public class AppConfig
{
    [JsonPropertyName("api_base_url")]
    public string ApiBaseUrl { get; set; } = "https://cs2.yachiyo8000.cn";

    [JsonPropertyName("api_timeout")]
    public int ApiTimeout { get; set; } = 10;

    [JsonPropertyName("minimize_to_tray")]
    public bool MinimizeToTray { get; set; } = true;

    [JsonPropertyName("is_dark_mode")]
    public bool IsDarkMode { get; set; } = true;

    [JsonPropertyName("start_on_boot")]
    public bool StartOnBoot { get; set; } = false;

    [JsonPropertyName("scanline_mode")]
    public int ScanlineMode { get; set; } = 1; // 0: Disabled, 1: Vertical, 2: Horizontal

    [JsonPropertyName("reduce_animations")]
    public bool ReduceAnimations { get; set; } = false;
}
