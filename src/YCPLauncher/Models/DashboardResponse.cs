using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace YCPLauncher.Models;

public class DashboardResponse
{
    public int Code { get; set; }
    public string Message { get; set; } = string.Empty;
    public DashboardData? Data { get; set; }
}

public class DashboardData
{
    public DashboardProfile Profile { get; set; } = new();
    public DashboardStats Stats { get; set; } = new();
    public List<DashboardMatch> RecentMatches { get; set; } = new();
    public DashboardNews News { get; set; } = new();
}

public class DashboardProfile
{
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string SteamId { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public string TeamName { get; set; } = string.Empty;
    public string TeamShort { get; set; } = string.Empty;
    public int CsRating { get; set; }
    public string RankTier { get; set; } = string.Empty;
}

public class DashboardStats
{
    public double Rating { get; set; }
    public double KdRatio { get; set; }
    public double HeadshotPct { get; set; }
    public int MatchesPlayed { get; set; }
    public double WinRate { get; set; }
    public int MvpCount { get; set; }
}

public class DashboardMatch
{
    public string MatchId { get; set; } = string.Empty;
    public string MapName { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public string Score { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Kd { get; set; } = string.Empty;
    public double Rating { get; set; }
}

public class DashboardNews
{
    public DashboardNewsBanner Banner { get; set; } = new();
    public DashboardNewsAnnouncement Announcement { get; set; } = new();
}

public class DashboardNewsBanner
{
    public string Tag { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
}

public class DashboardNewsAnnouncement
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsImportant { get; set; }
}
