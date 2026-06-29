namespace STS2Mobile.Launcher;

internal readonly struct DownloadViewUpdate
{
    private DownloadViewUpdate(
        string status = null,
        string logMessage = null,
        string downloadAction = null,
        string resetDownloadButton = null,
        LaunchUpdateAction? launchAction = null,
        bool? downloadButtonDisabled = null,
        bool hideActions = false,
        bool hideDownload = false,
        bool showRetry = false,
        bool resetDownload = false
    )
    {
        Status = status;
        LogMessage = logMessage;
        DownloadAction = downloadAction;
        ResetDownloadButton = resetDownloadButton;
        LaunchAction = launchAction;
        DownloadButtonDisabled = downloadButtonDisabled;
        HideActions = hideActions;
        HideDownload = hideDownload;
        ShowRetry = showRetry;
        ResetDownload = resetDownload;
    }

    private string Status { get; }
    private string LogMessage { get; }
    private string DownloadAction { get; }
    private string ResetDownloadButton { get; }
    private LaunchUpdateAction? LaunchAction { get; }
    private bool? DownloadButtonDisabled { get; }
    private bool HideActions { get; }
    private bool HideDownload { get; }
    private bool ShowRetry { get; }
    private bool ResetDownload { get; }

    internal static DownloadViewUpdate Ready(
        string buttonText = LauncherDownloadCoordinator.DownloadGameFilesButtonText
    )
        => new(
            downloadAction: buttonText,
            downloadButtonDisabled: false
        );

    internal static DownloadViewUpdate RedownloadApplied()
        => new(
            status: LauncherDownloadCoordinator.RedownloadStatusMessage,
            logMessage: LauncherDownloadCoordinator.RedownloadLogMessage,
            downloadAction: LauncherDownloadCoordinator.DownloadGameFilesButtonText,
            downloadButtonDisabled: false,
            hideActions: true
        );

    internal static DownloadViewUpdate Completed(bool filesReady, string readinessProblem)
        => new(
            status: filesReady
                ? $"Selected game version downloaded ({LauncherGameVersionDisplay.SelectedGameVersionName()}). Start game when ready."
                : readinessProblem,
            hideDownload: true,
            launchAction: filesReady
                ? LaunchUpdateAction.Hidden
                : (LaunchUpdateAction?)null,
            showRetry: !filesReady
        );

    internal static DownloadViewUpdate Failed(string message)
        => string.IsNullOrEmpty(message)
            ? new(resetDownload: true)
            : new(
                status: $"Download failed for selected game version ({LauncherGameVersionDisplay.SelectedGameVersionName()}): {message}",
                resetDownloadButton: LauncherDownloadCoordinator.RetryDownloadButtonText
            );

    internal static DownloadViewUpdate Cancelled()
        => new(
            status: LauncherDownloadCoordinator.DownloadCancelledStatus,
            downloadButtonDisabled: false
        );

    internal void Apply(LauncherView view, LauncherLaunchCoordinator launch)
    {
        if (HideActions)
            view.HideActions();

        if (DownloadAction != null)
            view.ShowDownloadAction(DownloadAction);

        if (DownloadButtonDisabled.HasValue)
            view.SetDownloadButtonDisabled(DownloadButtonDisabled.Value);

        if (Status != null)
            view.SetStatus(Status);

        if (LogMessage != null)
            view.AppendLog(LogMessage);

        if (HideDownload)
            view.HideDownload();

        if (ResetDownload)
            view.ResetDownload();
        else if (ResetDownloadButton != null)
            view.ResetDownload(ResetDownloadButton);

        if (LaunchAction.HasValue)
            launch.ShowLaunchActions(LaunchAction.Value);

        if (ShowRetry)
            view.ShowRetry();
    }
}
