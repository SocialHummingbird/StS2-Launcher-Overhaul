using System.Collections.Generic;

namespace STS2Mobile.Launcher;

internal static class CloudPushEligibilityPolicy
{
    internal static CloudPushEligibilityResult Evaluate(
        CloudPushEligibilityState state
    )
    {
        var blocks = new List<CloudPushEligibilityBlock>();
        var selectedVersion = string.IsNullOrWhiteSpace(state.SelectedVersion)
            ? "the selected game version"
            : state.SelectedVersion;

        if (state.SelectedModCount > 0)
        {
            AddBlock(
                blocks,
                CloudPushEligibilityBlockCode.ModsSelected,
                $"{state.SelectedModCount} mod(s) are selected for launch. Modded saves cannot be uploaded to Steam Cloud safely.",
                CloudPushRequiredActionCode.DeselectMods,
                "Deselect all mods and use a vanilla launch state before uploading."
            );
        }

        if (!state.ManualPullCompleted)
        {
            AddBlock(
                blocks,
                CloudPushEligibilityBlockCode.ManualPullNotCompleted,
                $"No completed Pull from Steam Cloud is recorded for {selectedVersion}.",
                CloudPushRequiredActionCode.CompletePullForSelectedVersion,
                $"Complete Pull from Steam Cloud for {selectedVersion}."
            );
        }
        else if (!state.ManualPullMatchesSelectedVersion)
        {
            AddBlock(
                blocks,
                CloudPushEligibilityBlockCode.ManualPullVersionMismatch,
                $"The latest completed Pull belongs to a different game version than {selectedVersion}.",
                CloudPushRequiredActionCode.CompletePullForSelectedVersion,
                $"Complete Pull from Steam Cloud for {selectedVersion}."
            );
        }

        if (!state.HasImportantLocalSaveEvidence)
        {
            AddBlock(
                blocks,
                CloudPushEligibilityBlockCode.ImportantLocalSavesMissing,
                $"No important Android local save files were found for {selectedVersion}.",
                CloudPushRequiredActionCode.VerifyAndroidLocalSaves,
                "Pull saves, open the game, and verify that Android local saves exist."
            );
        }

        if (!state.LocalSaveOriginMatchesSelectedRuntime)
        {
            AddBlock(
                blocks,
                CloudPushEligibilityBlockCode.LocalSaveOriginNotVerified,
                $"The Android local saves are not verified against the installed {selectedVersion} runtime.",
                CloudPushRequiredActionCode.CompletePullForSelectedVersion,
                $"Complete Pull from Steam Cloud against the installed {selectedVersion} runtime."
            );
        }

        if (state.HasBranchSwitchMarker)
        {
            AddBranchSwitchBlocks(blocks, state, selectedVersion);
        }

        return new CloudPushEligibilityResult(blocks);
    }

    private static void AddBranchSwitchBlocks(
        ICollection<CloudPushEligibilityBlock> blocks,
        CloudPushEligibilityState state,
        string selectedVersion
    )
    {
        if (!state.BranchSwitchEvidenceValid)
        {
            AddBlock(
                blocks,
                CloudPushEligibilityBlockCode.BranchSwitchEvidenceInvalid,
                $"The recorded game-version switch is incomplete or does not match {selectedVersion}.",
                CloudPushRequiredActionCode.RebuildBranchSwitchEvidence,
                "Select the intended game version again to rebuild branch-switch safety evidence."
            );
        }

        if (!state.HasManualPullAfterBranchSwitch)
        {
            AddBlock(
                blocks,
                CloudPushEligibilityBlockCode.ManualPullAfterBranchSwitchMissing,
                $"No completed Pull is recorded after the switch to {selectedVersion}.",
                CloudPushRequiredActionCode.CompletePullAfterBranchSwitch,
                $"Complete Pull from Steam Cloud after switching to {selectedVersion}."
            );
        }

        if (!state.IsLocalBackupEnabled)
        {
            AddBlock(
                blocks,
                CloudPushEligibilityBlockCode.LocalBackupDisabledAfterBranchSwitch,
                "Local Backup is disabled after the game-version switch.",
                CloudPushRequiredActionCode.EnableLocalBackup,
                "Turn on Local Backup before uploading saves from the switched game version."
            );
        }

        if (!state.HasBackupStoragePermission)
        {
            AddBlock(
                blocks,
                CloudPushEligibilityBlockCode.BackupStoragePermissionMissing,
                "Backup storage permission is unavailable after the game-version switch.",
                CloudPushRequiredActionCode.GrantBackupStoragePermission,
                "Grant storage access so pre-upload backups can be written."
            );
        }
    }

    private static void AddBlock(
        ICollection<CloudPushEligibilityBlock> blocks,
        CloudPushEligibilityBlockCode blockCode,
        string reason,
        CloudPushRequiredActionCode actionCode,
        string action
    )
        => blocks.Add(
            new CloudPushEligibilityBlock(
                blockCode,
                reason,
                new CloudPushRequiredAction(actionCode, action)
            )
        );
}
