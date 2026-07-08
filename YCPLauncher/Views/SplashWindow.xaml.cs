using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;

namespace YCPLauncher.Views;

public partial class SplashWindow : Window
{
    public SplashWindow()
    {
        InitializeComponent();
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        var intro = (Storyboard)FindResource("IntroAnimation");
        intro.Begin();

        // Wait for 2 seconds (intro is 2.5s but we start fade out a bit early for smoothness)
        await Task.Delay(2000);

        var outro = (Storyboard)FindResource("OutroAnimation");
        outro.Begin();
    }

    private void OutroAnimation_Completed(object sender, EventArgs e)
    {
        // When outro is done, show the main window and close this splash window
        var mainWindow = new MainWindow();
        System.Windows.Application.Current.MainWindow = mainWindow;
        mainWindow.Show();
        this.Close();
    }
}
