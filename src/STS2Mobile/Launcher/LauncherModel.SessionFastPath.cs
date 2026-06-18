using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal partial class LauncherModel
{
    // Loads saved credentials and determines the launcher path.
    internal FastPathResult StartSession()
    {
        _connectionResolved = false;
        return ResolveFastPath();
    }

    internal bool HasOwnershipMarker()
        => _steamSession.HasOwnershipMarker();

    private FastPathResult ResolveFastPath()
    {
        PatchHelper.Log("[Launcher] Fast path phase: credential store load");
        _credentialStore.Load();
        PatchHelper.Log("[Launcher] Fast path phase complete: credential store load");
        PatchHelper.Log("[Launcher] Fast path phase: cloud credential cache");
        LauncherCloudSaveState.SaveCredentials(_credentialStore);
        PatchHelper.Log("[Launcher] Fast path phase complete: cloud credential cache");

        PatchHelper.Log("[Launcher] Fast path phase: credential usability");
        var hasCredentials = _credentialStore.HasUsableCredentials();
        PatchHelper.Log("[Launcher] Fast path phase complete: credential usability");
        PatchHelper.Log("[Launcher] Fast path phase: ownership marker");
        var hasOwnershipMarker = _steamSession.HasOwnershipMarker();
        PatchHelper.Log("[Launcher] Fast path phase complete: ownership marker");
        PatchHelper.Log("[Launcher] Fast path phase: game files ready");
        var gameFilesReady = LauncherGameFiles.Ready(_dataDir);
        PatchHelper.Log("[Launcher] Fast path phase complete: game files ready");
        PatchHelper.Log(
            $"[Launcher] Fast path: creds={hasCredentials}, marker={hasOwnershipMarker}"
        );

        if (hasCredentials && hasOwnershipMarker && gameFilesReady)
            return FastPathResult.ReadyToLaunch;

        if (hasCredentials)
            return FastPathResult.AutoConnect;

        return FastPathResult.ShowLogin;
    }
}
