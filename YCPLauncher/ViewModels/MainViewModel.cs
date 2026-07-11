using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using YCPLauncher.Models;
using YCPLauncher.Services;
using System.Threading.Tasks;

namespace YCPLauncher.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly Action _onLogout;
    private readonly ApiService _apiService;

    [RelayCommand]
    private void JoinMatch()
    {
        NavigateToServersCommand?.Execute(null);
    }

    [ObservableProperty]
    private PlayerInfo _player;

    [ObservableProperty]
    private string _token = string.Empty;

    [ObservableProperty]
    private bool _isLoading = true;

    [ObservableProperty]
    private string _statRating = "0.00";
    [ObservableProperty]
    private string _statKd = "0.00";
    [ObservableProperty]
    private string _statHs = "0%";
    [ObservableProperty]
    private string _statMatches = "0";
    
    [ObservableProperty]
    private double _barRating = 0;
    [ObservableProperty]
    private double _barKd = 0;
    [ObservableProperty]
    private double _barHs = 0;

    [ObservableProperty]
    private DashboardNewsBanner _newsBanner = new();

    [ObservableProperty]
    private DashboardNewsAnnouncement _newsAnnouncement = new();

    public ObservableCollection<DashboardMatch> RecentMatches { get; } = new();

    // Mock Models for Missions and Live matches (Since they are just visual, we can define them as anonymous or inner objects, but better to define small classes)
    public class DailyMission
    {
        public string Title { get; set; } = "";
        public string ProgressText { get; set; } = "";
        public double ProgressPercent { get; set; }
    }

    public class LiveEsportsMatch
    {
        public string Team1 { get; set; } = "";
        public string Team2 { get; set; } = "";
        public string Score { get; set; } = "";
        public string EventName { get; set; } = "";
    }

    public ObservableCollection<DailyMission> DailyMissions { get; } = new();
    public ObservableCollection<LiveEsportsMatch> LiveMatches { get; } = new();

    public IRelayCommand? NavigateToServersCommand { get; set; }

    public MainViewModel(PlayerInfo player, string token, Action onLogout)
    {
        _player = player;
        _token = token;
        _onLogout = onLogout;
        _apiService = new ApiService();
        
        // Fire and forget data loading
        _ = LoadDashboardDataAsync();
    }

    private async Task LoadDashboardDataAsync()
    {
        // 1. Load from cache immediately if available
        var cachedResponse = DashboardCacheService.Instance.CachedData;
        if (cachedResponse != null)
        {
            UpdateUIWithData(cachedResponse);
            IsLoading = false;
        }
        else
        {
            IsLoading = true;
        }

        // 2. Trigger a background fetch to get latest
        // We run this async and update UI when done
        try
        {
            // Fetch directly using the ApiService just for this page load
            // Alternatively we could just call DashboardCacheService.Instance.FetchAndUpdateAsync()
            await DashboardCacheService.Instance.FetchAndUpdateAsync();
            var latestResponse = DashboardCacheService.Instance.CachedData;
            if (latestResponse != null)
            {
                UpdateUIWithData(latestResponse);
            }
        }
        catch { }
        finally
        {
            IsLoading = false;
        }
    }

    private void UpdateUIWithData(Models.DashboardResponse response)
    {
        var data = response.Data;
        if (data == null) return;
        
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            // Update Profile
            Player.DisplayName = data.Profile.DisplayName;
            Player.Username = data.Profile.Username;
            Player.SteamId = data.Profile.SteamId;
            Player.TeamName = data.Profile.TeamName;
            Player.TeamShort = data.Profile.TeamShort;
            OnPropertyChanged(nameof(Player));

            // Update Stats
            StatRating = data.Stats.Rating.ToString("F2");
            StatKd = data.Stats.KdRatio.ToString("F2");
            StatHs = data.Stats.HeadshotPct.ToString("F1") + "%";
            StatMatches = data.Stats.MatchesPlayed.ToString();
            
            BarRating = (data.Stats.Rating / 2.0) * 100;
            if (BarRating > 100) BarRating = 100;
            
            BarKd = (data.Stats.KdRatio / 2.0) * 100;
            if (BarKd > 100) BarKd = 100;
            
            BarHs = data.Stats.HeadshotPct;

            // Update Matches
            RecentMatches.Clear();
            foreach (var match in data.RecentMatches)
            {
                RecentMatches.Add(match);
            }

            // Update News
            if (data.News != null)
            {
                if (data.News.Banner != null)
                    NewsBanner = data.News.Banner;
                if (data.News.Announcement != null)
                    NewsAnnouncement = data.News.Announcement;
            }
            
            // Populate mock Missions
            DailyMissions.Clear();
            DailyMissions.Add(new DailyMission { Title = "Mirage 连胜", ProgressText = "2 / 3 场", ProgressPercent = 66.6 });
            DailyMissions.Add(new DailyMission { Title = "致命打击", ProgressText = "42 / 50 爆头", ProgressPercent = 84.0 });
            DailyMissions.Add(new DailyMission { Title = "团队大脑", ProgressText = "12 / 20 助攻", ProgressPercent = 60.0 });

            // Populate mock Live Matches
            LiveMatches.Clear();
            LiveMatches.Add(new LiveEsportsMatch { Team1 = "NAVI", Team2 = "FaZe", Score = "15 - 13", EventName = "IEM Cologne 2026 - Final" });
            LiveMatches.Add(new LiveEsportsMatch { Team1 = "Vitality", Team2 = "G2", Score = "Live", EventName = "IEM Cologne 2026 - Semi" });
        });
    }

    [RelayCommand]
    private void Logout()
    {
        AuthService.ClearToken();
        _onLogout();
    }
}
