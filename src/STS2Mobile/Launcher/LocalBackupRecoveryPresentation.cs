#nullable enable

using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal readonly record struct LocalBackupRecoveryPresentation(
    LocalBackupRefreshCompletion Completion,
    string StatusText
)
{
    internal static LocalBackupRecoveryPresentation Create(
        LocalBackupRefreshResult result
    )
    {
        var status = result.Completion switch
        {
            LocalBackupRefreshCompletion.Skipped
                => "Save Backup is disabled. Existing backup files were kept.",
            LocalBackupRefreshCompletion.Success
                => $"Save Backup refreshed: {result.Discovered} discovered, "
                    + $"{result.Mirrored} mirrored, {result.Archived} archived.",
            LocalBackupRefreshCompletion.PartialSuccess
                => $"Save Backup partially refreshed: {result.Mirrored} mirrored, "
                    + $"{result.Archived} archived, {result.Errors} errors. "
                    + "No save files were restored automatically.",
            _ => "Save Backup failed: "
                + (
                    string.IsNullOrWhiteSpace(result.FailureMessage)
                        ? $"{result.Errors} errors prevented the backup refresh."
                        : result.FailureMessage
                ),
        };

        return new LocalBackupRecoveryPresentation(
            result.Completion,
            status
        );
    }
}
