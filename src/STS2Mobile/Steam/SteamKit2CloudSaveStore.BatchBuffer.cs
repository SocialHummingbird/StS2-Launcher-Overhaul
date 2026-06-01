using System.Collections.Generic;

namespace STS2Mobile.Steam;

internal sealed partial class SteamKit2CloudSaveStore
{
    private readonly struct SaveBatchFile
    {
        internal SaveBatchFile(string canonPath, byte[] bytes)
        {
            CanonPath = canonPath;
            Bytes = bytes;
        }

        internal string CanonPath { get; }
        internal byte[] Bytes { get; }
    }

    private readonly object _saveBatchLock = new();
    private readonly List<SaveBatchFile> _saveBatchFiles = new();
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

            _saveBatchFiles.Add(new SaveBatchFile(canonPath, bytes));
            return true;
        }
    }

    private List<SaveBatchFile> EndCollectingSaveBatch()
    {
        lock (_saveBatchLock)
        {
            _saveBatchCollecting = false;

            if (_saveBatchFiles.Count == 0)
                return new List<SaveBatchFile>();

            var files = new List<SaveBatchFile>(_saveBatchFiles);
            _saveBatchFiles.Clear();
            return files;
        }
    }
}
