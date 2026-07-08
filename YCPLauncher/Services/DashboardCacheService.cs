using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using YCPLauncher.Models;

namespace YCPLauncher.Services;

public class DashboardCacheService
{
    private static DashboardCacheService? _instance;
    public static DashboardCacheService Instance => _instance ??= new DashboardCacheService();

    private readonly ApiService _apiService = new();
    private readonly string _cacheFilePath;
    private System.Threading.Timer? _pollingTimer;
    private string _token = string.Empty;

    public DashboardResponse? CachedData { get; private set; }

    public event Action<DashboardResponse?>? OnCacheUpdated;

    private DashboardCacheService()
    {
        var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "YCPLauncher");
        if (!Directory.Exists(appData))
            Directory.CreateDirectory(appData);

        _cacheFilePath = Path.Combine(appData, "dashboard_cache.json");
        LoadCacheFromDisk();
    }

    private void LoadCacheFromDisk()
    {
        try
        {
            if (File.Exists(_cacheFilePath))
            {
                var json = File.ReadAllText(_cacheFilePath);
                CachedData = JsonSerializer.Deserialize<DashboardResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
        }
        catch { }
    }

    public void StartPolling(string token)
    {
        _token = token;
        // Poll immediately and then every 60 seconds
        _pollingTimer?.Dispose();
        _pollingTimer = new System.Threading.Timer(async _ => await FetchAndUpdateAsync(), null, 0, 60000);
    }

    public void StopPolling()
    {
        _pollingTimer?.Dispose();
        _pollingTimer = null;
    }

    public async Task FetchAndUpdateAsync()
    {
        if (string.IsNullOrEmpty(_token)) return;

        try
        {
            var response = await _apiService.GetDashboardAsync(_token);
            if (response?.Data != null)
            {
                CachedData = response;
                OnCacheUpdated?.Invoke(response);

                // Save to disk
                var json = JsonSerializer.Serialize(response);
                File.WriteAllText(_cacheFilePath, json);
            }
        }
        catch { }
    }
}
