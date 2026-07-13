using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;

namespace YCPInstaller
{
    public partial class InstallerSplashWindow : Window
    {
        private bool _silentMode;

        public InstallerSplashWindow(bool silentMode = false)
        {
            InitializeComponent();
            _silentMode = silentMode;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Storyboard intro = (Storyboard)FindResource("IntroAnimation");
            intro.Begin();
            
            _ = UpdateLoadingProgressAsync();
            
            await Task.Delay(2500);
            
            Storyboard outro = (Storyboard)FindResource("OutroAnimation");
            outro.Begin();
        }

        private async Task UpdateLoadingProgressAsync()
        {
            await Task.Delay(500);
            
            string prefix = _silentMode ? "Applying Update Packages [" : "Decrypting Installer Packages [";
            int totalBars = 20;
            int currentPercent = 0;
            
            int steps = 40;
            int delayPerStep = 1500 / steps;
            
            for (int i = 0; i <= steps; i++)
            {
                currentPercent = (int)((i / (double)steps) * 100);
                int currentBars = (int)((i / (double)steps) * totalBars);
                
                string bars = new string('=', currentBars) + (currentBars < totalBars ? ">" : "") + new string(' ', Math.Max(0, totalBars - currentBars - 1));
                if (currentBars == totalBars) bars = new string('=', totalBars);
                
                LoadingText.Text = $"{prefix}{bars}] {currentPercent}%";
                
                await Task.Delay(delayPerStep);
            }
        }

        private void IntroAnimation_Completed(object sender, EventArgs e)
        {
        }

        private void OutroAnimation_Completed(object sender, EventArgs e)
        {
            if (_silentMode)
            {
                ((App)System.Windows.Application.Current).PerformSilentInstall();
            }
            else
            {
                MainWindow mainWindow = new MainWindow();
                mainWindow.Show();
                this.Close();
            }
        }
    }
}
