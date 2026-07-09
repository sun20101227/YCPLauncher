using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using YCPLauncher.Services;

namespace YCPLauncher.Views;

public partial class ChangePasswordDialog : Window
{
    private bool _isClosing = false;
    private readonly ApiService _apiService;
    private readonly string _token;

    public bool PasswordChanged { get; private set; } = false;

    public ChangePasswordDialog(string token)
    {
        InitializeComponent();
        _token = token;
        _apiService = new ApiService();
    }

    private async void BtnConfirm_Click(object sender, RoutedEventArgs e)
    {
        string oldPassword = TxtOldPassword.Password;
        string newPassword = TxtNewPassword.Password;

        if (string.IsNullOrWhiteSpace(oldPassword) || string.IsNullOrWhiteSpace(newPassword))
        {
            ShowError("密码不能为空");
            return;
        }

        if (newPassword.Length < 6)
        {
            ShowError("新密码长度不能少于 6 位");
            return;
        }

        BtnConfirm.IsEnabled = false;
        BtnConfirm.Content = "提交中...";
        TxtError.Visibility = Visibility.Collapsed;

        var result = await _apiService.ChangePasswordAsync(oldPassword, newPassword, _token);

        if (result != null && result.IsSuccess)
        {
            PasswordChanged = true;
            Close();
        }
        else
        {
            ShowError(result?.Message ?? "请求失败");
            BtnConfirm.IsEnabled = true;
            BtnConfirm.Content = "确认修改";
        }
    }

    private void ShowError(string message)
    {
        TxtError.Text = message;
        TxtError.Visibility = Visibility.Visible;
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        if (_isClosing) return;
        e.Cancel = true;
        _isClosing = true;

        var anim = new System.Windows.Media.Animation.DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.2));
        anim.Completed += (s, ev) => this.Close();
        this.BeginAnimation(Window.OpacityProperty, anim);
    }
}
