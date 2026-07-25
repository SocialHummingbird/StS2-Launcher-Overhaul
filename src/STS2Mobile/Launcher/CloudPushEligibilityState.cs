namespace STS2Mobile.Launcher;

internal readonly record struct CloudPushEligibilityState(
    string SelectedVersion,
    int SelectedModCount,
    bool ManualPullCompleted,
    bool ManualPullMatchesSelectedVersion,
    bool HasImportantLocalSaveEvidence,
    bool LocalSaveOriginMatchesSelectedRuntime,
    bool HasBranchSwitchMarker,
    bool BranchSwitchEvidenceValid,
    bool HasManualPullAfterBranchSwitch,
    bool IsLocalBackupEnabled,
    bool HasBackupStoragePermission
);
