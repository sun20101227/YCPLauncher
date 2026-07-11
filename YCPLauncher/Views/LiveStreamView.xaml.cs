using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using LibVLCSharp.Shared;
using YCPLauncher.Services;

namespace YCPLauncher.Views;

public partial class LiveStreamView : System.Windows.Controls.UserControl
{
    private LibVLC? _libVLC;
    private MediaPlayer? _mediaPlayer;
    private CancellationTokenSource? _timeoutCts;

    public LiveStreamView()
    {
        InitializeComponent();
        InitializeVlc();
    }

    private async void InitializeVlc()
    {
        try
        {
            var (libVlc, player) = await Task.Run(() =>
            {
                Core.Initialize();
                var lv = new LibVLC();
                var mp = new MediaPlayer(lv);
                return (lv, mp);
            });

            _libVLC = libVlc;
            _mediaPlayer = player;
            VideoPlayer.MediaPlayer = _mediaPlayer;

            StartStream();
        }
        catch (Exception ex)
        {
            Debug.WriteLine("VLC initialization failed: " + ex.Message);
            Dispatcher.Invoke(() => ShowFailed());
        }
    }

    private void StartStream()
    {
        if (_libVLC == null || _mediaPlayer == null) return;

        // Reset UI state
        Dispatcher.Invoke(() =>
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            FailedOverlay.Visibility = Visibility.Collapsed;
            TimeoutHint.Text = "";
            IconPlayPause.Text = "\uE769";
        });

        // Cancel any existing timeout
        _timeoutCts?.Cancel();
        _timeoutCts = new CancellationTokenSource();
        var token = _timeoutCts.Token;

        var cfg = ConfigService.GetConfig();
        string streamUrl = string.IsNullOrWhiteSpace(cfg.LiveStreamUrl)
            ? "rtmp://frp-pen.com:48399/live/ycp"
            : cfg.LiveStreamUrl;

        _mediaPlayer.Stop();
        var media = new Media(_libVLC, new Uri(streamUrl));
        _mediaPlayer.Play(media);

        // Hide loading when stream actually starts playing
        _mediaPlayer.Playing += OnPlaying;

        // 30-second timeout countdown
        _ = Task.Run(async () =>
        {
            for (int i = 30; i > 0; i--)
            {
                if (token.IsCancellationRequested) return;
                int sec = i;
                Dispatcher.Invoke(() => TimeoutHint.Text = $"连接超时倒计时 {sec} 秒");
                await Task.Delay(1000, CancellationToken.None);
            }

            if (!token.IsCancellationRequested)
            {
                // 30s passed, stop and show failed
                _mediaPlayer?.Stop();
                Dispatcher.Invoke(() => ShowFailed());
            }
        });
    }

    private void OnPlaying(object? sender, EventArgs e)
    {
        // Successfully connected – cancel timeout and hide overlay
        _timeoutCts?.Cancel();
        if (_mediaPlayer != null)
            _mediaPlayer.Playing -= OnPlaying;

        Dispatcher.Invoke(() =>
        {
            LoadingOverlay.Visibility = Visibility.Collapsed;
            FailedOverlay.Visibility = Visibility.Collapsed;
        });
    }

    private void ShowFailed()
    {
        LoadingOverlay.Visibility = Visibility.Collapsed;
        FailedOverlay.Visibility = Visibility.Visible;
    }

    private void BtnRetry_Click(object sender, RoutedEventArgs e)
    {
        // User manually clicked retry – attempt once again
        StartStream();
    }

    private void BtnPlayPause_Click(object sender, RoutedEventArgs e)
    {
        if (_mediaPlayer == null) return;
        if (_mediaPlayer.IsPlaying)
        {
            _mediaPlayer.Pause();
            IconPlayPause.Text = "\uE768";
        }
        else
        {
            _mediaPlayer.Play();
            IconPlayPause.Text = "\uE769";
        }
    }

    private void BtnRefresh_Click(object sender, RoutedEventArgs e)
    {
        StartStream();
    }

    private void SldVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_mediaPlayer != null)
            _mediaPlayer.Volume = (int)e.NewValue;
    }

    private void UserControl_Unloaded(object sender, RoutedEventArgs e)
    {
        _timeoutCts?.Cancel();
        if (_mediaPlayer != null)
        {
            _mediaPlayer.Playing -= OnPlaying;
            _mediaPlayer.Stop();
            _mediaPlayer.Dispose();
            _mediaPlayer = null;
        }
        _libVLC?.Dispose();
        _libVLC = null;
    }
}
