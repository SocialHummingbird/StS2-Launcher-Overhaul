namespace STS2Mobile.Launcher;

internal static partial class LauncherBackupEvidence
{
    private const int MaxBackupFilesToInspect = 500;
    private const string LocalPrePushSource = "local-pre-push";
    private const string CloudPrePushSource = "cloud-pre-push";

    internal static string BackupDirectory
        => STS2Mobile.AppPaths.ExternalSaveBackupsDir;

    internal static int LocalPrePushBackupCount()
        => CountBackups(LocalPrePushSource);

    internal static int CloudPrePushBackupCount()
        => CountBackups(CloudPrePushSource);

    internal static string LatestLocalPrePushBackupUtc()
        => LatestBackupUtc(LocalPrePushSource);

    internal static string LatestCloudPrePushBackupUtc()
        => LatestBackupUtc(CloudPrePushSource);

    internal static bool HasLocalPrePushBackupAfterBranchSwitch(string dataDir)
        => HasBackupAfterBranchSwitch(dataDir, LocalPrePushSource);

    internal static bool HasCloudPrePushBackupAfterBranchSwitch(string dataDir)
        => HasBackupAfterBranchSwitch(dataDir, CloudPrePushSource);

    internal static bool HasPrePushBackupEvidenceAfterBranchSwitch(string dataDir)
        => HasLocalPrePushBackupAfterBranchSwitch(dataDir)
            && HasCloudPrePushBackupAfterBranchSwitch(dataDir);
}
