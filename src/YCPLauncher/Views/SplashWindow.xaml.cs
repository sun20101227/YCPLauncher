using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;

namespace YCPLauncher.Views;

public partial class SplashWindow : Window
{
    private MainWindow? _mainWindow;

    public SplashWindow()
    {
        InitializeComponent();
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        var intro = (Storyboard)FindResource("IntroAnimation");
        intro.Begin();

        // Construct the main window while the splash is visible instead of
        // waiting and only starting the real work afterwards.
        await Task.Delay(80);
        _mainWindow = new MainWindow();
        await Task.Delay(420);

        var outro = (Storyboard)FindResource("OutroAnimation");
        outro.Begin();
    }

    private void OutroAnimation_Completed(object sender, EventArgs e)
    {
        var mainWindow = _mainWindow ?? new MainWindow();
        System.Windows.Application.Current.MainWindow = mainWindow;
        mainWindow.Show();
        mainWindow.Activate();
        mainWindow.Topmost = true;
        mainWindow.Topmost = false;
        mainWindow.Focus();
        this.Close();
    }
}
