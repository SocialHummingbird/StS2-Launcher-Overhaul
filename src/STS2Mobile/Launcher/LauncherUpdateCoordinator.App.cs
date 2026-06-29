using System;
using System.Threading.Tasks;
using Godot;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherUpdateCoordinator
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

        internal void Show(LauncherView view)
        {
            if (HasUpdate)
            {
                ShowUpdateAvailable(view);
                return;
            }

            view.AppendLog("Launcher is up to date");
        }

        private void ShowUpdateAvailable(LauncherView view)
        {
            view.AppendColoredLog(
                $"Launcher update available: v{LatestVersion} - "
                    + $"download at {LauncherRepoReleasesPage}",
                AppUpdateAvailableLogColor
            );
            view.SetStatus(
                $"Launcher update available! Visit GitHub to download v{LatestVersion}"
            );
        }
    }

    private async Task CheckForAppUpdatesAsync()
    {
        try
        {
            var updateCheck = await CheckLatestLauncherVersionAsync();
            _runOnMainThread(() => updateCheck.Show(_view));
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] App update check failed: {ex.Message}");
        }
    }
}
