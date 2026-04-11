// Aggiornato 2026-04-11
using LibVLCSharp.Shared;

namespace BeatItaliano;

public partial class MainPage : ContentPage
{
    private LibVLC? _libVLC;
    private MediaPlayer? _mediaPlayer;

    // ── URL del flusso streaming ──
    private const string StreamUrl =
        "https://stream-cdn-iad3.vaughnsoft.net/play/live_airdirector.flv";

    public MainPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        StartStreaming();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        StopStreaming();
    }

    /// <summary>
    /// Inizializza LibVLC e avvia lo streaming.
    /// </summary>
    private void StartStreaming()
    {
        try
        {
            // Inizializza il motore VLC con opzioni ottimizzate per Fire Stick
            _libVLC = new LibVLC(
                "--no-osd",                // Nessun on-screen display
                "--network-caching=3000",  // 3s di buffer per stabilità
                "--live-caching=3000",
                "--file-caching=1000",
                "--clock-jitter=0",
                "--clock-synchro=0",
                "--no-audio-time-stretch"
            );

            var media = new Media(_libVLC, new Uri(StreamUrl));

            _mediaPlayer = new MediaPlayer(media)
            {
                EnableHardwareDecoding = true,  // Usa decoder HW del Fire Stick
                Fullscreen = true
            };

            // Gestione eventi
            _mediaPlayer.Playing += OnMediaPlaying;
            _mediaPlayer.EncounteredError += OnMediaError;
            _mediaPlayer.EndReached += OnMediaEndReached;

            // Collega il player alla VideoView e avvia
            VideoView.MediaPlayer = _mediaPlayer;
            _mediaPlayer.Play();
        }
        catch (Exception ex)
        {
            ShowError($"Errore di inizializzazione: {ex.Message}");
        }
    }

    /// <summary>
    /// Ferma e rilascia tutte le risorse.
    /// </summary>
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

    // ── EVENTI ───────────────────────────────────────────

    private void OnMediaPlaying(object? sender, EventArgs e)
    {
        // Nascondi loading quando il video parte
        MainThread.BeginInvokeOnMainThread(() =>
        {
            LoadingOverlay.IsVisible = false;
            ErrorOverlay.IsVisible = false;
        });
    }

    private void OnMediaError(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            ShowError("Impossibile riprodurre lo streaming.\nVerifica la connessione internet.");
        });
    }

    private void OnMediaEndReached(object? sender, EventArgs e)
    {
        // Se lo stream termina, riprova dopo 3 secondi
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            LoadingOverlay.IsVisible = true;
            ErrorOverlay.IsVisible = false;
            await Task.Delay(3000);
            StopStreaming();
            StartStreaming();
        });
    }

    // ── HELPER ───────────────────────────────────────────

    private void ShowError(string message)
    {
        LoadingOverlay.IsVisible = false;
        ErrorOverlay.IsVisible = true;
        ErrorMessage.Text = message;
    }

    private void OnRetryClicked(object? sender, EventArgs e)
    {
        ErrorOverlay.IsVisible = false;
        LoadingOverlay.IsVisible = true;
        StopStreaming();
        StartStreaming();
    }
}