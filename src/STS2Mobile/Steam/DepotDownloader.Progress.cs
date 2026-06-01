using System;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    internal sealed class DownloadProgress
    {
        internal DownloadProgress(
            long totalBytes,
            long downloadedBytes,
            string currentFile
        )
        {
            TotalBytes = totalBytes;
            DownloadedBytes = downloadedBytes;
            CurrentFile = currentFile;
        }

        internal long TotalBytes { get; }
        internal long DownloadedBytes { get; }
        internal string CurrentFile { get; }

        internal double Percentage => TotalBytes > 0 ? (double)DownloadedBytes / TotalBytes * 100.0 : 0;
    }

    private sealed class DownloadProgressState
    {
        internal DownloadProgressState() { }

        internal long TotalBytes;
        internal long DownloadedBytes;
        internal string CurrentFile;
    }

    private readonly DownloadProgressState _progress = new();

    internal event Action<DownloadProgress> ProgressChanged;
    internal event Action<string> LogMessage;
}
