using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using YCPLauncher.ViewModels;

using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using UserControl  = System.Windows.Controls.UserControl;
using Color        = System.Windows.Media.Color;

namespace YCPLauncher.Views;

public partial class LoginView : UserControl
{
    private LoginViewModel? _vm;

    private static readonly SolidColorBrush FocusBrush =
        new(Color.FromRgb(0xFF, 0x6B, 0x00));
    private static readonly SolidColorBrush NormalBrush =
        new(Color.FromRgb(0x30, 0x36, 0x3D));

    public LoginView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
        {
            _vm = DataContext as LoginViewModel;
        };
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
}
