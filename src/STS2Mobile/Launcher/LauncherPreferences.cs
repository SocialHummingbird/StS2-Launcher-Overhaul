namespace STS2Mobile.Launcher;

internal static partial class LauncherPreferences
{
    private const string GameBranchPreferenceKey = "game_branch";
    private const string RendererModePreferenceKey = "renderer_mode";
    private static readonly PreferenceFile GameBranchPreference = new(GameBranchPreferenceKey);
    private static readonly PreferenceFile RendererModePreference = new(RendererModePreferenceKey);
}
