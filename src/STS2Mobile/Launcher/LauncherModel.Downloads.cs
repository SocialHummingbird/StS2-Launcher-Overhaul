using System;
using System.Threading;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal partial class LauncherModel
{
    private CancellationTokenSource _downloadCts;
    private DepotDownloader _downloader;
    private int _downloadRunning;

    internal event Action<DepotDownloader.DownloadProgress> DownloadProgressChanged;
    internal event Action<string> DownloadLogReceived;
    internal event Action DownloadCompleted;
    internal event Action<string> DownloadFailed;
    internal event Action DownloadCancelled;
    internal event Action<bool> UpdateCheckCompleted;
    internal event Action<string> UpdateCheckFailed;
    internal event Action BranchCatalogRefreshCompleted;
    internal event Action<string> BranchCatalogRefreshFailed;

    private bool DownloadIsRunning => Interlocked.CompareExchange(ref _downloadRunning, 0, 0) == 1;
}
