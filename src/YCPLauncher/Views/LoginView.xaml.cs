using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using YCPLauncher.Services;
using YCPLauncher.ViewModels;

using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using UserControl  = System.Windows.Controls.UserControl;

namespace YCPLauncher.Views;

public partial class LoginView : UserControl
{
    private LoginViewModel? _vm;

    public LoginView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
        {
            _vm = DataContext as LoginViewModel;
        };
    }

    private void LoginView_Loaded(object sender, RoutedEventArgs e)
    {
        if (ConfigService.AreAnimationsEnabled)
        {
            var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
            LoginCard.BeginAnimation(
                OpacityProperty,
                new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200))
                {
                    EasingFunction = ease,
                    FillBehavior = FillBehavior.Stop
                });
            LoginCardTranslate.BeginAnimation(
                System.Windows.Media.TranslateTransform.YProperty,
                new DoubleAnimation(8, 0, TimeSpan.FromMilliseconds(220))
                {
                    EasingFunction = ease,
                    FillBehavior = FillBehavior.Stop
                });
            IdentityBlock.BeginAnimation(
                OpacityProperty,
                new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200))
                {
                    EasingFunction = ease,
                    FillBehavior = FillBehavior.Stop
                });
        }

        Dispatcher.BeginInvoke(
            DispatcherPriority.Input,
            new Action(() =>
            {
                if (string.IsNullOrWhiteSpace(UsernameBox.Text))
                    UsernameBox.Focus();
                else
                    PasswordBox.Focus();
            }));
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (_vm != null) _vm.Password = PasswordBox.Password;
    }

    private void Input_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Return) _vm?.LoginCommand.Execute(null);
    }

    private void Password_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Return) _vm?.LoginCommand.Execute(null);
    }

    private void Field_GotFocus(object sender, RoutedEventArgs e)
    {
        var parent = ((FrameworkElement)sender).Parent as Grid;
        if (parent?.Parent is Border border)
            border.SetResourceReference(Border.BorderBrushProperty, "AccentBrush");
    }

    private void Field_LostFocus(object sender, RoutedEventArgs e)
    {
        var parent = ((FrameworkElement)sender).Parent as Grid;
        if (parent?.Parent is Border border)
            border.SetResourceReference(Border.BorderBrushProperty, "BorderBrush2");
    }

    private void OpenSettings_Click(object sender, RoutedEventArgs e)
    {
        if (System.Windows.Application.Current.MainWindow is MainWindow mw)
        {
            mw.NavigateToLoginSettings();
        }
    }
}
