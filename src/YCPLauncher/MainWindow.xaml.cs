using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using YCPLauncher.Models;
using YCPLauncher.Services;
using YCPLauncher.ViewModels;
using YCPLauncher.Views;
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
    private bool _ambientStartupReady;
    private bool _ambientMediaAvailable = true;
    private bool _ambientHasStarted;
    private bool _ambientIsPlaying;
    private bool _isInteractiveResize;
    private bool _isClosed;
    private bool _foregroundVideoActive;
    private bool _restoreIntroAfterBackground;

    private static readonly QuadraticEase ContentFadeEase = CreateContentFadeEase();

    public MainWindow()
    {
        InitializeComponent();
        TxtVersion.Text = App.CurrentVersion;
        TryAutoLogin();
        Loaded += MainWindow_Loaded;
        IsVisibleChanged += MainWindow_IsVisibleChanged;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Apply the mask immediately, but defer decoding until the initial UI work is idle.
        SetAmbientVisualsEnabled(ConfigService.GetConfig().IsDarkMode);
        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
        if (_isClosed) return;

        _ambientStartupReady = true;
        UpdateAmbientPlaybackState();
        _ = CheckForUpdatesOnStartupAsync();
    }

    public void SetAmbientVisualsEnabled(bool isDarkMode)
    {
        // The mask and video opacity are DynamicResources owned by each theme.
        _ = isDarkMode;

        if (_ambientMediaAvailable)
            AmbientVideo.Visibility = Visibility.Visible;

        UpdateAmbientPlaybackState();
    }

    private bool ShouldPlayAmbientVideo()
    {
        return _ambientStartupReady
            && _ambientMediaAvailable
            && IsLoaded
            && IsVisible
            && WindowState != WindowState.Minimized
            && !_isInteractiveResize
            && !_foregroundVideoActive
            && ConfigService.AreAnimationsEnabled;
    }

    private void UpdateAmbientPlaybackState()
    {
        if (_isClosed || !IsLoaded || !_ambientMediaAvailable)
            return;

        if (ShouldPlayAmbientVideo())
        {
            if (_ambientIsPlaying)
                return;

            AmbientVideo.Play();
            _ambientHasStarted = true;
            _ambientIsPlaying = true;
            return;
        }

        PauseAmbientVideo();
    }

    private void PauseAmbientVideo()
    {
        if (!_ambientHasStarted || !_ambientIsPlaying)
            return;

        AmbientVideo.Pause();
        _ambientIsPlaying = false;
    }

    private void AmbientVideo_MediaOpened(object sender, RoutedEventArgs e)
    {
        // The setting or window state can change while MediaElement opens asynchronously.
        // Enforce Pause again here because a command issued before MediaOpened is not a
        // reliable indication that the decoder stayed suspended.
        if (!ShouldPlayAmbientVideo())
        {
            AmbientVideo.Pause();
            _ambientIsPlaying = false;
            return;
        }

        UpdateAmbientPlaybackState();
    }

    private void AmbientVideo_MediaEnded(object sender, RoutedEventArgs e)
    {
        _ambientIsPlaying = false;
        AmbientVideo.Position = TimeSpan.Zero;
        UpdateAmbientPlaybackState();
    }

    private void AmbientVideo_MediaFailed(object sender, ExceptionRoutedEventArgs e)
    {
        _ambientMediaAvailable = false;
        _ambientIsPlaying = false;
        AmbientVideo.Visibility = Visibility.Collapsed;
    }

    private void MainWindow_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        UpdateAmbientPlaybackState();
        UpdateForegroundPlaybackState();
    }

    private static QuadraticEase CreateContentFadeEase()
    {
        var easing = new QuadraticEase { EasingMode = EasingMode.EaseOut };
        easing.Freeze();
        return easing;
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


    // ── Window Controls ──────────────────────────────────────────────────
    private void MinBtn_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void MaxBtn_Click(object sender, RoutedEventArgs e) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    private void CloseBtn_Click(object sender, RoutedEventArgs e) => Close();

    protected override void OnStateChanged(EventArgs e)
    {
        base.OnStateChanged(e);
        if (MaxIcon != null)
            MaxIcon.Text = WindowState == WindowState.Maximized ? "\xE923" : "\xE922";

        if (WindowState == WindowState.Minimized)
            TearDownForegroundVideoPage();

        UpdateAmbientPlaybackState();
        UpdateForegroundPlaybackState();
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
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();

        if (_restoreIntroAfterBackground && _player != null)
        {
            _restoreIntroAfterBackground = false;
            NavigateToIntro();
        }

        UpdateAmbientPlaybackState();
        UpdateForegroundPlaybackState();
    }

    private void ExitApp()
    { 
        _isExiting = true;
        MyNotifyIcon?.Dispose(); 
        Application.Current.Shutdown(); 
    }

    // ── Window Chrome ──────────────────────────────────────────────────────
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

    protected override void OnClosed(EventArgs e)
    {
        _isClosed = true;
        MainContent.Content = null;
        _ambientIsPlaying = false;
        AmbientVideo.Close();
        base.OnClosed(e);
    }

    private void HideToTray()
    {
        PauseAmbientVideo();
        TearDownForegroundVideoPage();
        Hide();
    }

    private void TearDownForegroundVideoPage()
    {
        if (MainContent.Content is not LiveStreamView liveStream)
            return;

        liveStream.SetPlaybackActive(false);
        MainContent.Content = null;
        _foregroundVideoActive = false;
        _restoreIntroAfterBackground = _player != null;
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
        // Never decode the ambient wallpaper and a foreground LibVLC stream at the
        // same time. Apart from wasting GPU/CPU, two independent video surfaces are
        // a common source of frame pacing issues on integrated graphics.
        _foregroundVideoActive = view is LiveStreamView;
        UpdateAmbientPlaybackState();

        MainContent.BeginAnimation(OpacityProperty, null);
        MainContent.Opacity = 1;
        MainContent.Content = view;
        UpdateForegroundPlaybackState();

        if (!ShouldAnimateContentTransition())
        {
            return;
        }

        // A single short compositor fade avoids the previous two-stage full-content
        // fade plus TranslateTransform while preserving a subtle navigation cue.
        var fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(140))
        {
            EasingFunction = ContentFadeEase,
            FillBehavior = FillBehavior.Stop
        };
        fade.Freeze();
        MainContent.BeginAnimation(
            OpacityProperty,
            fade,
            HandoffBehavior.SnapshotAndReplace);
    }

    private bool ShouldAnimateContentTransition()
    {
        return ConfigService.AreAnimationsEnabled
            && SystemParameters.ClientAreaAnimation
            && (RenderCapability.Tier >> 16) > 0
            && IsVisible
            && WindowState != WindowState.Minimized;
    }

    private void UpdateForegroundPlaybackState()
    {
        if (MainContent.Content is not LiveStreamView liveStream)
            return;

        liveStream.SetPlaybackActive(
            IsVisible &&
            WindowState != WindowState.Minimized &&
            !_isInteractiveResize &&
            !_isClosed);
    }

    public void NavigateToLogin()
    {
        _foregroundVideoActive = false;
        UpdateAmbientPlaybackState();
        DashboardCacheService.Instance.StopPolling();
        DashboardCacheService.Instance.ClearCache();
        _token = string.Empty; _player = null;
        SetNavActive(null);
        MainContent.Content = null;
        InnerGrid.Visibility = Visibility.Collapsed;
        LoginOverlay.Visibility = Visibility.Visible;
        LoginOverlay.Content = new LoginView { DataContext = new LoginViewModel(_api, OnLoginSuccess) };
    }

    public void NavigateToLoginSettings()
    {
        _foregroundVideoActive = false;
        UpdateAmbientPlaybackState();
        MainContent.Content = null;
        InnerGrid.Visibility = Visibility.Collapsed;
        LoginOverlay.Visibility = Visibility.Visible;
        LoginOverlay.Content = new SettingsView { DataContext = new SettingsViewModel() };
    }

    private void OnLoginSuccess(PlayerInfo player, string token)
    {
        _player = player; _token = token;
        DashboardCacheService.Instance.StartPolling(_token);
        LoginOverlay.Visibility = Visibility.Collapsed;
        LoginOverlay.Content = null;
        InnerGrid.Visibility = Visibility.Visible;
        
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
            NavigateToServersCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(NavigateToMatchmaking)
        };
        SetContent(new MainView { DataContext = vm });
    }


    
    public void NavigateToLiveStream()
    {
        SetNavActive(NavLiveStream);
        SetContent(new LiveStreamView());
    }
    
    public void NavigateToMatchmaking()
    {
        SetNavActive(NavMatchmaking);
        SetContent(new LobbyView());
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

    private void NavMatchmaking_Click(object sender, RoutedEventArgs e)
    { if (_player != null) NavigateToMatchmaking(); }

    private void NavLiveStream_Click(object sender, RoutedEventArgs e)
    { if (_player != null) NavigateToLiveStream(); }
    
    private void NavSettings_Click(object sender, RoutedEventArgs e)
    { NavigateToSettings(); }

    // ── Sidebar Highlight ─────────────────────────────────────────────────
    private void SetNavActive(Button? active)
    {
        IntroIndicator.Visibility     = NavIntro     == active ? Visibility.Visible : Visibility.Hidden;
        DashboardIndicator.Visibility = NavDashboard == active ? Visibility.Visible : Visibility.Hidden;
        MatchmakingIndicator.Visibility = NavMatchmaking == active ? Visibility.Visible : Visibility.Hidden;
        LiveStreamIndicator.Visibility= NavLiveStream== active ? Visibility.Visible : Visibility.Hidden;
        SettingsIndicator.Visibility  = NavSettings  == active ? Visibility.Visible : Visibility.Hidden;

        foreach (var button in new[] { NavIntro, NavDashboard, NavMatchmaking, NavLiveStream, NavSettings })
        {
            if (button == active)
            {
                button.SetResourceReference(Button.BackgroundProperty, "AccentBgBrush");
                button.SetResourceReference(Button.ForegroundProperty, "AccentBrush");
            }
            else
            {
                button.Background = Brushes.Transparent;
                button.SetResourceReference(Button.ForegroundProperty, "TextMutedBrush");
            }
        }
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
        DwmHelper.ApplyNativeWindows11Styles(this, Services.ConfigService.GetConfig().IsDarkMode);
        
        var handle = new WindowInteropHelper(this).Handle;
        HwndSource.FromHwnd(handle)?.AddHook(WindowProc);
    }

    private IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == 0x0231) // WM_ENTERSIZEMOVE
        {
            _isInteractiveResize = true;
            PauseAmbientVideo();
            UpdateForegroundPlaybackState();
        }
        else if (msg == 0x0232) // WM_EXITSIZEMOVE
        {
            _isInteractiveResize = false;
            Dispatcher.BeginInvoke(
                new Action(() =>
                {
                    UpdateAmbientPlaybackState();
                    UpdateForegroundPlaybackState();
                }),
                DispatcherPriority.ApplicationIdle);
        }

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
