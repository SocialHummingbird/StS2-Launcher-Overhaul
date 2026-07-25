#nullable enable

namespace STS2Mobile.Steam;

internal enum LocalBackupRefreshCompletion
{
    Skipped,
    Success,
    PartialSuccess,
    Failure,
}

internal readonly record struct LocalBackupRefreshResult(
    bool Attempted,
    bool StorageAccessAvailable,
    int Discovered,
    int Mirrored,
    int Archived,
    int Restored,
    int Errors,
    string FailureMessage
)
{
    public override string ToString()
        => $"discovered={Discovered}; mirrored={Mirrored}; archived={Archived}; "
            + $"restored={Restored}; errors={Errors}";

    internal LocalBackupRefreshCompletion Completion
    {
        get
        {
            if (!Attempted)
                return LocalBackupRefreshCompletion.Skipped;

            if (
                !StorageAccessAvailable
                || !string.IsNullOrWhiteSpace(FailureMessage)
            )
                return LocalBackupRefreshCompletion.Failure;

            if (Errors <= 0)
                return LocalBackupRefreshCompletion.Success;

            return Discovered > 0
                || Mirrored > 0
                || Archived > 0
                || Restored > 0
                ? LocalBackupRefreshCompletion.PartialSuccess
                : LocalBackupRefreshCompletion.Failure;
        }
    }

    internal static LocalBackupRefreshResult Skipped(bool storageAccessAvailable)
        => new(
            Attempted: false,
            StorageAccessAvailable: storageAccessAvailable,
            Discovered: 0,
            Mirrored: 0,
            Archived: 0,
            Restored: 0,
            Errors: 0,
            FailureMessage: ""
        );

    internal static LocalBackupRefreshResult StorageAccessMissing()
        => new(
            Attempted: true,
            StorageAccessAvailable: false,
            Discovered: 0,
            Mirrored: 0,
            Archived: 0,
            Restored: 0,
            Errors: 0,
            FailureMessage: "Shared-storage access is unavailable."
        );

    internal static LocalBackupRefreshResult Failed(string failureMessage)
        => new(
            Attempted: true,
            StorageAccessAvailable: true,
            Discovered: 0,
            Mirrored: 0,
            Archived: 0,
            Restored: 0,
            Errors: 0,
            FailureMessage: failureMessage
        );
}
