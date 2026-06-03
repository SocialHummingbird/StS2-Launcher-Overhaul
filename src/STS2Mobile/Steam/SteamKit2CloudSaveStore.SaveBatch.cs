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

        _writeQueue.Enqueue(() =>
            UploadSaveBatch(files));
    }
}
