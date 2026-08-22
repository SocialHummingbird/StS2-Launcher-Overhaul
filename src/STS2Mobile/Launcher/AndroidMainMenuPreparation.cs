using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static class AndroidMainMenuPreparation
{
    private static AndroidMainMenuWorkingSetIdentity CaptureWorkingSetIdentity()
    {
        try
        {
            var attempt = LauncherLaunchMarkers.ReadLastLaunchAttempt();
            return attempt.Present
                ? AndroidMainMenuWorkingSetIdentity.Create(
                    attempt.SelectedBranch,
                    attempt.PckSha256,
                    attempt.ModPlayMode
                )
                : AndroidMainMenuWorkingSetIdentity.Missing();
        }
        catch (System.Exception ex)
        {
            PatchHelper.Log(
                $"[MainMenuPreparation] Runtime identity unavailable: {ex.Message}"
            );
            return AndroidMainMenuWorkingSetIdentity.Missing();
        }
    }

    internal static AndroidMainMenuWorkingSetIdentity ObserveCurrentMountedResourceSetIdentity()
    {
        var identity = CaptureWorkingSetIdentity();
        AndroidAtlasCompatibilityPatches.ObserveMountedResourceSetIdentity(
            identity.ResourceSetKey
        );
        return identity;
    }
}
