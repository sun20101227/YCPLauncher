using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;

namespace YCPInstaller
{
    public partial class InstallerSplashWindow : Window
    {
        public InstallerSplashWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Storyboard intro = (Storyboard)FindResource("IntroAnimation");
            intro.Begin();
        }

        private void IntroAnimation_Completed(object sender, EventArgs e)
        {
            // After intro finishes, wait a little bit then play outro
            Task.Delay(500).ContinueWith(t =>
            {
                Dispatcher.Invoke(() =>
                {
                    Storyboard outro = (Storyboard)FindResource("OutroAnimation");
                    outro.Begin();
                });
            });
        }

        private void OutroAnimation_Completed(object sender, EventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }
    }
}
