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
            LocalBackupRefreshCompletion.Success when result.Restored > 0
                => $"Save recovery succeeded: {result.Restored} restored, "
                    + $"{result.Mirrored} mirrored, {result.Archived} archived. "
                    + "Upload availability was refreshed.",
            LocalBackupRefreshCompletion.Success
                => $"Save Backup refreshed: {result.Discovered} discovered, "
                    + $"{result.Mirrored} mirrored, {result.Archived} archived; "
                    + "no missing saves needed recovery.",
            LocalBackupRefreshCompletion.PartialSuccess
                => $"Save recovery partially completed: {result.Restored} restored, "
                    + $"{result.Mirrored} mirrored, {result.Archived} archived, "
                    + $"{result.Errors} errors. Upload availability was refreshed.",
            _ => "Save recovery failed: "
                + (
                    string.IsNullOrWhiteSpace(result.FailureMessage)
                        ? $"{result.Errors} errors prevented recovery."
                        : result.FailureMessage
                ),
        };

        return new LocalBackupRecoveryPresentation(
            result.Completion,
            status
        );
    }
}
