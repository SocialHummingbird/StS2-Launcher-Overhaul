using System;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherDownloadCoordinator
{
    private const string RedownloadConfirmationMessage =
        "Redownload selected version?\nOnly this version's downloaded files will be removed. Saves, your Steam login, and other versions stay in place. Download the version again when prompted. Do not uninstall the app or clear app data.";
    internal const string DownloadGameFilesButtonText = "Download Selected Version";
    internal const string RedownloadSelectedVersionButtonText = "Redownload selected version";
    internal const string RedownloadRequiredStatus =
        "The selected version must be redownloaded before it can launch.";
    internal const string DownloadCancelledStatus = "Download cancelled";
    internal const string RetryDownloadButtonText = "Retry Download";
    internal const string RedownloadStatusMessage =
        "Selected branch files removed. Download the branch again. Saves and other branches stay in place.";
    internal const string RedownloadLogMessage =
        "Selected branch files were removed for repair. Saves and other branches were left in place.";
    private const string BlockedRedownloadConfirmationMessage =
        "Redownload selected version?\nOnly this version's downloaded files will be removed. Saves, your Steam login, and other versions stay in place. Steam does not currently allow a replacement download for this version.";

    private readonly LauncherModel _model;
    private readonly LauncherView _view;
    private readonly LauncherLaunchCoordinator _launch;
    private readonly Action _refreshGameBranchOptions;
    private Action _automaticRepairContinuation;

    internal LauncherDownloadCoordinator(
        LauncherModel model,
        LauncherView view,
        LauncherLaunchCoordinator launch,
        Action refreshGameBranchOptions
    )
    {
        _model = model;
        _view = view;
        _launch = launch;
        _refreshGameBranchOptions = refreshGameBranchOptions;
    }
}
