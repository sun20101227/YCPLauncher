using System.Windows.Controls;
using System.Windows.Input;
using YCPLauncher.ViewModels;

// Explicitly use WPF's KeyEventArgs to avoid ambiguity with System.Windows.Forms
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace YCPLauncher.Views;

public partial class LoginPage : Page
{
    private LoginViewModel? _vm;

    public LoginPage()
    {
        InitializeComponent();
        DataContextChanged += (s, e) =>
        {
            _vm = DataContext as LoginViewModel;
            PasswordBox.PasswordChanged += (_, _) =>
            {
                if (_vm != null)
                    _vm.Password = PasswordBox.Password;
            };
        };
    }

    private void Input_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Return) _vm?.LoginCommand.Execute(null);
    }

    private void Password_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Return) _vm?.LoginCommand.Execute(null);
    }
}
