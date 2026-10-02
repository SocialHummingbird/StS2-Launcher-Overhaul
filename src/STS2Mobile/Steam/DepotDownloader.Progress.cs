using System;
using System.Threading;

namespace STS2Mobile.Steam;
internal sealed partial class DepotDownloader
{
    internal readonly struct DownloadProgress
    {
        private DownloadProgress(long totalBytes, long downloadedBytes, string currentFile, bool statusOnly = false)
        {
            TotalBytes = totalBytes;
            DownloadedBytes = downloadedBytes;
            CurrentFile = currentFile;
            StatusOnly = statusOnly;
        }

        private long TotalBytes { get; }
        private long DownloadedBytes { get; }
        private string CurrentFile { get; }
        private bool StatusOnly { get; }
        private double Percentage => TotalBytes > 0 ? (double)DownloadedBytes / TotalBytes * 100.0 : 0;

        internal static DownloadProgress Create(long totalBytes, long downloadedBytes, string currentFile) => new(totalBytes, downloadedBytes, currentFile);
        internal static DownloadProgress Status(string message) => new(0, 0, message, statusOnly: true);
        internal void ApplyTo(Action<double, string> setProgress, Action<string> appendLog)
        {
            if (StatusOnly)
            {
                setProgress(0, CurrentFile);
                return;
            }

            setProgress(Percentage, $"{FormatSize(DownloadedBytes)} / {FormatSize(TotalBytes)} ({Percentage:F1}%)");
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
    private static string FormatBytes(long bytes)
    {
        const long kilobyte = 1024L;
        const long megabyte = kilobyte * 1024L;
        const long gigabyte = megabyte * 1024L;
        if (bytes >= gigabyte)
            return $"{bytes / (double)gigabyte:F1} GB";
        if (bytes >= megabyte)
            return $"{bytes / (double)megabyte:F1} MB";
        if (bytes >= kilobyte)
            return $"{bytes / (double)kilobyte:F1} KB";
        return $"{bytes} B";
    }

    private void Log(string msg)
    {
        PatchHelper.Log($"[Depot] {msg}");
        try
        {
            LogMessage?.Invoke(msg);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Depot] Log callback failed: {ex.Message}");
        }
    }

    private long _lastProgressReportTicks;
    private void ReportProgress()
    {
        if (OperatingSystem.IsAndroid() && !CanReportAndroidProgress())
            return;
        InvokeProgressChanged(SnapshotProgress());
    }

    private void ForceReportProgress()
    {
        Interlocked.Exchange(ref _lastProgressReportTicks, DateTime.UtcNow.Ticks);
        InvokeProgressChanged(SnapshotProgress());
    }

    private bool CanReportAndroidProgress()
    {
        var now = DateTime.UtcNow.Ticks;
        var last = Interlocked.Read(ref _lastProgressReportTicks);
        if (now - last < TimeSpan.TicksPerMillisecond * 250)
            return false;
        return Interlocked.CompareExchange(ref _lastProgressReportTicks, now, last) == last;
    }

    private void InvokeProgressChanged(DownloadProgress progress)
    {
        try
        {
            ProgressChanged?.Invoke(progress);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Depot] Progress callback failed: {ex.Message}");
        }
    }

    private DownloadProgress SnapshotProgress()
    {
        return DownloadProgress.Create(Interlocked.Read(ref _totalDownloadBytes), Interlocked.Read(ref _downloadedBytes), _currentDownloadFile);
    }
}
