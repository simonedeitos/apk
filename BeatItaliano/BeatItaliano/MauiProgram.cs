// Aggiornato 2026-04-11
using LibVLCSharp.MAUI;
using Microsoft.Extensions.Logging;

namespace BeatItaliano;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseLibVLCSharp()  // <-- Registra LibVLCSharp
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}