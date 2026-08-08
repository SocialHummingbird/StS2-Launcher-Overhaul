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
        PatchHelper.Log(
            $"[Launcher] Fast path: creds={hasCredentials}, marker={hasOwnershipMarker}"
        );

        if (hasCredentials)
        {
            if (!hasOwnershipMarker)
                return FastPathResult.AutoConnect();

            PatchHelper.Log("[Launcher] Fast path phase: selected downloaded-state readiness");
            var readiness = LauncherLaunchReadiness.EvaluateDownloadedState(
                _dataDir,
                LauncherPreferences.ReadGameBranch(),
                "session fast path downloaded-state readiness"
            );
            PatchHelper.Log("[Launcher] Fast path phase complete: selected downloaded-state readiness");

            if (readiness.Ready)
                return FastPathResult.Ready(readiness);

            return FastPathResult.AutoConnect();
        }

        return FastPathResult.ShowLogin();
    }
}
