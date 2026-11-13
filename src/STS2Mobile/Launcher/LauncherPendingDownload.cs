using System;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

// Pending transfer evidence only selects Resume; installed readiness is checked separately.
internal readonly struct LauncherPendingDownload
{
    private LauncherPendingDownload(bool canResume, string problem = null)
    {
        CanResume = canResume;
        Problem = problem;
    }

    internal bool CanResume { get; }
    internal string Problem { get; }

    internal static LauncherPendingDownload Inspect(string dataDir, string branch)
    {
        try
        {
            BranchInstallState state = null;
            try
            {
                state = BranchInstallStateStore.Current.Read(dataDir, branch);
            }
            catch (BranchInstallStateException error) when (
                error.Kind is BranchInstallStateFailureKind.Missing
                    or BranchInstallStateFailureKind.LegacyRequiresRecovery
            )
            {
                // A staged transfer can predate installation-state publication.
            }

            if (state?.IsReady == true)
                return new(false);
            if (state != null)
            {
                if (IsPhase(state.Phase, LocalPckRepairOperation.PhasePrefix)
                    || IsPhase(state.Phase, "selected-branch-recovery"))
                {
                    return new(false);
                }

                if (state.Depots.Count > 0 || IsGameDownloadPhase(state.Phase))
                    return new(true);
            }

            return new(DepotDownloadStaging.HasPending(
                SteamGameInstallPaths.DownloadStateDirectoryPath(dataDir, branch)
            ));
        }
        catch (Exception error)
        {
            return new(false, error.Message);
        }
    }

    private static bool IsPhase(string phase, string prefix)
        => string.Equals(phase, prefix, StringComparison.Ordinal)
            || phase.StartsWith(prefix + "-", StringComparison.Ordinal);

    private static bool IsGameDownloadPhase(string phase)
        => phase is "loading-cdn-servers" or "downloading" or "cancelled" or "failed"
            or "preparing-android-pck" or "publishing-completed-steam-generation"
            or "calculating-game-identity" or "invalidating-previous-derived-artifacts";
}
