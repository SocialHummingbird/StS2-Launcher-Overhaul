namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private const string RedownloadConfirmationMessage =
        "Redownload selected game version?\nThis keeps your Steam login and other cached versions, but deletes the selected version's downloaded files.";
    private const string DownloadGameFilesButtonText = "Download Selected Version";
    private const string RedownloadSelectedVersionButtonText = "Redownload Selected Version";
    private const string DownloadCancelledStatus = "Download cancelled";
    private const string RetryDownloadButtonText = "Retry Download";
    private const string RedownloadStatusMessage =
        "Selected game version deleted. Download again to rebuild it.";
    private const string RedownloadLogMessage =
        "Selected game version files were deleted for a clean redownload.";
    private const string BlockedRedownloadConfirmationMessage =
        "Delete selected game version cache?\nThis branch is currently blocked by Steam app-info availability evidence, so the launcher will delete the selected local cache but will not start a replacement download.";
}
