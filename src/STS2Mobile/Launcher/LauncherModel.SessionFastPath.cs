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
        _credentialStore.Load();
        LauncherCloudSaveState.SaveCredentials(_credentialStore);

        var hasCredentials = _credentialStore.HasCredentials;
        var hasOwnershipMarker = _steamSession.HasOwnershipMarker();
        var gameFilesReady = LauncherGameFiles.Ready(_dataDir);
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
