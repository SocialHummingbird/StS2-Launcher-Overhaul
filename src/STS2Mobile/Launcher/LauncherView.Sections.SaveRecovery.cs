#nullable enable

using System.Collections.Generic;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    internal void SetSaveRecoveryCandidates(
        IReadOnlyList<SaveRecoveryCandidatePresentation>? candidates,
        string selectedCandidateId = ""
    )
        => Actions.SetSaveRecoveryCandidates(
            candidates,
            selectedCandidateId
        );

    internal void SetSaveRecoveryBusy(bool busy, string status)
        => Actions.SetSaveRecoveryBusy(busy, status);

    internal void SetSaveRecoveryState(
        string status,
        bool canUndo,
        bool canApprove
    )
        => Actions.SetSaveRecoveryState(status, canUndo, canApprove);
}
