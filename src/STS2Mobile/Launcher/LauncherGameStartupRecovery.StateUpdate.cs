using Godot;
using System;

namespace STS2Mobile.Launcher;

internal static partial class LauncherGameStartupRecovery
{
    private const string GameVisibilityUnconfirmedReason =
        "game foreground visibility confirmation failed";
    private const string StartupObservationReason = "post-startup observation";

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

        internal static RecoveryStateUpdate StartupObserved()
            => new(
                StartupObservationReason,
                "Home screen ready.",
                "after NGame.GameStartup returned"
            );

        internal static RecoveryStateUpdate GameVisibilityUnconfirmed()
            => new(
                GameVisibilityUnconfirmedReason,
                "Home screen is ready but Android did not confirm a foreground focused game window. Use recovery controls below."
            );

        internal void Apply(Node gameNode, Label startupStatus)
        {
            LauncherLaunchMarkers.WriteStartupPhase(Reason);
            if (ShouldWriteSceneSnapshot())
            {
                LauncherDiagnostics.WriteStartupSceneSnapshot(
                    gameNode,
                    EffectiveSnapshotReason
                );
            }
            LauncherStartupStatus.Set(startupStatus, StatusMessage);
        }

        private bool ShouldWriteSceneSnapshot()
            => (Reason != StartupObservationReason)
                || PostStartupDiagnosticsSettings.DetailedTraceEnabled();
    }
}
