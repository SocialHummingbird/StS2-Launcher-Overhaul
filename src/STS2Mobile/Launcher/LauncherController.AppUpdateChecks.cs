using System;
using System.Threading.Tasks;
using Godot;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private static readonly Color AppUpdateAvailableLogColor = new(1f, 0.85f, 0.2f);

    private readonly struct LauncherUpdateResult
    {
        private LauncherUpdateResult(string? latestVersion)
        {
            LatestVersion = latestVersion;
        }

        private string? LatestVersion { get; }
        private bool HasUpdate => LatestVersion != null;

        internal static LauncherUpdateResult FromLatestVersion(string? latestVersion)
            => new(latestVersion);

        internal void Show(LauncherController controller)
        {
            if (HasUpdate)
            {
                controller.ShowLauncherUpdateAvailable(LatestVersion ?? "");
                return;
            }

            controller.ShowLauncherUpToDate();
        }
    }

    private async Task CheckForAppUpdatesAsync()
    {
        try
        {
            var result = LauncherUpdateResult.FromLatestVersion(
                await CheckLatestLauncherVersionAsync()
            );
            _runOnMainThread(() => result.Show(this));
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
