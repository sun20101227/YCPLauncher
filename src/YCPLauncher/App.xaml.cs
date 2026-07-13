using System.Windows;
using System.Reflection;

// Resolve WPF/WinForms ambiguity
using MessageBox = System.Windows.MessageBox;

namespace YCPLauncher;

public partial class App : System.Windows.Application
{
    public static string CurrentVersion { get; } = GetCurrentVersion();

    private static System.Threading.Mutex? _mutex;
    private static volatile bool _isShuttingDown;

    private static string GetCurrentVersion()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        return version == null
            ? "1.1.8"
            : $"{version.Major}.{version.Minor}.{version.Build}";
    }

    private static System.Threading.EventWaitHandle? _wakeUpHandle;

    protected override void OnStartup(StartupEventArgs e)
    {
        const string appName = "YCPLauncher_SingleInstance_Mutex";
        bool createdNew;
        _mutex = new System.Threading.Mutex(true, appName, out createdNew);

        if (!createdNew)
        {
            // Try to wake up the existing instance
            try
            {
                var evt = System.Threading.EventWaitHandle.OpenExisting("YCPLauncher_WakeUpEvent");
                evt.Set();
            }
            catch { }
            
            System.Windows.Application.Current.Shutdown();
            return;
        }

        // First instance creates the event
        _wakeUpHandle = new System.Threading.EventWaitHandle(false, System.Threading.EventResetMode.AutoReset, "YCPLauncher_WakeUpEvent");
        
        // Listen for wake-up signals in a background thread
        System.Threading.Tasks.Task.Run(() =>
        {
            while (!_isShuttingDown)
            {
                try
                {
                    _wakeUpHandle.WaitOne();
                }
                catch (ObjectDisposedException)
                {
                    break;
                }

                if (_isShuttingDown)
                    break;

                System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    if (_isShuttingDown)
                        return;

                    var mainWindow = System.Windows.Application.Current.MainWindow;
                    if (mainWindow != null)
                    {
                        mainWindow.Show();
                        if (mainWindow.WindowState == WindowState.Minimized)
                        {
                            mainWindow.WindowState = WindowState.Normal;
                        }
                        mainWindow.Activate();
                        mainWindow.Topmost = true;
                        mainWindow.Topmost = false;
                        mainWindow.Focus();
                    }
                });
            }
        });

        // Apply saved theme at startup
        var cfg = Services.ConfigService.GetConfig();
        string themePath = cfg.IsDarkMode ? "Themes/DarkTheme.xaml" : "Themes/LightTheme.xaml";
        var uri = new Uri(themePath, UriKind.Relative);
        var dict = new ResourceDictionary { Source = uri };
        if (System.Windows.Application.Current.Resources.MergedDictionaries.Count > 0)
        {
            System.Windows.Application.Current.Resources.MergedDictionaries[0] = dict;
        }

        base.OnStartup(e);

        DispatcherUnhandledException += (_, ex) =>
        {
            var msg = BuildExceptionMessage(ex.Exception);
            MessageBox.Show(msg, "未处理的错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
            ex.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (_, ex) =>
        {
            var msg = BuildExceptionMessage(ex.ExceptionObject as Exception);
            MessageBox.Show(msg, "应用程序错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        };

        // Manually show the main window to prevent 2nd instance flashes
        var splash = new Views.SplashWindow();
        splash.Show();
        splash.Activate();
        splash.Topmost = true;
        splash.Topmost = false;
        splash.Focus();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _isShuttingDown = true;
        Services.DashboardCacheService.Instance.StopPolling();

        try { _wakeUpHandle?.Set(); } catch { }
        _wakeUpHandle?.Dispose();
        _wakeUpHandle = null;

        try { _mutex?.ReleaseMutex(); } catch { }
        _mutex?.Dispose();
        _mutex = null;

        base.OnExit(e);
    }

    private static string BuildExceptionMessage(Exception? ex)
    {
        if (ex == null) return "未知错误";
        var sb = new System.Text.StringBuilder();
        var current = ex;
        int level = 0;
        while (current != null)
        {
            if (level == 0)
                sb.AppendLine($"[错误] {current.GetType().Name}");
            else
                sb.AppendLine($"[内层 {level}] {current.GetType().Name}");
            sb.AppendLine(current.Message);
            sb.AppendLine();
            current = current.InnerException;
            level++;
        }
        return sb.ToString();
    }
}
