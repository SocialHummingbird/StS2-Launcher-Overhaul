using MegaCrit.Sts2.Core.Saves;

namespace STS2Mobile.Steam;

internal partial class SteamKit2CloudSaveStore
{
    // Gameplay no longer writes to Steam. These interface hooks remain no-ops;
    // launcher transfers await and verify every file individually.
    void ICloudSaveStore.BeginSaveBatch() { }

    void ICloudSaveStore.EndSaveBatch() { }
}
