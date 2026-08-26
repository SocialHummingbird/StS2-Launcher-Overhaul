using System;
using System.IO;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static class LocalPckRepairOperation
{
    internal const string PhasePrefix = "local-pck-repair";
    internal const string StartingPhase = PhasePrefix + "-starting";
    internal const string ProgressMessage = "Updating Android game preparation…";

    internal static InstalledGameVersionReadiness ClassifyForLauncherRouting(
        string dataDir,
        string branch
    )
    {
        var normalizedBranch = SteamGameBranch.StorageIdentity(branch);
        var classification = LauncherGameFiles.ClassifyInstalledVersion(
            dataDir,
            normalizedBranch
        );
        if (classification != InstalledGameVersionReadiness.RedownloadRequired)
            return classification;

        try
        {
            var state = BranchInstallStateStore.Current.Read(
                dataDir,
                normalizedBranch
            );
            if (state.Status == BranchInstallStatus.Updating
                && state.Phase.StartsWith(PhasePrefix + "-", StringComparison.Ordinal)
                && LauncherGameFiles.CanResumeAutomaticPckRepair(
                    dataDir,
                    normalizedBranch,
                    state
                ))
            {
                return InstalledGameVersionReadiness.AutomaticRepair;
            }
        }
        catch
        {
        }

        return InstalledGameVersionReadiness.RedownloadRequired;
    }

    internal static BranchInstallCompletion RepairAutomatic(
        string dataDir,
        string branch,
        Action<GameIdentity> beforeReadyPublication = null
    )
    {
        var normalizedBranch = SteamGameBranch.StorageIdentity(branch);
        BranchInstallState noOpState = null;
        var update = BranchInstallUpdate.StartOrResume(
            dataDir,
            normalizedBranch,
            targetDepots: null,
            phase: StartingPhase,
            shouldBeginUpdate: current => ShouldBeginRepair(
                dataDir,
                normalizedBranch,
                current,
                out noOpState
            )
        );

        if (update == null)
        {
            return new BranchInstallCompletion(
                noOpState.Branch,
                noOpState.TransactionId,
                noOpState.GameIdentity
            );
        }

        using (update)
        {
            try
            {
                update.UpdatePhase(PhasePrefix + "-patching");
                var pckPath = Path.Combine(
                    SteamGameInstallPaths.GameDirectory(dataDir, normalizedBranch),
                    LauncherStorageNames.GamePck
                );
                DepotDownloader.RepairGamePckForArm64V2(update, pckPath);

                update.UpdatePhase(PhasePrefix + "-validating-arm64-v2");
                if (!LauncherGameFiles.HasArm64V2PckPostconditions(
                        dataDir,
                        normalizedBranch
                    ))
                {
                    throw new IOException(
                        $"Local PCK repair did not satisfy every ARM64 v2 postcondition for branch '{normalizedBranch}'."
                    );
                }

                update.UpdatePhase(PhasePrefix + "-refreshing-branch-marker");
                RefreshBranchMarkerTimestamp(dataDir, normalizedBranch, pckPath);

                return update.CompleteInstalledFiles(
                    AndroidPckPreparationVersions.V2,
                    beforeReadyPublication,
                    phasePrefix: PhasePrefix
                );
            }
            catch (Exception ex)
            {
                update.RecordFailure(PhasePrefix + "-failed", ex);
                throw;
            }
        }
    }

    private static bool ShouldBeginRepair(
        string dataDir,
        string branch,
        BranchInstallState current,
        out BranchInstallState noOpState
    )
    {
        noOpState = null;
        if (current?.Status == BranchInstallStatus.Updating)
        {
            if (current.Phase.StartsWith(PhasePrefix + "-", StringComparison.Ordinal)
                && LauncherGameFiles.CanResumeAutomaticPckRepair(
                    dataDir,
                    branch,
                    current
                ))
            {
                return true;
            }

            throw new IOException(
                $"Branch '{branch}' does not contain a safely repeatable interrupted local PCK repair."
            );
        }

        var readiness = LauncherGameFiles.ClassifyInstalledVersion(dataDir, branch);
        if (readiness == InstalledGameVersionReadiness.AutomaticRepair)
            return true;

        if (readiness == InstalledGameVersionReadiness.Ready
            && current?.IsReady == true
            && LauncherGameFiles.HasArm64V2PckPostconditions(dataDir, branch))
        {
            noOpState = current;
            return false;
        }

        throw new IOException(
            $"Branch '{branch}' is not eligible for local automatic PCK repair; a redownload is required."
        );
    }

    private static void RefreshBranchMarkerTimestamp(
        string dataDir,
        string branch,
        string pckPath
    )
    {
        var markerPath = SteamGameInstallPaths.BranchMarkerPath(dataDir, branch);
        var marker = new FileInfo(markerPath);
        marker.Refresh();
        if (!marker.Exists)
        {
            throw new FileNotFoundException(
                "Cannot complete local PCK repair because the existing branch marker is missing.",
                markerPath
            );
        }

        var pck = new FileInfo(pckPath);
        pck.Refresh();
        if (!pck.Exists)
        {
            throw new FileNotFoundException(
                "Cannot complete local PCK repair because the selected PCK is missing.",
                pckPath
            );
        }

        var desiredTimestamp = DateTime.UtcNow;
        if (desiredTimestamp < marker.LastWriteTimeUtc)
            desiredTimestamp = marker.LastWriteTimeUtc;
        if (desiredTimestamp <= pck.LastWriteTimeUtc)
            desiredTimestamp = pck.LastWriteTimeUtc.AddSeconds(1);

        File.SetLastWriteTimeUtc(markerPath, desiredTimestamp);
        marker.Refresh();
        if (marker.LastWriteTimeUtc < pck.LastWriteTimeUtc)
        {
            throw new IOException(
                $"Branch marker timestamp did not advance past the repaired PCK for branch '{branch}'."
            );
        }
    }
}
