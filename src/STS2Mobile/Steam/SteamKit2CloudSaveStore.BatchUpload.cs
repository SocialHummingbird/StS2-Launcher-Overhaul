using System;
using System.Collections.Generic;
using SteamKit2.Internal;

namespace STS2Mobile.Steam;

internal sealed partial class SteamKit2CloudSaveStore
{
    private readonly struct UploadBatchSession
    {
        private UploadBatchSession(ulong id)
        {
            Id = id;
        }

        private ulong Id { get; }
        private bool HasId => Id != 0;

        internal static UploadBatchSession Started(ulong id)
            => new(id);

        internal static UploadBatchSession NotStarted()
            => new(id: 0);

        internal void Complete(SteamKit2CloudSaveStore store)
        {
            if (HasId)
                store.CompleteUploadBatch(Id);
        }

        internal void Upload(SteamKit2CloudSaveStore store, SaveBatchFile file)
            => file.Upload(store, Id);
    }

    private void UploadSaveBatch(IReadOnlyList<SaveBatchFile> files)
    {
        var session = BeginUploadBatchOrNone(files);
        UploadBatchFiles(files, session);
        session.Complete(this);
    }

    private UploadBatchSession BeginUploadBatchOrNone(IReadOnlyList<SaveBatchFile> files)
    {
        try
        {
            return UploadBatchSession.Started(BeginUploadBatch(files));
        }
        catch (Exception ex)
        {
            PatchHelper.Log(BeginSaveBatchFailed(ex));
            return UploadBatchSession.NotStarted();
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
            file.AddTo(request);

        var result = SendCloudBlocking<
            CCloud_BeginAppUploadBatch_Request,
            CCloud_BeginAppUploadBatch_Response
        >("BeginAppUploadBatch", request);
        return result.batch_id;
    }

    private void UploadBatchFiles(
        IReadOnlyList<SaveBatchFile> files,
        UploadBatchSession session
    )
    {
        foreach (var file in files)
            session.Upload(this, file);
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
