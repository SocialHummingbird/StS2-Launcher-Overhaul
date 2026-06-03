using System;
using System.Threading.Tasks;
using Godot;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private readonly struct AppUpdateUi
    {
        private static readonly Color UpdateAvailableLogColor = new(1f, 0.85f, 0.2f);

        internal static AppUpdateUi Create()
            => new();

        internal void ShowUpToDate(LauncherView view)
            => view.AppendLog("Launcher is up to date");

        internal void ShowUpdateAvailable(LauncherView view, string latestVersion)
        {
            view.AppendColoredLog(
                $"Launcher update available: v{latestVersion} - "
                    + $"download at {LauncherRepoReleasesPage}",
                UpdateAvailableLogColor
            );
            view.SetStatus(
                $"Launcher update available! Visit GitHub to download v{latestVersion}"
            );
        }
    }

    private async Task CheckForAppUpdatesAsync()
    {
        try
        {
            var latestVersion = await CheckLatestLauncherVersionAsync();
            if (latestVersion == null)
            {
                _runOnMainThread(() => AppUpdateUi.Create().ShowUpToDate(_view));
                return;
            }

            _runOnMainThread(
                () => AppUpdateUi.Create().ShowUpdateAvailable(_view, latestVersion)
            );
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] App update check failed: {ex.Message}");
        }
    }
}
