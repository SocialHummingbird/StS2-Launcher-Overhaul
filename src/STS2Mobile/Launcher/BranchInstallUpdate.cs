using System;
using System.Collections.Generic;

namespace STS2Mobile.Launcher;

internal sealed class BranchInstallUpdate : IDisposable
{
    private BranchInstallStateStore.UpdateSession _session;

    private BranchInstallUpdate(BranchInstallStateStore.UpdateSession session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
    }

    internal string Branch => Session.Branch;
    internal Guid TransactionId => Session.TransactionId;
    internal BranchInstallState State => Session.State;

    internal static BranchInstallUpdate StartOrResume(
        string dataDir,
        string branch,
        IReadOnlyList<BranchInstallDepot> targetDepots,
        string phase = "downloading"
    )
        => new(
            BranchInstallStateStore.Current.BeginOrResumeUpdate(
                dataDir,
                branch,
                phase,
                targetDepots
            )
        );

    internal void RequireUpdatingBeforeInstalledMutation()
        => Session.RequireUpdating();

    internal void UpdatePhase(string phase)
        => Session.UpdateProgress(phase);

    internal BranchInstallCompletion CompleteInstalledFiles(
        string pckPreparationVersion,
        Action<GameIdentity> beforeReadyPublication = null
    )
    {
        Session.UpdateProgress("calculating-game-identity");
        var identity = GameIdentityReader.ReadInstalled(Session.DataDir, Branch);

        Session.UpdateProgress("invalidating-previous-derived-artifacts");
        BranchInstallDerivedArtifacts.InvalidatePreviousIdentity(
            Session.DataDir,
            identity
        );

        beforeReadyPublication?.Invoke(identity);
        Session.CommitReady(identity, pckPreparationVersion);
        return new BranchInstallCompletion(Branch, TransactionId, identity);
    }

    internal void RecordFailure(string phase, Exception exception)
        => Session.RecordFailure(phase, exception);

    private BranchInstallStateStore.UpdateSession Session
        => _session ?? throw new ObjectDisposedException(nameof(BranchInstallUpdate));

    public void Dispose()
    {
        var session = _session;
        _session = null;
        session?.Dispose();
    }
}
