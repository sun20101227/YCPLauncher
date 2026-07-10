using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Drawing;
using YCPLauncher.Models;
using YCPLauncher.Services;
using YCPLauncher.ViewModels;
using YCPLauncher.Views;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Interop;

using YCPLauncher.Helpers;
using Application      = System.Windows.Application;
using Button           = System.Windows.Controls.Button;

namespace YCPLauncher;

public partial class MainWindow : Window
{
    private readonly ApiService _api = new();
    private PlayerInfo? _player;
    private string _token = string.Empty;
    private bool _isExiting = false;

    public MainWindow()
    {
        InitializeComponent();
        TxtVersion.Text = App.CurrentVersion;
        TryAutoLogin();
        _ = CheckForUpdatesOnStartupAsync();
    }

    private async Task CheckForUpdatesOnStartupAsync()
    {
        try
        {
            var update = await _api.CheckForUpdateAsync(App.CurrentVersion);
            if (update != null && update.UpdateAvailable)
            {
                Dispatcher.Invoke(() =>
                {
                    var dialog = new UpdateDialog(update);
                    dialog.Owner = this;
                    dialog.ShowDialog();
                });
            }
        }
        catch { }
    }



    protected override void OnStateChanged(EventArgs e)
    {
        base.OnStateChanged(e);
        if (MaxIcon != null)
            MaxIcon.Text = WindowState == WindowState.Maximized ? "\xE923" : "\xE922";
    }

    // ── Tray ──────────────────────────────────────────────────────────────
    private void TrayIcon_DoubleClick(object sender, RoutedEventArgs e)
    {
        ShowWindow();
    }

    private void TrayShow_Click(object sender, RoutedEventArgs e)
    {
        ShowWindow();
    }

    private void TrayExit_Click(object sender, RoutedEventArgs e)
    {
        ExitApp();
    }

    private void ShowWindow()
    { Show(); WindowState = WindowState.Normal; Activate(); }

    private void ExitApp()
    { 
        _isExiting = true;
        MyNotifyIcon?.Dispose(); 
        Application.Current.Shutdown(); 
    }

