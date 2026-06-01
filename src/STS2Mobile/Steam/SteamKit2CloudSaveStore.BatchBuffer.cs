using System.Collections.Generic;

namespace STS2Mobile.Steam;

internal sealed partial class SteamKit2CloudSaveStore
{
    private readonly object _saveBatchLock = new();
    private readonly List<(string CanonPath, byte[] Bytes)> _saveBatchFiles = new();
    private bool _saveBatchCollecting;

    private void BeginCollectingSaveBatch()
    {
        lock (_saveBatchLock)
        {
            _saveBatchCollecting = true;
            _saveBatchFiles.Clear();
        }
    }

    private bool TryCollectSaveBatch(string canonPath, byte[] bytes)
    {
        lock (_saveBatchLock)
        {
            if (!_saveBatchCollecting)
                return false;

            _saveBatchFiles.Add((CanonPath: canonPath, Bytes: bytes));
            return true;
        }
    }

    private List<(string CanonPath, byte[] Bytes)> EndCollectingSaveBatch()
    {
        lock (_saveBatchLock)
        {
            _saveBatchCollecting = false;

            if (_saveBatchFiles.Count == 0)
                return new List<(string CanonPath, byte[] Bytes)>();

            var files = new List<(string CanonPath, byte[] Bytes)>(_saveBatchFiles);
            _saveBatchFiles.Clear();
            return files;
        }
    }
}
