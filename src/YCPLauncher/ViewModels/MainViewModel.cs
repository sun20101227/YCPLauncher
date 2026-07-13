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

    public IRelayCommand? NavigateToServersCommand { get; set; }

    public MainViewModel(PlayerInfo player, string token, Action onLogout)
    {
        _player = player;
        _token = token;
        _onLogout = onLogout;
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
            
        });
    }

    [RelayCommand]
    private void Logout()
    {
        AuthService.ClearToken();
        _onLogout();
    }
}
