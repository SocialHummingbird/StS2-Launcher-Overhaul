namespace STS2Mobile.Launcher;

internal static partial class LauncherPreferences
{
    internal static string ReadRendererMode()
        => LauncherRendererMode.Normalize(
            RendererModePreference.ReadText(LauncherRendererMode.Auto)
        );

    internal static void SaveRendererMode(string mode)
        => RendererModePreference.WriteText(LauncherRendererMode.Normalize(mode));
}
