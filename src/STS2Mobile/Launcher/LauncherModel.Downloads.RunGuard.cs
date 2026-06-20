using System.Threading;

namespace STS2Mobile.Launcher;

internal partial class LauncherModel
{
    private readonly struct DownloadRunGuard
    {
        private DownloadRunGuard(LauncherModel model, bool acquired)
        {
            Model = model;
            Acquired = acquired;
        }

        private LauncherModel Model { get; }
        internal bool Acquired { get; }

        internal static DownloadRunGuard TryAcquire(LauncherModel model)
            => new(
                model,
                Interlocked.Exchange(ref model._downloadRunning, 1) == 0
            );

        internal void Release()
        {
            if (Acquired)
                Interlocked.Exchange(ref Model._downloadRunning, 0);
        }
    }
}
