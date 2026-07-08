namespace YCPLauncher.Models;

public class PlayerInfo
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string SteamId { get; set; } = string.Empty;
    public string? TeamName { get; set; }
    public string? TeamShort { get; set; }
    public string? TeamLogo { get; set; }
}
