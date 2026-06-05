using System;
using System.Collections.Generic;
using SteamKit2.Internal;

namespace STS2Mobile.Steam;

internal sealed partial class SteamKit2CloudSaveStore
{
    private readonly struct SaveUploadBatch
    {
        private SaveUploadBatch(
            SteamKit2CloudSaveStore owner,
            IReadOnlyList<SaveBatchFile> files,
            ulong batchId
        )
        {
            Owner = owner;
            Files = files;
            BatchId = batchId;
        }

        private SteamKit2CloudSaveStore Owner { get; }
        private IReadOnlyList<SaveBatchFile> Files { get; }
        private ulong BatchId { get; }
        private bool Started => BatchId != 0;

        internal static SaveUploadBatch Start(
            SteamKit2CloudSaveStore owner,
            IReadOnlyList<SaveBatchFile> files
        )
            => new(owner, files, TryBegin(owner, files));

        internal void Upload()
        {
            UploadFiles();
            if (Started)
                TryComplete();
        }

        private static ulong TryBegin(
            SteamKit2CloudSaveStore owner,
            IReadOnlyList<SaveBatchFile> files
        )
        {
            try
            {
                return owner.BeginUploadBatch(files);
            }
            catch (Exception ex)
            {
                PatchHelper.Log(BeginSaveBatchFailed(ex));
                return 0;
            }
        }

        private void UploadFiles()
        {
            foreach (var file in Files)
                Owner.UploadWithRetry(file.CanonPath, file.Bytes, BatchId);
        }

        private void TryComplete()
        {
            try
            {
                Owner.CompleteUploadBatch(BatchId);
            }
            catch (Exception ex)
            {
                PatchHelper.Log(EndSaveBatchFailed(ex));
            }
        }
    }

    private void UploadSaveBatch(IReadOnlyList<SaveBatchFile> files)
        => SaveUploadBatch.Start(this, files).Upload();

    private ulong BeginUploadBatch(IReadOnlyList<SaveBatchFile> files)
    {
        var result = SendCloudBlocking<
            CCloud_BeginAppUploadBatch_Request,
            CCloud_BeginAppUploadBatch_Response
        >("BeginAppUploadBatch", CreateBeginAppUploadBatchRequest(files));
        return result.batch_id;
    }

    private void CompleteUploadBatch(ulong batchId)
        => SendCloudBlocking<
            CCloud_CompleteAppUploadBatch_Request,
            CCloud_CompleteAppUploadBatch_Response
        >(
            "CompleteAppUploadBatchBlocking",
            CreateCompleteAppUploadBatchRequest(batchId)
        );
}
