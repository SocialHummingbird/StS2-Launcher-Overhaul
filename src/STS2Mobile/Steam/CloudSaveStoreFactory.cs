using System;
using MegaCrit.Sts2.Core.Saves;

namespace STS2Mobile.Steam;

internal static class CloudSaveStoreFactory
{
    internal static CloudSaveStore CreateCloudSaveStore(string accountName, string refreshToken)
        => new(CreateLocalStore(), CreateCloudStore(accountName, refreshToken));

    private static ISaveStore CreateLocalStore()
        => OperatingSystem.IsAndroid()
            ? new AndroidLocalSaveStore()
            : new GodotFileIo(UserDataPathProvider.GetAccountScopedBasePath(null));

    private static ICloudSaveStore CreateCloudStore(string accountName, string refreshToken)
        => SteamKit2CloudSaveStore.GetOrCreate(accountName, refreshToken);
}
