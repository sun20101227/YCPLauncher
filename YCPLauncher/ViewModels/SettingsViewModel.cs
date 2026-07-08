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
    }

    [ObservableProperty] private bool _hardwareAcceleration = true;
    [ObservableProperty] private bool _autoUpdate = true;
    [ObservableProperty] private bool _networkOptimization = true;
    [ObservableProperty] private bool _dynamicBackground = true;

    [RelayCommand]
    private void ClearCache()
    {
        // Mock clear cache
        System.Windows.MessageBox.Show("缓存已清空 (0 B)", "清理完毕", MessageBoxButton.OK, MessageBoxImage.Information);
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
                System.Windows.MessageBox.Show("找不到卸载程序 (YCPUninstaller.exe)。\n您可能是处于开发环境或未通过安装包安装本程序。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show("无法启动卸载程序：" + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    partial void OnIsDarkModeChanged(bool value)
    {
        string themePath = value ? "Themes/DarkTheme.xaml" : "Themes/LightTheme.xaml";
        var uri = new Uri(themePath, UriKind.Relative);
        
        var dict = new ResourceDictionary { Source = uri };
        
        Application.Current.Resources.MergedDictionaries.Clear();
        Application.Current.Resources.MergedDictionaries.Add(dict);

        ConfigService.GetConfig().IsDarkMode = value;
        ConfigService.SaveConfig();
    }

    partial void OnStartOnBootChanged(bool value)
    {
        ConfigService.GetConfig().StartOnBoot = value;
        ConfigService.SaveConfig();
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
}
