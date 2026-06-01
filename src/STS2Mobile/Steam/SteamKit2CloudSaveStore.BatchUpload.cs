using System;
using System.Collections.Generic;
using SteamKit2.Internal;

namespace STS2Mobile.Steam;

internal sealed partial class SteamKit2CloudSaveStore
{
    private void UploadSaveBatch(IReadOnlyList<SaveBatchFile> files)
    {
        var batchId = BeginUploadBatchOrNull(files);
        UploadBatchFiles(files, batchId);

        if (batchId.HasValue)
            CompleteUploadBatch(batchId.Value);
    }

    private ulong? BeginUploadBatchOrNull(IReadOnlyList<SaveBatchFile> files)
    {
        try
        {
            return BeginUploadBatch(files);
        }
        catch (Exception ex)
        {
            PatchHelper.Log(BeginSaveBatchFailed(ex));
            return null;
        }
    }

    private ulong BeginUploadBatch(IReadOnlyList<SaveBatchFile> files)
    {
        var request = new CCloud_BeginAppUploadBatch_Request
        {
            appid = SteamCloudApp.AppId,
            machine_name = "android",
        };
        foreach (var file in files)
            request.files_to_upload.Add(file.CanonPath);

        var result = SendCloudBlocking<
            CCloud_BeginAppUploadBatch_Request,
            CCloud_BeginAppUploadBatch_Response
        >("BeginAppUploadBatch", request);
        return result.batch_id;
    }

    private void UploadBatchFiles(IReadOnlyList<SaveBatchFile> files, ulong? batchId)
    {
        foreach (var file in files)
            UploadWithRetry(file.CanonPath, file.Bytes, batchId.GetValueOrDefault());
    }

    private void CompleteUploadBatch(ulong batchId)
    {
        try
        {
            SendCloudBlocking<
                CCloud_CompleteAppUploadBatch_Request,
                CCloud_CompleteAppUploadBatch_Response
            >(
                "CompleteAppUploadBatchBlocking",
                new CCloud_CompleteAppUploadBatch_Request
                {
                    appid = SteamCloudApp.AppId,
                    batch_id = batchId,
                    batch_eresult = (uint)SteamKit2.EResult.OK,
                }
            );
        }
        catch (Exception ex)
        {
            PatchHelper.Log(EndSaveBatchFailed(ex));
        }
    }
}
