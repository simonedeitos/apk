using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Widget;
using LibVLCSharp.Shared;

namespace BeatItaliano;

[Activity(
    Label = "BeatItaliano",
    MainLauncher = true,
    Theme = "@style/AppTheme",
    LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = ConfigChanges.ScreenSize
                         | ConfigChanges.Orientation
                         | ConfigChanges.UiMode
                         | ConfigChanges.ScreenLayout
                         | ConfigChanges.SmallestScreenSize
                         | ConfigChanges.Density,
    ScreenOrientation = ScreenOrientation.Landscape
)]
[IntentFilter(
    new[] { Android.Content.Intent.ActionMain },
    Categories = new[] {
        Android.Content.Intent.CategoryLauncher,
        "android.intent.category.LEANBACK_LAUNCHER"
    }
)]
public class MainActivity : Activity
{
    private static bool _vlcCoreInitialized;

    private LibVLC? _libVLC;
    private MediaPlayer? _mediaPlayer;
    private SurfaceView? _videoSurface;

    private LinearLayout? _loadingOverlay;
    private LinearLayout? _errorOverlay;
    private TextView? _errorMessage;
    private Button? _retryButton;

    private const string StreamUrl =
        "https://stream-cdn-iad3.vaughnsoft.net/play/live_airdirector.flv";

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        Window?.SetFlags(WindowManagerFlags.Fullscreen, WindowManagerFlags.Fullscreen);
        Window?.AddFlags(WindowManagerFlags.KeepScreenOn);

        if (Window?.DecorView != null)
        {
            Window.DecorView.SystemUiVisibility =
                (StatusBarVisibility)(
                    SystemUiFlags.Fullscreen
                    | SystemUiFlags.HideNavigation
                    | SystemUiFlags.ImmersiveSticky
                    | SystemUiFlags.LayoutStable
                    | SystemUiFlags.LayoutHideNavigation
                    | SystemUiFlags.LayoutFullscreen);
        }

        SetContentView(Resource.Layout.activity_main);

        _videoSurface = FindViewById<SurfaceView>(Resource.Id.videoSurface);
        _loadingOverlay = FindViewById<LinearLayout>(Resource.Id.loadingOverlay);
        _errorOverlay = FindViewById<LinearLayout>(Resource.Id.errorOverlay);
        _errorMessage = FindViewById<TextView>(Resource.Id.errorMessage);
        _retryButton = FindViewById<Button>(Resource.Id.retryButton);

        if (_retryButton != null)
        {
            _retryButton.Click += (s, e) =>
            {
                if (_errorOverlay != null) _errorOverlay.Visibility = ViewStates.Gone;
                if (_loadingOverlay != null) _loadingOverlay.Visibility = ViewStates.Visible;
                StopStreaming();
                StartStreaming();
            };
        }

        if (!_vlcCoreInitialized)
        {
            Core.Initialize();
            _vlcCoreInitialized = true;
        }

        StartStreaming();
    }

    private void StartStreaming()
    {
        try
        {
            _libVLC = new LibVLC(
                "--no-osd",
                "--network-caching=3000",
                "--live-caching=3000",
                "--file-caching=1000",
                "--clock-jitter=0",
                "--clock-synchro=0",
                "--no-audio-time-stretch"
            );

            _mediaPlayer = new MediaPlayer(_libVLC);

            _mediaPlayer.Playing += OnMediaPlaying;
            _mediaPlayer.EncounteredError += OnMediaError;
            _mediaPlayer.EndReached += OnMediaEndReached;

            if (_videoSurface?.Holder?.Surface != null)
            {
                _mediaPlayer.AndroidSurface = _videoSurface.Holder.Surface;
            }
            else if (_videoSurface?.Holder != null)
            {
                _videoSurface.Holder.AddCallback(new SurfaceCallback(_mediaPlayer));
            }

            using var media = new Media(_libVLC, new Uri(StreamUrl));
            _mediaPlayer.Play(media);
        }
        catch (Exception ex)
        {
            ShowError($"Errore di inizializzazione: {ex.Message}");
        }
    }

    private void StopStreaming()
    {
        if (_mediaPlayer != null)
        {
            _mediaPlayer.Playing -= OnMediaPlaying;
            _mediaPlayer.EncounteredError -= OnMediaError;
            _mediaPlayer.EndReached -= OnMediaEndReached;
            _mediaPlayer.Stop();
            _mediaPlayer.Dispose();
            _mediaPlayer = null;
        }

        if (_libVLC != null)
        {
            _libVLC.Dispose();
            _libVLC = null;
        }
    }

    private void OnMediaPlaying(object? sender, EventArgs e)
    {
        RunOnUiThread(() =>
        {
            if (_loadingOverlay != null) _loadingOverlay.Visibility = ViewStates.Gone;
            if (_errorOverlay != null) _errorOverlay.Visibility = ViewStates.Gone;
        });
    }

    private void OnMediaError(object? sender, EventArgs e)
    {
        RunOnUiThread(() =>
        {
            ShowError("Impossibile riprodurre lo streaming.\nVerifica la connessione internet.");
        });
    }

    private void OnMediaEndReached(object? sender, EventArgs e)
    {
        _ = Task.Run(async () =>
        {
            await Task.Delay(3000);
            RunOnUiThread(() =>
            {
                if (_loadingOverlay != null) _loadingOverlay.Visibility = ViewStates.Visible;
                if (_errorOverlay != null) _errorOverlay.Visibility = ViewStates.Gone;
                StopStreaming();
                StartStreaming();
            });
        });
    }

    private void ShowError(string message)
    {
        if (_loadingOverlay != null) _loadingOverlay.Visibility = ViewStates.Gone;
        if (_errorOverlay != null) _errorOverlay.Visibility = ViewStates.Visible;
        if (_errorMessage != null) _errorMessage.Text = message;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        StopStreaming();
    }

    private sealed class SurfaceCallback : Java.Lang.Object, ISurfaceHolderCallback
    {
        private readonly MediaPlayer _player;

        public SurfaceCallback(MediaPlayer player)
        {
            _player = player;
        }

        public void SurfaceCreated(ISurfaceHolder holder)
        {
            _player.AndroidSurface = holder.Surface;
        }

        public void SurfaceChanged(ISurfaceHolder holder, Android.Graphics.Format format, int width, int height)
        {
        }

        public void SurfaceDestroyed(ISurfaceHolder holder)
        {
            _player.AndroidSurface = null;
        }
    }
}
