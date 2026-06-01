using System;
using System.Threading;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
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
        return new DownloadProgress(
            Interlocked.Read(ref _totalDownloadBytes),
            Interlocked.Read(ref _downloadedBytes),
            _currentDownloadFile
        );
    }
}
