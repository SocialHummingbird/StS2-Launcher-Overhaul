#nullable enable

using System;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    internal void WireSaveRecoveryEvents(
        Action scanRequested,
        Action currentExportRequested,
        Action<string> exportRequested,
        Action<string> restoreRequested,
        Action undoRequested,
        Action approveRequested
    )
    {
        Actions.SaveRecoveryScanPressed += scanRequested;
        Actions.SaveRecoveryCurrentExportPressed += currentExportRequested;
        Actions.SaveRecoveryExportPressed += exportRequested;
        Actions.SaveRecoveryRestorePressed += restoreRequested;
        Actions.SaveRecoveryUndoPressed += undoRequested;
        Actions.SaveRecoveryApprovePressed += approveRequested;
    }
}
