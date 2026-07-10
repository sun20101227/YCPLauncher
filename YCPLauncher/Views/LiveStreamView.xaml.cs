using System;
using System.Diagnostics;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using LibVLCSharp.Shared;

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

            // Start playing RTMP stream
            var media = new Media(_libVLC, new Uri("rtmp://frp-pen.com:48399/live/ycp"));
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
            
            // Navigate to the correct Huyoutalk or chat page
            // Based on user feedback, the previous /chat/ URL was returning 404.
            // Using the original live.php or a configurable URL.
            ChatWebView.CoreWebView2.Navigate("https://ycp.yachiyo8000.cn/live.php");
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
