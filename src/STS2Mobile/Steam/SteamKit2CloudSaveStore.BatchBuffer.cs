using System.Collections.Generic;
using SteamKit2.Internal;

namespace STS2Mobile.Steam;

internal sealed partial class SteamKit2CloudSaveStore
{
    private readonly struct SaveBatchFile
    {
        private SaveBatchFile(string canonPath, byte[] bytes)
        {
            CanonPath = canonPath;
            Bytes = bytes;
        }

        private string CanonPath { get; }
        private byte[] Bytes { get; }

        internal static SaveBatchFile Create(string canonPath, byte[] bytes)
            => new(canonPath, bytes);

        internal void AddTo(CCloud_BeginAppUploadBatch_Request request)
            => request.files_to_upload.Add(CanonPath);

        internal void Upload(SteamKit2CloudSaveStore store, ulong batchId)
            => store.UploadWithRetry(CanonPath, Bytes, batchId);
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

            _saveBatchFiles.Add(SaveBatchFile.Create(canonPath, bytes));
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
