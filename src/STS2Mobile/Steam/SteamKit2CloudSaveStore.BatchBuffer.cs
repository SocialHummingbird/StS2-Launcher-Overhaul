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

    private sealed class SaveBatchBuffer
    {
        private readonly object _lock = new();
        private readonly List<SaveBatchFile> _files = new();
        private bool _collecting;

        internal void BeginCollecting()
        {
            lock (_lock)
            {
                _collecting = true;
                _files.Clear();
            }
        }

        internal bool TryCollect(string canonPath, byte[] bytes)
        {
            lock (_lock)
            {
                if (!_collecting)
                    return false;

                _files.Add(new SaveBatchFile(canonPath, bytes));
                return true;
            }
        }

        internal List<SaveBatchFile> EndCollecting()
        {
            lock (_lock)
            {
                _collecting = false;

                if (_files.Count == 0)
                    return new List<SaveBatchFile>();

                var files = new List<SaveBatchFile>(_files);
                _files.Clear();
                return files;
            }
        }
    }

    private readonly SaveBatchBuffer _saveBatch = new();
}
