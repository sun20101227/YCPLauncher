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
    private CancellationTokenSource? _pollingCts;
    private Task? _pollingTask;
    private readonly SemaphoreSlim _fetchLock = new(1, 1);
    private string _token = string.Empty;
    private long _sessionGeneration;

    public DashboardResponse? CachedData { get; private set; }

    public event Action<DashboardResponse?>? OnCacheUpdated;

    private DashboardCacheService()
    {
        var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "YCPLauncher");
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
        StopPolling();
        _token = token;
        var generation = Interlocked.Increment(ref _sessionGeneration);
        var cts = new CancellationTokenSource();
        _pollingCts = cts;
        _pollingTask = PollLoopAsync(generation, cts.Token);
    }

    public void StopPolling()
    {
        Interlocked.Increment(ref _sessionGeneration);
        var cts = Interlocked.Exchange(ref _pollingCts, null);
        cts?.Cancel();
        cts?.Dispose();
        _pollingTask = null;
        _token = string.Empty;
    }

    public async Task FetchAndUpdateAsync()
    {
        var tokenSnapshot = _token;
        var generationSnapshot = Volatile.Read(ref _sessionGeneration);
        if (string.IsNullOrEmpty(tokenSnapshot)) return;
        if (!await _fetchLock.WaitAsync(0)) return;

        try
        {
            var response = await _apiService.GetDashboardAsync(tokenSnapshot);
            if (generationSnapshot != Volatile.Read(ref _sessionGeneration) ||
                !string.Equals(tokenSnapshot, _token, StringComparison.Ordinal))
                return;

            if (response?.Data != null)
            {
                CachedData = response;
                OnCacheUpdated?.Invoke(response);

                var json = JsonSerializer.Serialize(response);
                await File.WriteAllTextAsync(_cacheFilePath, json);
            }
        }
        catch { }
        finally
        {
            _fetchLock.Release();
        }
    }

    private async Task PollLoopAsync(long generation, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested &&
                   generation == Volatile.Read(ref _sessionGeneration))
            {
                await FetchAndUpdateAsync();
                await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when the user logs out, switches account, or exits.
        }
    }

    public long ClearCache()
    {
        long bytes = 0;
        try
        {
            if (File.Exists(_cacheFilePath))
            {
                bytes = new FileInfo(_cacheFilePath).Length;
                File.Delete(_cacheFilePath);
            }
        }
        catch { }

        CachedData = null;
        OnCacheUpdated?.Invoke(null);
        return bytes;
    }
}
