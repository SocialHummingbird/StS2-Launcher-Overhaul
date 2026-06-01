using System.Collections.Concurrent;
using System.Threading;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _fileWriteLocks =
        new();

    private static SemaphoreSlim GetDepotFileWriteLock(DepotFileTarget target)
        => _fileWriteLocks.GetOrAdd(target.LockKey, _ => new SemaphoreSlim(1, 1));
}
