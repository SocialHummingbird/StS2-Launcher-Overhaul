using MegaCrit.Sts2.Core.Saves;

namespace STS2Mobile.Steam;

internal sealed partial class SteamKit2CloudSaveStore
{
    void ICloudSaveStore.BeginSaveBatch()
        => _saveBatch.BeginCollecting();

    void ICloudSaveStore.EndSaveBatch()
    {
        var files = _saveBatch.EndCollecting();
        if (files.Count == 0)
            return;

        EnqueueBatchUpload(files);
    }

    internal void EndSaveBatchAndUploadNow()
    {
        var files = _saveBatch.EndCollecting();
        if (files.Count == 0)
            return;

        PatchHelper.Log(UploadingBatchSynchronously(files.Count));
        UploadSaveBatch(files);
        PatchHelper.Log(UploadedBatchSynchronously(files.Count));
    }
}
