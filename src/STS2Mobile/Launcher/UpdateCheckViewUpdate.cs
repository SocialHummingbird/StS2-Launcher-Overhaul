namespace STS2Mobile.Launcher;

internal readonly struct UpdateCheckViewUpdate
{
    private UpdateCheckViewUpdate(
        string logMessage = null,
        string status = null,
        LauncherStatusSeverity? statusSeverity = null,
        LauncherVersionPrimaryAction primaryAction =
            LauncherVersionPrimaryAction.CheckForUpdates,
        LauncherVersionCheckOutcome outcome = LauncherVersionCheckOutcome.None
    )
    {
        LogMessage = logMessage;
        Status = status;
        StatusSeverity = statusSeverity;
        PrimaryAction = primaryAction;
        Outcome = outcome;
    }

    private string LogMessage { get; }
    private string Status { get; }
    private LauncherStatusSeverity? StatusSeverity { get; }
    private LauncherVersionPrimaryAction PrimaryAction { get; }
    private LauncherVersionCheckOutcome Outcome { get; }

    internal static UpdateCheckViewUpdate Completed(bool hasUpdate, string selectedVersion)
        => hasUpdate
            ? new(
                logMessage: $"Update available for selected game version ({selectedVersion}).",
                status: $"Update available for selected game version ({selectedVersion}).",
                statusSeverity: LauncherStatusSeverity.Warning,
                primaryAction: LauncherVersionPrimaryAction.UpdateSelectedVersion,
                outcome: LauncherVersionCheckOutcome.UpdateAvailable
            )
            : new(
                logMessage: $"Selected game version is up to date ({selectedVersion}).",
                outcome: LauncherVersionCheckOutcome.UpToDate
            );

    internal static UpdateCheckViewUpdate Failed(string message, string selectedVersion)
        => new(
            logMessage: $"Update check failed for selected game version ({selectedVersion}): {message}",
            status: $"Update check failed for selected game version ({selectedVersion}): {message}",
            statusSeverity: LauncherStatusSeverity.Error
        );

    internal static UpdateCheckViewUpdate Blocked(string message, string selectedVersion)
        => new(
            logMessage: $"Update check blocked for selected game version ({selectedVersion}): {message}",
            status: $"Update check blocked for selected game version ({selectedVersion}): {message}",
            statusSeverity: LauncherStatusSeverity.Warning
        );

    internal void Apply(LauncherView view)
    {
        if (LogMessage != null)
            view.AppendLog(LogMessage);

        if (Status != null)
            view.SetStatus(
                Status,
                StatusSeverity
                    ?? throw new System.InvalidOperationException(
                        "An update status message requires an explicit severity."
                    )
            );

        view.SetVersionCheckOutcome(Outcome);
        view.SetVersionPrimaryAction(PrimaryAction);
    }
}
