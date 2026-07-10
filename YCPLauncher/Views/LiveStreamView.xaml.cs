using System;
using System.Diagnostics;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using LibVLCSharp.Shared;
using YCPLauncher.Services;

namespace YCPLauncher.Views;

public partial class LiveStreamView : System.Windows.Controls.UserControl
{
    private LibVLC? _libVLC;
    private MediaPlayer? _mediaPlayer;

    public LiveStreamView()
    {
        InitializeComponent();
        InitializeWebViewAsync();
        InitializeVlc();
    }

    private void InitializeVlc()
    {
        try
        {
            Core.Initialize();
            _libVLC = new LibVLC();
            _mediaPlayer = new MediaPlayer(_libVLC);
            VideoPlayer.MediaPlayer = _mediaPlayer;

            var cfg = ConfigService.GetConfig();
            string streamUrl = string.IsNullOrWhiteSpace(cfg.LiveStreamUrl) ? "rtmp://frp-pen.com:48399/live/ycp" : cfg.LiveStreamUrl;

            // Start playing RTMP stream
            var media = new Media(_libVLC, new Uri(streamUrl));
            _mediaPlayer.Play(media);

            _mediaPlayer.Playing += (s, e) =>
            {
                Dispatcher.Invoke(() => LoadingOverlay.Visibility = Visibility.Collapsed);
            };
        }
        catch (Exception ex)
        {
            Debug.WriteLine("VLC initialization failed: " + ex.Message);
        }
    }

    private async void InitializeWebViewAsync()
    {
        try
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string userDataFolder = System.IO.Path.Combine(appData, "YCPLauncher", "WebView2Data");
            var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
            await ChatWebView.EnsureCoreWebView2Async(env);
            
            var cfg = ConfigService.GetConfig();
            string chatUrl = string.IsNullOrWhiteSpace(cfg.ChatUrl) ? "https://huyoutalk.mihuyou.online/ycp2026" : cfg.ChatUrl;
            
            ChatWebView.CoreWebView2.Navigate(chatUrl);
        }
        catch (Exception ex)
        {
            Debug.WriteLine("WebView2 initialization failed: " + ex.Message);
        }
    }

    private void UserControl_Unloaded(object sender, RoutedEventArgs e)
    {
        if (_mediaPlayer != null)
        {
            _mediaPlayer.Stop();
            _mediaPlayer.Dispose();
            _mediaPlayer = null;
        }
        if (_libVLC != null)
        {
            _libVLC.Dispose();
            _libVLC = null;
        }
        ChatWebView?.Dispose();
    }
}
