namespace STS2Mobile.Launcher;

internal readonly struct UpdateCheckViewUpdate
{
    private const string UpdateCheckFailedButtonText = "Check Failed";
    private const string UpdateCheckBlockedButtonText = "Check Blocked";
    private const string UpToDateButtonText = "Up to Date";
    private const string UpdateGameFilesButtonText = "Update Selected Version";

    private UpdateCheckViewUpdate(
        string logMessage = null,
        string updateButtonText = null,
        string downloadButtonText = null,
        string status = null,
        bool hideActions = false
    )
    {
        LogMessage = logMessage;
        UpdateButtonText = updateButtonText;
        DownloadButtonText = downloadButtonText;
        Status = status;
        HideActions = hideActions;
    }

    private string LogMessage { get; }
    private string UpdateButtonText { get; }
    private string DownloadButtonText { get; }
    private string Status { get; }
    private bool HideActions { get; }

    internal static UpdateCheckViewUpdate Completed(bool hasUpdate)
        => hasUpdate
            ? new(
                downloadButtonText: UpdateGameFilesButtonText,
                status: $"Update available for selected game version ({LauncherGameVersionDisplay.SelectedGameVersionName()}).",
                hideActions: true
            )
            : new(
                logMessage: $"Selected game version is up to date ({LauncherGameVersionDisplay.SelectedGameVersionName()}).",
                updateButtonText: UpToDateButtonText
            );

    internal static UpdateCheckViewUpdate Failed(string message)
        => new(
            logMessage: $"Update check failed for selected game version ({LauncherGameVersionDisplay.SelectedGameVersionName()}): {message}",
            updateButtonText: UpdateCheckFailedButtonText,
            status: $"Update check failed for selected game version ({LauncherGameVersionDisplay.SelectedGameVersionName()}): {message}"
        );

    internal static UpdateCheckViewUpdate Blocked(string message)
        => new(
            logMessage: $"Update check blocked for selected game version ({LauncherGameVersionDisplay.SelectedGameVersionName()}): {message}",
            updateButtonText: UpdateCheckBlockedButtonText,
            status: $"Update check blocked for selected game version ({LauncherGameVersionDisplay.SelectedGameVersionName()}): {message}"
        );

    internal void Apply(LauncherView view)
    {
        if (LogMessage != null)
            view.AppendLog(LogMessage);

        if (HideActions)
            view.HideActions();

        if (DownloadButtonText != null)
            view.ShowDownloadAction(DownloadButtonText);

        if (Status != null)
            view.SetStatus(Status);

        if (UpdateButtonText != null)
            view.SetUpdateButtonText(UpdateButtonText);
    }
}
