using System;
using System.Threading.Tasks;
using System.Windows;

namespace YCPLauncher.Views;

public partial class LaunchLoadingDialog : Window
{
    public LaunchLoadingDialog()
    {
        InitializeComponent();
    }

    public static async void ShowAndAutoClose(int delayMs = 5000)
    {
        try
        {
            var dialog = new LaunchLoadingDialog();
            dialog.Show();
            await Task.Delay(delayMs);
            dialog.Close();
        }
        catch { }
    }
}
