using System;
using System.Collections.Generic;
using SteamKit2.Internal;

namespace STS2Mobile.Steam;

internal sealed partial class SteamKit2CloudSaveStore
{
    private void UploadSaveBatch(List<(string canonPath, byte[] bytes)> files)
    {
        if (!TryBeginUploadBatch(files, out var batchId))
        {
            UploadBatchFilesIndividually(files);
            return;
        }

        foreach (var (canonPath, bytes) in files)
            UploadWithRetry(canonPath, bytes, batchId);

        CompleteUploadBatch(batchId);
    }

    private bool TryBeginUploadBatch(
        List<(string canonPath, byte[] bytes)> files,
        out ulong batchId
    )
    {
        try
        {
            batchId = BeginUploadBatch(files);
            return true;
        }
        catch (Exception ex)
        {
            PatchHelper.Log(StoreMessage.BeginSaveBatchFailed(ex));
            batchId = 0;
            return false;
        }
    }

    private ulong BeginUploadBatch(List<(string canonPath, byte[] bytes)> files)
    {
        var request = new CCloud_BeginAppUploadBatch_Request
        {
            appid = SteamCloudApp.AppId,
            machine_name = "android",
        };
        foreach (var (canonPath, _) in files)
            request.files_to_upload.Add(canonPath);

        var result = SendCloudBlocking<
            CCloud_BeginAppUploadBatch_Request,
            CCloud_BeginAppUploadBatch_Response
        >("BeginAppUploadBatch", request);
        return result.batch_id;
    }

    private void UploadBatchFilesIndividually(List<(string canonPath, byte[] bytes)> files)
    {
        foreach (var (canonPath, bytes) in files)
            UploadWithRetry(canonPath, bytes);
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
            PatchHelper.Log(StoreMessage.EndSaveBatchFailed(ex));
        }
    }
}
