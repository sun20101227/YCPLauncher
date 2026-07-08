using System.Windows;

// Resolve WPF/WinForms ambiguity
using MessageBox = System.Windows.MessageBox;

namespace YCPLauncher;

public partial class App : System.Windows.Application
{
    public static readonly string CurrentVersion = "0.0.10 Beta";

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Show full inner exception chain for diagnosis
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
