using System;
using System.Threading.Tasks;
using System.Windows;

namespace YCPLauncher.Views;

public partial class TerminalLaunchDialog : Window
{
    private bool _isClosed = false;

    public TerminalLaunchDialog()
    {
        InitializeComponent();
    }

    public async Task RunLogsAsync(string[] logs)
    {
        try
        {
            foreach (var log in logs)
            {
                if (_isClosed) break;

                // Typewriter effect for each line
                for (int i = 0; i <= log.Length; i++)
                {
                    if (_isClosed) break;
                    LogText.Text = LogText.Text.Substring(0, LogText.Text.Length - (LogText.Text.EndsWith("_") ? 1 : 0));
                    LogText.Text += log.Substring(i, 1 == 0 ? 0 : 1) + "_";
                    LogScroll.ScrollToEnd();
                    
                    if (i < log.Length)
                        await Task.Delay(10); // typing speed
                }

                // Remove cursor and add newline
                LogText.Text = LogText.Text.Substring(0, LogText.Text.Length - 1) + "\n";
                LogScroll.ScrollToEnd();

                await Task.Delay(300); // Wait between lines
            }

            if (!_isClosed)
            {
                await Task.Delay(1500); // Wait a bit before closing
                Close();
            }
        }
        catch { }
    }

    protected override void OnClosed(EventArgs e)
    {
        _isClosed = true;
        base.OnClosed(e);
    }
}
