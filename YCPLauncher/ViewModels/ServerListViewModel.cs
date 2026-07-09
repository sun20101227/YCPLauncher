using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Windows;
using YCPLauncher.Models;
using YCPLauncher.Services;

using MessageBox       = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage  = System.Windows.MessageBoxImage;
using Clipboard        = System.Windows.Clipboard;

namespace YCPLauncher.ViewModels;

public partial class ServerListViewModel : ObservableObject
{
    private readonly ApiService _api;
    private readonly string _token;
    private readonly string _displayName;

    public Action<string>? OnServerJoined { get; set; }

    [ObservableProperty] private ServerInfo? _bestServer;
    [ObservableProperty] private bool   _hasBestServer = false;
    
    [ObservableProperty] private System.Collections.ObjectModel.ObservableCollection<ServerInfo> _servers = new();
    
    [ObservableProperty] private bool   _isLoading     = false;
    [ObservableProperty] private bool   _isPingRefresh = false;
    [ObservableProperty] private string _errorMessage  = string.Empty;
    [ObservableProperty] private bool   _hasError      = false;
    [ObservableProperty] private string _lastUpdated   = string.Empty;

    public ServerListViewModel(ApiService api, string token, string displayName)
    {
        _api         = api;
        _token       = token;
        _displayName = displayName;
    }

    [RelayCommand]
    public async Task LoadServersAsync()
    {
        IsLoading = true;
        HasError  = false;
        ErrorMessage = string.Empty;

        try
        {
            var result = await _api.GetServersAsync(_token);
            if (result.Error != null)
            {
                ErrorMessage = result.Error;
                HasError = true;
                return;
            }

            if (result.Servers != null && result.Servers.Any())
            {
                var pinged = await Task.WhenAll(
                    result.Servers.Select(async s =>
                    {
                        s.Ping = await PingHostAsync(s.Ip);
                        return s;
                    }));
                
                var best = pinged.OrderBy(s => s.Ping).FirstOrDefault();
                BestServer = best;
                HasBestServer = best != null;
                LastUpdated = $"最后刷新：{DateTime.Now:HH:mm:ss}";

                Servers.Clear();
                foreach (var server in pinged.OrderBy(s => s.Ping))
                {
                    Servers.Add(server);
                }
            }
            else
            {
                BestServer = null;
                HasBestServer = false;
                Servers.Clear();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"网络错误：{ex.Message}";
            HasError = true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task RefreshPingAsync()
    {
        if (IsPingRefresh) return;
        IsPingRefresh = true;
        try
        {
            await LoadServersAsync();
        }
        finally { IsPingRefresh = false; }
    }

    [RelayCommand]
    private void JoinServer(ServerInfo server)
    {
        if (server == null) return;
        var steamProcs = Process.GetProcessesByName("steam");
        if (steamProcs.Length == 0)
        {
            MessageBox.Show("请先启动 Steam", "Steam 未运行",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        GameLauncherService.LaunchDirect(server.Ip, server.Port, _displayName);

        OnServerJoined?.Invoke($"正在连接 {server.Name}...");
    }

    [RelayCommand]
    private void CopyIp(ServerInfo server)
    {
        if (server == null) return;
        try
        {
            Clipboard.SetText($"{server.Ip}:{server.Port}");
            OnServerJoined?.Invoke($"已复制 {server.Ip}:{server.Port}");
        }
        catch { }
    }

    private static async Task<int> PingHostAsync(string host)
    {
        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(host, 3000);
            return reply.Status == IPStatus.Success ? (int)reply.RoundtripTime : 999;
        }
        catch { return 999; }
    }
}
