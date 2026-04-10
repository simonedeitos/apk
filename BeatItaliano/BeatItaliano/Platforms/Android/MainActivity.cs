using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;

namespace BeatItaliano;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = ConfigChanges.ScreenSize
                         | ConfigChanges.Orientation
                         | ConfigChanges.UiMode
                         | ConfigChanges.ScreenLayout
                         | ConfigChanges.SmallestScreenSize
                         | ConfigChanges.Density,
    ScreenOrientation = ScreenOrientation.Landscape  // Forza landscape
)]
// ── Intent filter per Amazon Fire TV Launcher ──
[IntentFilter(
    new[] { Android.Content.Intent.ActionMain },
    Categories = new[] {
        Android.Content.Intent.CategoryLauncher,
        "android.intent.category.LEANBACK_LAUNCHER"   // Appare nel menu Fire TV
    }
)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // Fullscreen immersivo — nasconde barre di sistema
        if (Window != null)
        {
            Window.SetFlags(
                WindowManagerFlags.Fullscreen,
                WindowManagerFlags.Fullscreen);

            Window.DecorView.SystemUiVisibility =
                (StatusBarVisibility)(
                    SystemUiFlags.Fullscreen
                    | SystemUiFlags.HideNavigation
                    | SystemUiFlags.ImmersiveSticky
                    | SystemUiFlags.LayoutStable
                    | SystemUiFlags.LayoutHideNavigation
                    | SystemUiFlags.LayoutFullscreen);
        }

        // Mantieni lo schermo sempre acceso
        Window?.AddFlags(WindowManagerFlags.KeepScreenOn);
    }
}