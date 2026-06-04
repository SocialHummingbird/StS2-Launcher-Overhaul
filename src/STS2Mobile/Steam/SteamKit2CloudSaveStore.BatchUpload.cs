using System;
using System.Collections.Generic;
using SteamKit2.Internal;

namespace STS2Mobile.Steam;

internal sealed partial class SteamKit2CloudSaveStore
{
    private void UploadSaveBatch(IReadOnlyList<SaveBatchFile> files)
    {
        var batchId = TryBeginUploadBatch(files);
        UploadBatchFiles(files, batchId);
        if (batchId != 0)
            TryCompleteUploadBatch(batchId);
    }

    private ulong TryBeginUploadBatch(IReadOnlyList<SaveBatchFile> files)
    {
        try
        {
            return BeginUploadBatch(files);
        }
        catch (Exception ex)
        {
            PatchHelper.Log(BeginSaveBatchFailed(ex));
            return 0;
        }
    }

    private ulong BeginUploadBatch(IReadOnlyList<SaveBatchFile> files)
    {
        var result = SendCloudBlocking<
            CCloud_BeginAppUploadBatch_Request,
            CCloud_BeginAppUploadBatch_Response
        >("BeginAppUploadBatch", CreateBeginAppUploadBatchRequest(files));
        return result.batch_id;
    }

    private void UploadBatchFiles(
        IReadOnlyList<SaveBatchFile> files,
        ulong batchId
    )
    {
        foreach (var file in files)
            UploadWithRetry(file.CanonPath, file.Bytes, batchId);
    }

    private void TryCompleteUploadBatch(ulong batchId)
    {
        try
        {
            SendCloudBlocking<
                CCloud_CompleteAppUploadBatch_Request,
                CCloud_CompleteAppUploadBatch_Response
            >(
                "CompleteAppUploadBatchBlocking",
                CreateCompleteAppUploadBatchRequest(batchId)
            );
        }
        catch (Exception ex)
        {
            PatchHelper.Log(EndSaveBatchFailed(ex));
        }
    }
}
