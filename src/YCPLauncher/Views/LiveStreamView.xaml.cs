using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using LibVLCSharp.Shared;
using YCPLauncher.Services;

namespace YCPLauncher.Views;

public partial class LiveStreamView : System.Windows.Controls.UserControl
{
    private const int ConnectionTimeoutSeconds = 30;

    private static readonly Lazy<LibVLC> SharedLibVlc = new(() =>
    {
        Core.Initialize();
        return new LibVLC("--no-video-title-show");
    }, LazyThreadSafetyMode.ExecutionAndPublication);

    private LibVLC? _libVLC;
    private MediaPlayer? _mediaPlayer;
    private Media? _currentMedia;
    private Task? _initializationTask;
    private CancellationTokenSource? _lifetimeCts;
    private CancellationTokenSource? _timeoutCts;
    private EventHandler<EventArgs>? _playingHandler;
    private EventHandler<EventArgs>? _errorHandler;
    private int _playbackGeneration;
    private bool _playbackActive = true;
    private bool _resumeWhenActivated;

    public LiveStreamView()
    {
        InitializeComponent();
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        await EnsureInitializedAsync();
    }

    private async Task EnsureInitializedAsync()
    {
        if (_mediaPlayer != null)
            return;

        if (_lifetimeCts is { IsCancellationRequested: false } &&
            _initializationTask is { IsCompleted: false } activeInitialization)
        {
            await activeInitialization;
            return;
        }

        _lifetimeCts?.Cancel();
        _lifetimeCts?.Dispose();
        _lifetimeCts = new CancellationTokenSource();
        var token = _lifetimeCts.Token;

        var initializationTask = InitializeVlcAsync(token);
        _initializationTask = initializationTask;

        try
        {
            await initializationTask;
        }
        finally
        {
            // Do not let an older, cancelled load clear a newer reload's task.
            if (ReferenceEquals(_initializationTask, initializationTask))
                _initializationTask = null;
        }
    }

    private async Task InitializeVlcAsync(CancellationToken cancellationToken)
    {
        MediaPlayer? player = null;

        try
        {
            // MainWindow animates newly navigated content. Creating the native VLC child window
            // during that animation causes it to repaint independently of WPF, so wait until the
            // transition has settled before attaching the video surface.
            if (ConfigService.AreAnimationsEnabled)
                await Task.Delay(160, cancellationToken);

            var result = await Task.Run(() =>
            {
                var lv = SharedLibVlc.Value;
                var mp = new MediaPlayer(lv)
                {
                    EnableHardwareDecoding = true
                };
                return (lv, mp);
            }, cancellationToken);

            player = result.mp;

            if (cancellationToken.IsCancellationRequested || !IsLoaded)
            {
                player.Dispose();
                return;
            }

            _libVLC = result.lv;
            _mediaPlayer = player;
            player = null;

            AttachVideoSurface();
            _mediaPlayer.Volume = (int)SldVolume.Value;
            BtnPlayPause.IsEnabled = true;
            BtnRefresh.IsEnabled = true;
            StartStream();
        }
        catch (OperationCanceledException)
        {
            player?.Dispose();
        }
        catch (Exception ex)
        {
            player?.Dispose();
            Debug.WriteLine("VLC initialization failed: " + ex.Message);
            if (!cancellationToken.IsCancellationRequested)
                ShowFailed();
        }
    }

