using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal partial class LauncherModel
{
    private void RaiseDownloadCompleted()
        => Raise(DownloadCompleted, nameof(DownloadCompleted));

    private void RaiseDownloadCancelled()
        => Raise(DownloadCancelled, nameof(DownloadCancelled));

    private void RaiseDownloadFailed(string message)
        => Raise(DownloadFailed, message, nameof(DownloadFailed));

    private void RaiseDownloadLogReceived(string message)
        => Raise(DownloadLogReceived, message, nameof(DownloadLogReceived));

    private void RaiseDownloadProgressChanged(DepotDownloader.DownloadProgress progress)
        => Raise(DownloadProgressChanged, progress, nameof(DownloadProgressChanged));

    private void RaiseUpdateCheckCompleted(bool hasUpdate)
        => Raise(UpdateCheckCompleted, hasUpdate, nameof(UpdateCheckCompleted));

    private void RaiseUpdateCheckFailed(string message)
        => Raise(UpdateCheckFailed, message, nameof(UpdateCheckFailed));
}
