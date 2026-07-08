using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;
using YCPLauncher.Models;
using YCPLauncher.Services;

namespace YCPLauncher.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly ApiService _api;
    private readonly Action<PlayerInfo, string> _onLoginSuccess;

    [ObservableProperty] private string _username = string.Empty;
    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool   _isLoading = false;
    [ObservableProperty] private bool   _hasError  = false;

    public LoginViewModel(ApiService api, Action<PlayerInfo, string> onLoginSuccess)
    {
        _api = api;
        _onLoginSuccess = onLoginSuccess;

        // Pre-fill last used username
        var last = AuthService.LoadLastUsername();
        if (!string.IsNullOrEmpty(last)) Username = last;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "请输入账号和密码";
            HasError = true;
            return;
        }

        IsLoading = true;
        HasError  = false;
        ErrorMessage = string.Empty;

        try
        {
            var result = await _api.LoginAsync(Username.Trim(), Password);
            if (result.Error != null)
            {
                ErrorMessage = result.Error;
                HasError = true;
            }
            else if (result.Token != null && result.Player != null)
            {
                AuthService.SaveToken(result.Token);
                AuthService.SavePlayer(result.Player);
                AuthService.SaveLastUsername(Username.Trim());
                _onLoginSuccess(result.Player, result.Token);
            }
            else
            {
                ErrorMessage = "登录失败，请重试";
                HasError = true;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"网络错误：{ex.Message}";
            HasError = true;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
