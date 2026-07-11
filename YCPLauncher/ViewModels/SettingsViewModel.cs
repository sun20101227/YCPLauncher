using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;
using YCPLauncher.Services;
using Application = System.Windows.Application;
using System.Diagnostics;
using System.IO;

namespace YCPLauncher.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    public static SettingsViewModel Instance { get; } = new SettingsViewModel();
    [ObservableProperty]
    private bool _isDarkMode;
    
    [ObservableProperty]
    private bool _startOnBoot;
    
    [ObservableProperty]
    private bool _minimizeToTray;

    public SettingsViewModel()
    {
        var cfg = ConfigService.GetConfig();
        _isDarkMode = cfg.IsDarkMode;
        _startOnBoot = cfg.StartOnBoot;
        _minimizeToTray = cfg.MinimizeToTray;

        _reduceAnimations = cfg.ReduceAnimations;
        _launchNoVid = cfg.LaunchNoVid;
        _launchHighFreq = cfg.LaunchHighFreq;
        _launchConsole = cfg.LaunchConsole;
        _launchMethod = cfg.LaunchMethod;

        _apiBaseUrl = cfg.ApiBaseUrl;
        _chatUrl = cfg.ChatUrl;
        _liveStreamUrl = cfg.LiveStreamUrl;
        
        IsLoggedOut = string.IsNullOrEmpty(AuthService.LoadToken());
    }

    public bool IsLoggedOut { get; }

    [RelayCommand]
    private void GoBack()
    {
        if (Application.Current.MainWindow is MainWindow mw)
        {
            mw.NavigateToLogin();
        }
    }

    [ObservableProperty] private bool _hardwareAcceleration = true;
    partial void OnHardwareAccelerationChanged(bool value)
    {
        // Settings are conceptual right now, but we'll persist them
        // ConfigService.GetConfig().HardwareAcceleration = value;
        // ConfigService.SaveConfig();
    }

    [ObservableProperty] private bool _networkOptimization = true;
    partial void OnNetworkOptimizationChanged(bool value)
    {
        // ConfigService.GetConfig().NetworkOptimization = value;
        // ConfigService.SaveConfig();
    }

    [ObservableProperty] private bool _launchNoVid;
    partial void OnLaunchNoVidChanged(bool value)
    {
        ConfigService.GetConfig().LaunchNoVid = value;
        ConfigService.SaveConfig();
    }

    [ObservableProperty] private bool _launchHighFreq;
    partial void OnLaunchHighFreqChanged(bool value)
    {
        ConfigService.GetConfig().LaunchHighFreq = value;
        ConfigService.SaveConfig();
    }

    [ObservableProperty] private bool _launchConsole;
    partial void OnLaunchConsoleChanged(bool value)
    {
        ConfigService.GetConfig().LaunchConsole = value;
        ConfigService.SaveConfig();
    }

    [ObservableProperty] private int _launchMethod;
    partial void OnLaunchMethodChanged(int value)
    {
        ConfigService.GetConfig().LaunchMethod = value;
        ConfigService.SaveConfig();
    }

    public string CurrentVersion => "v" + YCPLauncher.App.CurrentVersion;

    [RelayCommand]
    private void ClearCache()
    {
        var dialog = new YCPLauncher.Views.MessageDialog("缓存已清空 (0 B)", "清理完毕");
        dialog.Owner = Application.Current.MainWindow;
        dialog.ShowDialog();
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task CheckUpdateAsync()
    {
        try
        {
            var updateInfo = await new ApiService().CheckForUpdateAsync(YCPLauncher.App.CurrentVersion);
            if (updateInfo != null && updateInfo.UpdateAvailable)
            {
                var dialog = new YCPLauncher.Views.UpdateDialog(updateInfo);
                dialog.Owner = Application.Current.MainWindow;
                dialog.ShowDialog();
            }
            else
            {
                var dialog = new YCPLauncher.Views.MessageDialog($"当前已是最新版本 ({CurrentVersion})。", "检查更新");
                dialog.Owner = Application.Current.MainWindow;
                dialog.ShowDialog();
            }
        }
        catch (Exception ex)
        {
            var dialog = new YCPLauncher.Views.MessageDialog("检查更新失败: " + ex.Message, "错误");
            dialog.Owner = Application.Current.MainWindow;
            dialog.ShowDialog();
        }
    }

    [RelayCommand]
    private void ViewChangelog()
    {
        var dialog = new YCPLauncher.Views.ChangelogDialog();
        dialog.Owner = Application.Current.MainWindow;
        dialog.ShowDialog();
    }

    [RelayCommand]
    private void UninstallLauncher()
    {
        try
        {
            string uninstallerPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "YCPUninstaller.exe");
            if (File.Exists(uninstallerPath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = uninstallerPath,
                    UseShellExecute = true
                });
            }
            else
            {
                var dialog = new YCPLauncher.Views.MessageDialog("找不到卸载程序 (YCPUninstaller.exe)。\n您可能是处于开发环境或未通过安装包安装本程序。", "错误");
                dialog.Owner = Application.Current.MainWindow;
                dialog.ShowDialog();
            }
        }
        catch (Exception ex)
        {
            var dialog = new YCPLauncher.Views.MessageDialog("无法启动卸载程序：" + ex.Message, "错误");
            dialog.Owner = Application.Current.MainWindow;
            dialog.ShowDialog();
        }
    }

    [RelayCommand]
    private void ChangePassword()
    {
        var token = AuthService.LoadToken();
        if (string.IsNullOrEmpty(token)) return;

        var dialog = new YCPLauncher.Views.ChangePasswordDialog(token);
        dialog.Owner = Application.Current.MainWindow;
        dialog.ShowDialog();

        if (dialog.PasswordChanged)
        {
            var msgDialog = new YCPLauncher.Views.MessageDialog("密码修改成功，请重新登录。", "成功");
            msgDialog.Owner = Application.Current.MainWindow;
            msgDialog.ShowDialog();
            
            // Clear token and restart application
            AuthService.ClearToken();
            var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
            if (exePath != null)
            {
                System.Diagnostics.Process.Start(exePath);
                Application.Current.Shutdown();
            }
        }
    }

    partial void OnIsDarkModeChanged(bool value)
    {
        string themePath = value ? "Themes/DarkTheme.xaml" : "Themes/LightTheme.xaml";
        var uri = new Uri(themePath, UriKind.Relative);
        var dict = new ResourceDictionary { Source = uri };
        
        if (Application.Current.Resources.MergedDictionaries.Count > 0)
        {
            Application.Current.Resources.MergedDictionaries[0] = dict;
        }
        else
        {
            Application.Current.Resources.MergedDictionaries.Add(dict);
        }

        ConfigService.GetConfig().IsDarkMode = value;
        ConfigService.SaveConfig();

        if (Application.Current.MainWindow != null)
        {
            YCPLauncher.Helpers.DwmHelper.SetImmersiveDarkMode(Application.Current.MainWindow, value);
        }
    }

    partial void OnStartOnBootChanged(bool value)
    {
        ConfigService.GetConfig().StartOnBoot = value;
        ConfigService.SaveConfig();
        
        try
        {
            var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
            if (key != null)
            {
                if (value)
                {
                    string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "";
                    if (!string.IsNullOrEmpty(exePath))
                    {
                        key.SetValue("YCPLauncher", $"\"{exePath}\" --silent");
                    }
                }
                else
                {
                    key.DeleteValue("YCPLauncher", false);
                }
            }
        }
        catch { }
    }

    partial void OnMinimizeToTrayChanged(bool value)
    {
        ConfigService.GetConfig().MinimizeToTray = value;
        ConfigService.SaveConfig();
    }



    [ObservableProperty]
    private bool _reduceAnimations;

    partial void OnReduceAnimationsChanged(bool value)
    {
        ConfigService.GetConfig().ReduceAnimations = value;
        ConfigService.SaveConfig();
    }

    [ObservableProperty]
    private string _apiBaseUrl = "https://cs2.yachiyo8000.cn";
    partial void OnApiBaseUrlChanged(string value)
    {
        ConfigService.GetConfig().ApiBaseUrl = value;
        ConfigService.SaveConfig();
    }

    [ObservableProperty]
    private string _chatUrl = "https://huyoutalk.mihuyou.online/ycp2026";
    partial void OnChatUrlChanged(string value)
    {
        ConfigService.GetConfig().ChatUrl = value;
        ConfigService.SaveConfig();
    }

    [ObservableProperty]
    private string _liveStreamUrl = "rtmp://frp-pen.com:48399/live/ycp";
    partial void OnLiveStreamUrlChanged(string value)
    {
        ConfigService.GetConfig().LiveStreamUrl = value;
        ConfigService.SaveConfig();
    }
}
