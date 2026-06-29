using System;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherDownloadCoordinator
{
    private const string RedownloadConfirmationMessage =
        "Redownload selected game version?\nThis keeps your Steam login and other cached versions, but deletes the selected version's downloaded files.";
    internal const string DownloadGameFilesButtonText = "Download Selected Version";
    internal const string RedownloadSelectedVersionButtonText = "Redownload Selected Version";
    internal const string DownloadCancelledStatus = "Download cancelled";
    internal const string RetryDownloadButtonText = "Retry Download";
    internal const string RedownloadStatusMessage =
        "Selected game version deleted. Download again to rebuild it.";
    internal const string RedownloadLogMessage =
        "Selected game version files were deleted for a clean redownload.";
    private const string BlockedRedownloadConfirmationMessage =
        "Delete selected game version cache?\nThis branch is currently blocked by Steam app-info availability evidence, so the launcher will delete the selected local cache but will not start a replacement download.";

    private readonly LauncherModel _model;
    private readonly LauncherView _view;
    private readonly LauncherLaunchCoordinator _launch;
    private readonly Action _refreshGameBranchOptions;

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
