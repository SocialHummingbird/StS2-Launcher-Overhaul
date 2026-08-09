using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static partial class LauncherPreferences
{
    internal readonly struct ActionPreferences
    {
        internal ActionPreferences(
            string gameBranch,
            string rendererMode
        )
        {
            GameBranch = SteamGameBranch.Normalize(gameBranch);
            RendererMode = LauncherRendererMode.Normalize(rendererMode);
        }

        internal string GameBranch { get; }
        internal string RendererMode { get; }
    }

    internal static ActionPreferences ReadActionPreferences()
        => new(
            ReadGameBranch(),
            ReadRendererMode()
        );

    internal static ActionPreferences ReadActionPreferences(string gameBranch)
        => new(
            gameBranch,
            ReadRendererMode()
        );

}
