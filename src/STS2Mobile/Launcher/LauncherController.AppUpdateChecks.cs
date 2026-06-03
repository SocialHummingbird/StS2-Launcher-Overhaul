using System;
using System.Threading.Tasks;
using Godot;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private static readonly Color AppUpdateAvailableLogColor = new(1f, 0.85f, 0.2f);

    private async Task CheckForAppUpdatesAsync()
    {
        try
        {
            var latestVersion = await CheckLatestLauncherVersionAsync();
            if (latestVersion == null)
            {
                _runOnMainThread(ShowLauncherUpToDate);
                return;
            }

            _runOnMainThread(() => ShowLauncherUpdateAvailable(latestVersion));
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] App update check failed: {ex.Message}");
        }
    }

    private void ShowLauncherUpToDate()
        => _view.AppendLog("Launcher is up to date");

    private void ShowLauncherUpdateAvailable(string latestVersion)
    {
        _view.AppendColoredLog(
            $"Launcher update available: v{latestVersion} - "
                + $"download at {LauncherRepoReleasesPage}",
            AppUpdateAvailableLogColor
        );
        _view.SetStatus(
            $"Launcher update available! Visit GitHub to download v{latestVersion}"
        );
    }
}
