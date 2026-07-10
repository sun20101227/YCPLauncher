using System.Windows;
using YCPLauncher.ViewModels;

namespace YCPLauncher.Views;
public partial class SettingsView : System.Windows.Controls.UserControl
{
    private bool _isUpdating = false;

    public SettingsView()
    {
        InitializeComponent();
        this.Loaded += SettingsView_Loaded;
    }

    private void SettingsView_Loaded(object sender, RoutedEventArgs e)
    {
        if (this.DataContext is SettingsViewModel vm)
        {
            _isUpdating = true;
            ApiPwd.Password = vm.ApiBaseUrl;
            StreamPwd.Password = vm.LiveStreamUrl;
            ChatPwd.Password = vm.ChatUrl;
            _isUpdating = false;
        }
    }

    private void ApiPwd_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (_isUpdating) return;
        if (this.DataContext is SettingsViewModel vm)
        {
            vm.ApiBaseUrl = ApiPwd.Password;
        }
    }

    private void StreamPwd_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (_isUpdating) return;
        if (this.DataContext is SettingsViewModel vm)
        {
            vm.LiveStreamUrl = StreamPwd.Password;
        }
    }

    private void ChatPwd_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (_isUpdating) return;
        if (this.DataContext is SettingsViewModel vm)
        {
            vm.ChatUrl = ChatPwd.Password;
        }
    }
}
