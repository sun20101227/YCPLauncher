using System.Text.Json.Serialization;

namespace YCPLauncher.Models;

public class ServerInfo
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Ip { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Location { get; set; } = string.Empty;

    /// <summary>Nullable: API may return null, client fills in via ICMP ping.</summary>
    [JsonPropertyName("ping")]
    public int? ApiPing { get; set; }

    [JsonIgnore]
    public int Ping { get; set; } = 999;

    [JsonPropertyName("players")]
    public int Players { get; set; }

    [JsonPropertyName("max_players")]
    public int MaxPlayers { get; set; }
}
