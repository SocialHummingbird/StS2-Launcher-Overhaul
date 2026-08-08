using System;
using MegaCrit.Sts2.Core.Saves;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static partial class LauncherCloudSaveState
{
    internal static SaveManager CreateAndroidGameplaySaveManager()
    {
        if (!OperatingSystem.IsAndroid())
            throw new PlatformNotSupportedException(
                "The Android gameplay SaveManager can only be created on Android."
            );

        var saveManager = new SaveManager(CloudSaveStoreFactory.CreateLocalStore());
        PatchHelper.Log("[Save] Created Android gameplay SaveManager with local storage only");
        return saveManager;
    }
}
