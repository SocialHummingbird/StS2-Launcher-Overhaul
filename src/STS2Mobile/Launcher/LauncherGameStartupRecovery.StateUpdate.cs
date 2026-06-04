using Godot;
using System;

namespace STS2Mobile.Launcher;

internal static partial class LauncherGameStartupRecovery
{
    private const string MainMenuGuardFailureReason = "main menu guard failed";
    private const string MainMenuRecoveryFailureReason =
        "main menu recovery failed after watchdog";
    private const string StartupObservationReason = "post-startup observation";
    private const string WatchdogStalledReason = "game startup watchdog";
    private const string WatchdogRecoveredReason = "main menu recovered after watchdog";

    private readonly struct RecoveryStateUpdate
    {
        private RecoveryStateUpdate(
            string reason,
            string statusMessage,
            string snapshotReason = null
        )
        {
            Reason = reason;
            StatusMessage = statusMessage;
            SnapshotReason = snapshotReason;
        }

        private string Reason { get; }
        private string StatusMessage { get; }
        private string SnapshotReason { get; }
        private string EffectiveSnapshotReason => SnapshotReason ?? Reason;

        internal static RecoveryStateUpdate GameStartupFailed(Exception ex)
        {
            var root = ex.GetBaseException();
            var message = $"{root.GetType().Name}: {root.Message}";
            return new RecoveryStateUpdate(
                $"game startup failed: {message}",
                $"Game startup failed: {message}"
            );
        }

        internal static RecoveryStateUpdate SettingsAndSavesFailed(Exception ex)
            => new(
                "settings and saves failed",
                $"Settings/save init failed: {ex.GetBaseException().Message}"
            );

        internal static RecoveryStateUpdate MainMenuGuardFailed()
            => new(
                MainMenuGuardFailureReason,
                "Main menu did not load. Use recovery controls below."
            );

        internal static RecoveryStateUpdate StartupObserved()
            => new(
                StartupObservationReason,
                "Game startup returned. Recovery controls remain briefly.",
                "after NGame.GameStartup returned"
            );

        internal static RecoveryStateUpdate WatchdogStalled()
            => new(
                WatchdogStalledReason,
                "Game startup stalled. Attempting main menu recovery..."
            );

        internal static RecoveryStateUpdate WatchdogRecovered()
            => new(
                WatchdogRecoveredReason,
                "Main menu recovered after startup stall. Recovery controls remain briefly."
            );

        internal static RecoveryStateUpdate MainMenuRecoveryFailed()
            => new(
                MainMenuRecoveryFailureReason,
                "Game startup stalled and main menu recovery failed. Use recovery controls below."
            );

        internal void Apply(Node gameNode, Label startupStatus)
        {
            LauncherLaunchMarkers.WriteStartupPhase(Reason);
            LauncherDiagnostics.WriteStartupSceneSnapshot(
                gameNode,
                EffectiveSnapshotReason
            );
            LauncherStartupStatus.Set(startupStatus, StatusMessage);
        }
    }
}
