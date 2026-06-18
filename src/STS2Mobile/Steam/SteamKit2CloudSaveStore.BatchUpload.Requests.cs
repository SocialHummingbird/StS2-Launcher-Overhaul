using System.Collections.Generic;
using SteamKit2.Internal;

namespace STS2Mobile.Steam;

internal partial class SteamKit2CloudSaveStore
{
    private static CCloud_BeginAppUploadBatch_Request CreateBeginAppUploadBatchRequest(
        IReadOnlyList<SaveBatchFile> files
    )
    {
        var request = new CCloud_BeginAppUploadBatch_Request
        {
            appid = SteamCloudApp.AppId,
            machine_name = "android",
        };
        foreach (var file in files)
            request.files_to_upload.Add(file.CanonPath);

        return request;
    }

    private static CCloud_CompleteAppUploadBatch_Request CreateCompleteAppUploadBatchRequest(
        ulong batchId
    )
        => new()
        {
            appid = SteamCloudApp.AppId,
            batch_id = batchId,
            batch_eresult = (uint)SteamKit2.EResult.OK,
        };
}
