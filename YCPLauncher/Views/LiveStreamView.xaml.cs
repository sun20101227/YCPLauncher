using System;
using System.Diagnostics;
using System.Windows;
using Microsoft.Web.WebView2.Core;

namespace YCPLauncher.Views;

public partial class LiveStreamView : System.Windows.Controls.UserControl
{
    public LiveStreamView()
    {
        InitializeComponent();
        InitializeWebViewAsync();
    }

    private async void InitializeWebViewAsync()
    {
        try
        {
            var env = await CoreWebView2Environment.CreateAsync(null, System.IO.Path.Combine(System.IO.Path.GetTempPath(), "YCPLauncher_WebView"));
            await PlayerWebView.EnsureCoreWebView2Async(env);
            
            PlayerWebView.CoreWebView2.Navigate("https://ycp.yachiyo8000.cn/live.php");
            
            PlayerWebView.NavigationCompleted += (s, e) =>
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            };
        }
        catch (Exception ex)
        {
            Debug.WriteLine("WebView2 initialization failed: " + ex.Message);
        }
    }
}