    // ── Window Chrome ──────────────────────────────────────────────────────
    private void DragBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        else DragMove();
    }
    private void MinBtn_Click(object sender, RoutedEventArgs e)  => WindowState = WindowState.Minimized;
    private void MaxBtn_Click(object sender, RoutedEventArgs e) 
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }
    private void CloseBtn_Click(object sender, RoutedEventArgs e)
    {
        if (ConfigService.GetConfig().MinimizeToTray)
            HideToTray();
        else
            ExitApp();
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    { 
        if (!_isExiting && ConfigService.GetConfig().MinimizeToTray)
        {
            e.Cancel = true; 
            HideToTray(); 
        }
        else
        {
            MyNotifyIcon?.Dispose();
        }
    }

    private void HideToTray()
    {
        Hide();
    }

    // ── Auto Login ────────────────────────────────────────────────────────
    private void TryAutoLogin()
    {
        var token  = AuthService.LoadToken();
        var player = AuthService.LoadPlayer();
        if (!string.IsNullOrEmpty(token) && player != null)
        {
            _token  = token;
            _player = player;
            DashboardCacheService.Instance.StartPolling(_token);
            NavigateToIntro(); // Changed to default to Intro
            _ = ValidateTokenAsync();
        }
        else NavigateToLogin();
    }

    private async Task ValidateTokenAsync()
    {
        try
        {
            var r = await _api.GetServersAsync(_token);
            if (r.Error != null)
                Dispatcher.Invoke(() => { AuthService.ClearToken(); NavigateToLogin(); });
        }
        catch { }
    }

    // ── Navigation + Fade ────────────────────────────────────────────────
    public void SetContent(System.Windows.Controls.UserControl view)
    {
        var out_ = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(150)) { EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut } };
        out_.Completed += (_, _) =>
        {
            if (view.RenderTransform is not TranslateTransform)
                view.RenderTransform = new TranslateTransform();
            
            MainContent.Content = view;
            
            var inOpacity = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(250)) { EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut } };
            var inSlide = new DoubleAnimation(20, 0, TimeSpan.FromMilliseconds(300)) { EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut } };
            
            MainContent.BeginAnimation(OpacityProperty, inOpacity);
            view.RenderTransform.BeginAnimation(TranslateTransform.YProperty, inSlide);
        };
        MainContent.BeginAnimation(OpacityProperty, out_);
    }

    public void NavigateToLogin()
    {
        _token = string.Empty; _player = null;
        SetNavActive(null);
        NavPanel.Visibility = Visibility.Collapsed;
        SetContent(new LoginView { DataContext = new LoginViewModel(_api, OnLoginSuccess) });
    }

    private void OnLoginSuccess(PlayerInfo player, string token)
    {
        _player = player; _token = token;
        DashboardCacheService.Instance.StartPolling(_token);
        NavPanel.Visibility = Visibility.Visible;
        
        bool isFirstLaunch = true;
        try
        {
            using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"SOFTWARE\YCPLauncher"))
            {
                key.SetValue("HasSeenIntro", "1");
            }
        }
        catch { }

        // Always navigate to Intro as requested
        NavigateToIntro();
    }

    public void NavigateToIntro()
    {
        SetNavActive(NavIntro);
        SetContent(new IntroView());
    }

    public void NavigateToMain()
    {
        SetNavActive(NavDashboard);
        var vm = new MainViewModel(_player!, _token, NavigateToLogin)
        {
            NavigateToServersCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(NavigateToServerList)
        };
        SetContent(new MainView { DataContext = vm });
    }

    public void NavigateToServerList()
    {
        SetNavActive(NavServers);
        var vm = new ServerListViewModel(_api, _token, _player?.DisplayName ?? "Player");
        vm.OnServerJoined = msg => Dispatcher.Invoke(() => Toast.Show(msg));
        SetContent(new ServerListView { DataContext = vm });
    }
    
    public void NavigateToLiveStream()
    {
        SetNavActive(NavLiveStream);
        SetContent(new LiveStreamView());
    }
    
    public void NavigateToSettings()
    {
        SetNavActive(NavSettings);
        SetContent(new SettingsView { DataContext = new SettingsViewModel() });
    }

    private void NavIntro_Click(object sender, RoutedEventArgs e)
    { if (_player != null) NavigateToIntro(); }

    private void NavDashboard_Click(object sender, RoutedEventArgs e)
    { if (_player != null) NavigateToMain(); }

    private void NavServers_Click(object sender, RoutedEventArgs e)
    { if (_player != null) NavigateToServerList(); }
    
    private void NavLiveStream_Click(object sender, RoutedEventArgs e)
    { if (_player != null) NavigateToLiveStream(); }
    
    private void NavSettings_Click(object sender, RoutedEventArgs e)
    { NavigateToSettings(); }

    // ── Sidebar Highlight ─────────────────────────────────────────────────
    private void SetNavActive(Button? active)
    {
        IntroIndicator.Visibility     = NavIntro     == active ? Visibility.Visible : Visibility.Hidden;
        DashboardIndicator.Visibility = NavDashboard == active ? Visibility.Visible : Visibility.Hidden;
        ServersIndicator.Visibility   = NavServers   == active ? Visibility.Visible : Visibility.Hidden;
        LiveStreamIndicator.Visibility= NavLiveStream== active ? Visibility.Visible : Visibility.Hidden;
        SettingsIndicator.Visibility  = NavSettings  == active ? Visibility.Visible : Visibility.Hidden;
        
        var activeBrush = (System.Windows.Media.Brush)FindResource("TextPrimaryBrush");
        var inactiveBrush = (System.Windows.Media.Brush)FindResource("TextMutedBrush");

        if (NavIntro.Content is Grid ig && ig.Children.Count > 2 && ig.Children[2] is TextBlock it)
            it.Foreground = NavIntro == active ? activeBrush : inactiveBrush;
        if (NavDashboard.Content is Grid dg && dg.Children.Count > 2 && dg.Children[2] is TextBlock dt)
            dt.Foreground = NavDashboard == active ? activeBrush : inactiveBrush;
        if (NavServers.Content is Grid sg && sg.Children.Count > 2 && sg.Children[2] is TextBlock st)
            st.Foreground = NavServers == active ? activeBrush : inactiveBrush;
        if (NavLiveStream.Content is Grid lg && lg.Children.Count > 2 && lg.Children[2] is TextBlock lt)
            lt.Foreground = NavLiveStream == active ? activeBrush : inactiveBrush;
        if (NavSettings.Content is Grid seg && seg.Children.Count > 2 && seg.Children[2] is TextBlock set)
            set.Foreground = NavSettings == active ? activeBrush : inactiveBrush;
    }

    public void ShowToast(string msg, bool err = false) => Toast.Show(msg, err);

    // ── Win32 Borderless Window Maximize Fix ──────────────────────────────
    [StructLayout(LayoutKind.Sequential)]
    public struct POINT { public int x; public int y; }

    [StructLayout(LayoutKind.Sequential)]
    public struct MINMAXINFO { public POINT ptReserved; public POINT ptMaxSize; public POINT ptMaxPosition; public POINT ptMinTrackSize; public POINT ptMaxTrackSize; }

    [StructLayout(LayoutKind.Sequential)]
    public struct MONITORINFO { public int cbSize; public RECT rcMonitor; public RECT rcWork; public uint dwFlags; }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int left; public int top; public int right; public int bottom; }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        DwmHelper.ApplyNativeWindows11Styles(this);
        
        var handle = new WindowInteropHelper(this).Handle;
        HwndSource.FromHwnd(handle)?.AddHook(WindowProc);
    }

    private IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == 0x0024) // WM_GETMINMAXINFO
        {
            var mmi = (MINMAXINFO)Marshal.PtrToStructure(lParam, typeof(MINMAXINFO))!;
            IntPtr monitor = MonitorFromWindow(hwnd, 2); // MONITOR_DEFAULTTONEAREST
            if (monitor != IntPtr.Zero)
            {
                var monitorInfo = new MONITORINFO();
                monitorInfo.cbSize = Marshal.SizeOf(typeof(MONITORINFO));
                if (GetMonitorInfo(monitor, ref monitorInfo))
                {
                    RECT rcWorkArea = monitorInfo.rcWork;
                    RECT rcMonitorArea = monitorInfo.rcMonitor;
                    
                    mmi.ptMaxPosition.x = Math.Abs(rcWorkArea.left - rcMonitorArea.left);
                    mmi.ptMaxPosition.y = Math.Abs(rcWorkArea.top - rcMonitorArea.top);
                    mmi.ptMaxSize.x = Math.Abs(rcWorkArea.right - rcWorkArea.left);
                    mmi.ptMaxSize.y = Math.Abs(rcWorkArea.bottom - rcWorkArea.top);
                    
                    Marshal.StructureToPtr(mmi, lParam, true);
                    handled = true;
                }
            }
        }
        return IntPtr.Zero;
    }
}