    private void StartStream()
    {
        if (_libVLC == null || _mediaPlayer == null || !IsLoaded || !_playbackActive)
            return;

        var generation = ++_playbackGeneration;
        CancelConnectionTimeout();
        DetachAttemptEvents();

        var cfg = ConfigService.GetConfig();
        string streamUrl = string.IsNullOrWhiteSpace(cfg.LiveStreamUrl)
            ? "rtmp://frp-pen.com:48399/live/ycp"
            : cfg.LiveStreamUrl.Trim();

        if (!Uri.TryCreate(streamUrl, UriKind.Absolute, out var uri))
        {
            Debug.WriteLine("Invalid live stream URL: " + streamUrl);
            _mediaPlayer.Stop();
            _currentMedia?.Dispose();
            _currentMedia = null;
            ShowFailed();
            return;
        }

        ShowLoading();

        try
        {
            _mediaPlayer.Stop();
            _currentMedia?.Dispose();
            _currentMedia = new Media(_libVLC, uri);

            // Event handlers are registered before Play to avoid missing a fast Playing event.
            _playingHandler = (_, _) => OnPlaying(generation);
            _errorHandler = (_, _) => OnEncounteredError(generation);
            _mediaPlayer.Playing += _playingHandler;
            _mediaPlayer.EncounteredError += _errorHandler;

            var lifetimeToken = _lifetimeCts?.Token ?? CancellationToken.None;
            _timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(lifetimeToken);
            _ = RunConnectionTimeoutAsync(generation, _timeoutCts.Token);

            if (!_mediaPlayer.Play(_currentMedia))
            {
                FailAttempt(generation, stopPlayer: false);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine("VLC playback failed: " + ex.Message);
            FailAttempt(generation);
        }
    }

    private async Task RunConnectionTimeoutAsync(int generation, CancellationToken cancellationToken)
    {
        try
        {
            for (int seconds = ConnectionTimeoutSeconds; seconds > 0; seconds--)
            {
                if (generation != _playbackGeneration || !IsLoaded)
                    return;

                TimeoutHint.Text = $"连接超时倒计时 {seconds} 秒";
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }

            if (generation != _playbackGeneration || !IsLoaded)
                return;

            FailAttempt(generation);
        }
        catch (OperationCanceledException)
        {
            // Expected when playback starts, the user retries, or the view unloads.
        }
    }

    private void OnPlaying(int generation)
    {
        Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
        {
            if (generation != _playbackGeneration || !IsLoaded)
                return;

            CancelConnectionTimeout();
            if (_mediaPlayer != null && _playingHandler != null)
                _mediaPlayer.Playing -= _playingHandler;
            _playingHandler = null;
            LoadingOverlay.Visibility = Visibility.Collapsed;
            FailedOverlay.Visibility = Visibility.Collapsed;
            IconPlayPause.Text = "\uE769";
        }));
    }

    private void OnEncounteredError(int generation)
    {
        Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
        {
            if (generation != _playbackGeneration || !IsLoaded)
                return;

            FailAttempt(generation);
        }));
    }

    private void ShowLoading()
    {
        LoadingOverlay.Visibility = Visibility.Visible;
        FailedOverlay.Visibility = Visibility.Collapsed;
        TimeoutHint.Text = "";
        IconPlayPause.Text = "\uE769";
    }

    private void ShowFailed()
    {
        LoadingOverlay.Visibility = Visibility.Collapsed;
        FailedOverlay.Visibility = Visibility.Visible;
        IconPlayPause.Text = "\uE768";
    }

    private void FailAttempt(int generation, bool stopPlayer = true)
    {
        if (generation != _playbackGeneration)
            return;

        // Invalidate callbacks already queued by LibVLC before exposing the failure state.
        ++_playbackGeneration;
        CancelConnectionTimeout();
        DetachAttemptEvents();
        if (stopPlayer)
            _mediaPlayer?.Stop();
        ShowFailed();
    }

    private void CancelConnectionTimeout()
    {
        var timeoutCts = Interlocked.Exchange(ref _timeoutCts, null);
        if (timeoutCts == null)
            return;

        timeoutCts.Cancel();
        timeoutCts.Dispose();
    }

    private void DetachAttemptEvents()
    {
        if (_mediaPlayer == null)
            return;

        if (_playingHandler != null)
            _mediaPlayer.Playing -= _playingHandler;
        if (_errorHandler != null)
            _mediaPlayer.EncounteredError -= _errorHandler;

        _playingHandler = null;
        _errorHandler = null;
    }

    private void BtnRetry_Click(object sender, RoutedEventArgs e)
    {
        if (_mediaPlayer != null)
        {
            StartStream();
            return;
        }

        _ = EnsureInitializedAsync();
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

    public void SetPlaybackActive(bool active)
    {
        if (_playbackActive == active)
            return;

        _playbackActive = active;
        if (!active)
        {
            _resumeWhenActivated =
                (_mediaPlayer != null &&
                 (_mediaPlayer.IsPlaying ||
                  LoadingOverlay.Visibility == Visibility.Visible)) ||
                _initializationTask is { IsCompleted: false };
            ++_playbackGeneration;
            CancelConnectionTimeout();
            DetachAttemptEvents();
            DetachVideoSurface();
            _mediaPlayer?.Stop();
            return;
        }

        if (!IsLoaded)
            return;

        if (_mediaPlayer == null)
        {
            if (_resumeWhenActivated)
            {
                _resumeWhenActivated = false;
                _ = EnsureInitializedAsync();
            }
            return;
        }

        AttachVideoSurface();
        if (_resumeWhenActivated)
        {
            _resumeWhenActivated = false;
            StartStream();
        }
    }

    private void AttachVideoSurface()
    {
        if (!_playbackActive || _mediaPlayer == null)
            return;

        VideoPlayer.Visibility = Visibility.Visible;
        if (!ReferenceEquals(VideoPlayer.MediaPlayer, _mediaPlayer))
            VideoPlayer.MediaPlayer = _mediaPlayer;
    }

    private void DetachVideoSurface()
    {
        // LibVLCSharp.WPF owns native child/overlay windows. Stopping playback alone
        // does not guarantee that those windows are hidden when the WPF owner is sent
        // to the tray, so detach and collapse the surface synchronously first.
        VideoPlayer.MediaPlayer = null;
        VideoPlayer.Visibility = Visibility.Collapsed;
    }

    private void SldVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_mediaPlayer != null)
            _mediaPlayer.Volume = (int)e.NewValue;
    }

    private void UserControl_Unloaded(object sender, RoutedEventArgs e)
    {
        ++_playbackGeneration;
        CancelConnectionTimeout();

        var lifetimeCts = Interlocked.Exchange(ref _lifetimeCts, null);
        if (lifetimeCts != null)
        {
            lifetimeCts.Cancel();
            lifetimeCts.Dispose();
        }

        DetachAttemptEvents();
        BtnPlayPause.IsEnabled = false;
        BtnRefresh.IsEnabled = false;

        DetachVideoSurface();
        VideoPlayer.Dispose();

        if (_mediaPlayer != null)
        {
            _mediaPlayer.Stop();
            _mediaPlayer.Dispose();
            _mediaPlayer = null;
        }

        _currentMedia?.Dispose();
        _currentMedia = null;
        _libVLC = null;
    }
}
