using System.Windows;

// Resolve WPF/WinForms ambiguity
using MessageBox = System.Windows.MessageBox;

namespace YCPLauncher;

public partial class App : System.Windows.Application
{
    public static readonly string CurrentVersion = "1.1.2";

    private static System.Threading.Mutex? _mutex;

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    protected override void OnStartup(StartupEventArgs e)
    {
        const string appName = "YCPLauncher_SingleInstance_Mutex";
        bool createdNew;
        _mutex = new System.Threading.Mutex(true, appName, out createdNew);

        if (!createdNew)
        {
            var currentProcess = System.Diagnostics.Process.GetCurrentProcess();
            foreach (var process in System.Diagnostics.Process.GetProcessesByName(currentProcess.ProcessName))
            {
                if (process.Id != currentProcess.Id)
                {
                    IntPtr handle = process.MainWindowHandle;
                    if (handle != IntPtr.Zero)
                    {
                        ShowWindow(handle, 9); // SW_RESTORE
                        SetForegroundWindow(handle);
                    }
                    break;
                }
            }
            System.Windows.Application.Current.Shutdown();
            return;
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
