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

        _ = UpdateLoadingProgressAsync();

        // Wait for 2.2 seconds to let the progress hit 100% and stay for a moment
        await Task.Delay(2200);

        var outro = (Storyboard)FindResource("OutroAnimation");
        outro.Begin();
    }

    private async Task UpdateLoadingProgressAsync()
    {
        await Task.Delay(500); // Wait for the text to fade in
        
        string prefix = "Connecting to Game Servers [";
        int totalBars = 20;
        int currentPercent = 0;
        
        // We have about 1.5 seconds to go from 0 to 100%
        int steps = 40;
        int delayPerStep = 1500 / steps;
        
        for (int i = 0; i <= steps; i++)
        {
            currentPercent = (int)((i / (double)steps) * 100);
            int currentBars = (int)((i / (double)steps) * totalBars);
            
            string bars = new string('=', currentBars) + (currentBars < totalBars ? ">" : "") + new string(' ', Math.Max(0, totalBars - currentBars - 1));
            if (currentBars == totalBars) bars = new string('=', totalBars); // Remove arrow when full
            
            LoadingText.Text = $"{prefix}{bars}] {currentPercent}%";
            
            await Task.Delay(delayPerStep);
        }
    }

    private void OutroAnimation_Completed(object sender, EventArgs e)
    {
        var mainWindow = new MainWindow();
        System.Windows.Application.Current.MainWindow = mainWindow;
        mainWindow.Show();
        this.Close();
    }
}
