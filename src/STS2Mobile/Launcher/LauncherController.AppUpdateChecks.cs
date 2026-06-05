using System;
using System.Threading.Tasks;
using Godot;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private static readonly Color AppUpdateAvailableLogColor = new(1f, 0.85f, 0.2f);

    private readonly struct LauncherUpdateCheckResult
    {
        private LauncherUpdateCheckResult(string latestVersion)
        {
            LatestVersion = latestVersion;
        }

        private string LatestVersion { get; }
        private bool HasUpdate => LatestVersion != null;

        internal static LauncherUpdateCheckResult Available(
            LauncherVersion latestVersion
        )
            => new(latestVersion.ToString());

        internal static LauncherUpdateCheckResult UpToDate()
            => new(latestVersion: null);

        internal void Show(LauncherController controller)
        {
            if (HasUpdate)
            {
                ShowUpdateAvailable(controller);
                return;
            }

            controller._view.AppendLog("Launcher is up to date");
        }

        private void ShowUpdateAvailable(LauncherController controller)
        {
            controller._view.AppendColoredLog(
                $"Launcher update available: v{LatestVersion} - "
                    + $"download at {LauncherRepoReleasesPage}",
                AppUpdateAvailableLogColor
            );
            controller._view.SetStatus(
                $"Launcher update available! Visit GitHub to download v{LatestVersion}"
            );
        }
    }

    private async Task CheckForAppUpdatesAsync()
    {
        try
        {
            var updateCheck = await CheckLatestLauncherVersionAsync();
            _runOnMainThread(() => updateCheck.Show(this));
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] App update check failed: {ex.Message}");
        }
    }
}
