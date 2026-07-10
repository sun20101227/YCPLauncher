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

    private async void InitializeVlc()
    {
        try
        {
            // Run heavy VLC init on background thread so UI doesn't freeze
            var (libVlc, player) = await Task.Run(() =>
            {
                Core.Initialize();
                var lv = new LibVLC();
                var mp = new MediaPlayer(lv);
                return (lv, mp);
            });

            _libVLC = libVlc;
            _mediaPlayer = player;

            // VideoPlayer.MediaPlayer must be set on UI thread
            VideoPlayer.MediaPlayer = _mediaPlayer;

            var cfg = ConfigService.GetConfig();
            string streamUrl = string.IsNullOrWhiteSpace(cfg.LiveStreamUrl)
                ? "rtmp://frp-pen.com:48399/live/ycp"
                : cfg.LiveStreamUrl;

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

    private void BtnPlayPause_Click(object sender, RoutedEventArgs e)
    {
        if (_mediaPlayer == null) return;
        if (_mediaPlayer.IsPlaying)
        {
            _mediaPlayer.Pause();
            IconPlayPause.Text = "\uE768"; // Play icon
        }
        else
        {
            _mediaPlayer.Play();
            IconPlayPause.Text = "\uE769"; // Pause icon
        }
    }

    private void BtnRefresh_Click(object sender, RoutedEventArgs e)
    {
        if (_mediaPlayer != null && _libVLC != null)
        {
            _mediaPlayer.Stop();
            LoadingOverlay.Visibility = Visibility.Visible;
            IconPlayPause.Text = "\uE769"; // Pause icon (since it auto-plays)
            var cfg = ConfigService.GetConfig();
            string streamUrl = string.IsNullOrWhiteSpace(cfg.LiveStreamUrl) ? "rtmp://frp-pen.com:48399/live/ycp" : cfg.LiveStreamUrl;
            var media = new Media(_libVLC, new Uri(streamUrl));
            _mediaPlayer.Play(media);
        }
    }

    private void SldVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_mediaPlayer != null)
        {
            _mediaPlayer.Volume = (int)e.NewValue;
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
