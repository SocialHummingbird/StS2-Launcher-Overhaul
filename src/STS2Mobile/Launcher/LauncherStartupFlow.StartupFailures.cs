using System;

namespace STS2Mobile.Launcher;

internal static partial class LauncherStartupFlow
{
    private readonly partial struct StartupContext
    {
        internal void HandleSettingsAndSavesFailure(Exception ex)
            => LauncherGameStartupRecovery.HandleSettingsAndSavesFailure(
                GameNode,
                Status,
                ex
            );

        internal void HandleFailure(Exception ex)
            => LauncherGameStartupRecovery.HandleFailure(GameNode, Status, ex);
    }
}
