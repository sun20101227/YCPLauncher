using System.Text.Json.Serialization;

namespace YCPLauncher.Models;

public class PlayerInfo
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;
    
    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; } = string.Empty;
    
    [JsonPropertyName("steam_id")]
    public string SteamId { get; set; } = string.Empty;
    
    [JsonPropertyName("team_name")]
    public string? TeamName { get; set; }
    
    [JsonPropertyName("team_short")]
    public string? TeamShort { get; set; }
    
    [JsonPropertyName("team_logo")]
    public string? TeamLogo { get; set; }
}
