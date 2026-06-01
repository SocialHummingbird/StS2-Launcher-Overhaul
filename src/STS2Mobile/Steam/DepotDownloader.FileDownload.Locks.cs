using System.Collections.Concurrent;
using System.Threading;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _fileWriteLocks =
        new();

    private static SemaphoreSlim GetDepotFileWriteLock(
        (
            string FileName,
            string FilePath,
            string? FileDir,
            string TempPath,
            string LockKey
        ) target
    )
        => _fileWriteLocks.GetOrAdd(target.LockKey, _ => new SemaphoreSlim(1, 1));
}
