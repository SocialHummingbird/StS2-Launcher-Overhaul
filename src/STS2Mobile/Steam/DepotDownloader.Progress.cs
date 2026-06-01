using System;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    internal readonly struct DownloadProgress
    {
        private DownloadProgress(
            long totalBytes,
            long downloadedBytes,
            string currentFile
        )
        {
            TotalBytes = totalBytes;
            DownloadedBytes = downloadedBytes;
            CurrentFile = currentFile;
        }

        private long TotalBytes { get; }
        private long DownloadedBytes { get; }
        private string CurrentFile { get; }

        private double Percentage
            => TotalBytes > 0 ? (double)DownloadedBytes / TotalBytes * 100.0 : 0;

        internal static DownloadProgress Create(
            long totalBytes,
            long downloadedBytes,
            string currentFile
        )
            => new(totalBytes, downloadedBytes, currentFile);

        internal void ApplyTo(
            Action<double, string> setProgress,
            Action<string> appendLog
        )
        {
            setProgress(
                Percentage,
                $"{FormatSize(DownloadedBytes)} / {FormatSize(TotalBytes)} ({Percentage:F1}%)"
            );
            appendLog(CurrentFile);
        }

        private static string FormatSize(long bytes)
        {
            if (bytes >= 1024L * 1024 * 1024)
                return $"{bytes / (1024.0 * 1024 * 1024):F1} GB";
            if (bytes >= 1024L * 1024)
                return $"{bytes / (1024.0 * 1024):F1} MB";
            return $"{bytes / 1024.0:F0} KB";
        }
    }

    private long _totalDownloadBytes;
    private long _downloadedBytes;
    private string _currentDownloadFile;

    internal event Action<DownloadProgress> ProgressChanged;
    internal event Action<string> LogMessage;
}
