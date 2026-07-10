using System.Windows;
using System.Windows.Controls;
using YCPLauncher.ViewModels;

namespace YCPLauncher.Views;
public partial class SettingsView : System.Windows.Controls.UserControl
{
    private bool _isUpdating = false;
    private const string DUMMY_PWD = "••••••••••••••••••••";

    public SettingsView()
    {
        InitializeComponent();
        this.Loaded += SettingsView_Loaded;
        
        ApiPwd.GotFocus += Pwd_GotFocus;
        ApiPwd.LostFocus += Pwd_LostFocus;
        StreamPwd.GotFocus += Pwd_GotFocus;
        StreamPwd.LostFocus += Pwd_LostFocus;
        ChatPwd.GotFocus += Pwd_GotFocus;
        ChatPwd.LostFocus += Pwd_LostFocus;
    }

    private void SettingsView_Loaded(object sender, RoutedEventArgs e)
    {
        if (this.DataContext is SettingsViewModel vm)
        {
            _isUpdating = true;
            ApiPwd.Password = DUMMY_PWD;
            StreamPwd.Password = DUMMY_PWD;
            ChatPwd.Password = DUMMY_PWD;
            _isUpdating = false;
        }
    }

    private void Pwd_GotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is PasswordBox pb)
        {
            if (pb.Password == DUMMY_PWD)
            {
                _isUpdating = true;
                pb.Password = "";
                _isUpdating = false;
            }
        }
    }

    private void Pwd_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is PasswordBox pb)
        {
            if (string.IsNullOrWhiteSpace(pb.Password))
            {
                _isUpdating = true;
                pb.Password = DUMMY_PWD;
                _isUpdating = false;
            }
        }
    }

    private void ApiPwd_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (_isUpdating) return;
        if (ApiPwd.Password != DUMMY_PWD && this.DataContext is SettingsViewModel vm)
        {
            vm.ApiBaseUrl = ApiPwd.Password;
        }
    }

    private void StreamPwd_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (_isUpdating) return;
        if (StreamPwd.Password != DUMMY_PWD && this.DataContext is SettingsViewModel vm)
        {
            vm.LiveStreamUrl = StreamPwd.Password;
        }
    }

    private void ChatPwd_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (_isUpdating) return;
        if (ChatPwd.Password != DUMMY_PWD && this.DataContext is SettingsViewModel vm)
        {
            vm.ChatUrl = ChatPwd.Password;
        }
    }
}
