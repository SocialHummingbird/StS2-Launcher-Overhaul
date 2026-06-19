using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using STS2Mobile.Patches;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private const string AutomationFileName = "launcher_automation_action.txt";
    private const string AutomationMarkerFileName = "last_launcher_automation.txt";

    private void TryStartAutomation()
    {
        var request = LauncherAutomationRequest.TryConsume(_model.DataDir);
        if (!request.HasValue)
            return;

        _ = RunAutomationAsync(request.Value);
    }

    private async Task RunAutomationAsync(LauncherAutomationRequest request)
    {
        WriteAutomationMarker(request, "started");
        try
        {
            if (!string.IsNullOrWhiteSpace(request.Branch))
            {
                LauncherPreferences.SaveGameBranch(request.Branch);
                LauncherBranchAvailabilityStatus.Clear(_model.DataDir);
                RefreshGameBranchOptions();
            }

            _runOnMainThread(() =>
                _view.AppendLog($"[Automation] Running {request.Action} for {SteamGameBranch.DisplayName(LauncherPreferences.ReadGameBranch())}.")
            );

            if (request.RefreshCatalog)
            {
                _runOnMainThread(() => _view.SetRefreshGameVersionsBusy(true));
                await _model.RefreshBranchCatalogAsync().ConfigureAwait(false);
                _runOnMainThread(RefreshGameBranchOptions);
                _runOnMainThread(() => _view.SetRefreshGameVersionsBusy(false));
            }

            if (request.CheckUpdates)
                await _model.CheckForUpdatesAsync().ConfigureAwait(false);

            if (request.Redownload)
            {
                _model.ResetGameFilesForRedownload();
                _runOnMainThread(() =>
                    _view.AppendLog("[Automation] Selected game version cache cleared before replacement download.")
                );
            }

            if (request.Download)
                await _model.StartDownloadAsync().ConfigureAwait(false);

            if (request.LaunchSafe)
            {
                _runOnMainThread(() =>
                {
                    _view.AppendLog("[Automation] Safe launch requested after replacement download.");
                    RefreshSelectedRuntimeSlotEvidence();
                    _model.LaunchSafe();
                });
            }

            WriteAutomationMarker(request, "completed");
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Automation] {request.Action} failed: {ex}");
            WriteAutomationMarker(request, "failed", ex.GetBaseException().Message);
        }
        finally
        {
            _runOnMainThread(() => _view.SetRefreshGameVersionsBusy(false));
        }
    }

    private void WriteAutomationMarker(LauncherAutomationRequest request, string status, string message = "")
    {
        try
        {
            var path = Path.Combine(_model.DataDir, AutomationMarkerFileName);
            File.WriteAllText(
                path,
                $"UTC: {DateTimeOffset.UtcNow:O}\n"
                + $"Status: {status}\n"
                + $"Action: {request.Action}\n"
                + $"Selected branch: {SteamGameBranch.Normalize(LauncherPreferences.ReadGameBranch())}\n"
                + $"Requested branch: {request.Branch}\n"
                + $"Message: {message}\n"
            );
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Automation] Failed to write automation marker: {ex.Message}");
        }
    }

    private readonly struct LauncherAutomationRequest
    {
        private LauncherAutomationRequest(string action, string branch)
        {
            Action = action;
            Branch = SteamGameBranch.Normalize(branch);
        }

        internal string Action { get; }
        internal string Branch { get; }
        internal bool RefreshCatalog => Action.Contains("refresh", StringComparison.OrdinalIgnoreCase);
        internal bool CheckUpdates => Action.Contains("check", StringComparison.OrdinalIgnoreCase);
        internal bool Redownload => Action.Contains("redownload", StringComparison.OrdinalIgnoreCase);
        internal bool Download => Action.Contains("download", StringComparison.OrdinalIgnoreCase);
        internal bool LaunchSafe => Action.Contains("launchsafe", StringComparison.OrdinalIgnoreCase);

        internal static LauncherAutomationRequest? TryConsume(string dataDir)
        {
            var path = Path.Combine(dataDir, AutomationFileName);
            if (!File.Exists(path))
                return null;

            var lines = File.ReadAllLines(path)
                .Select(line => line.Trim())
                .Where(line => line.Length > 0 && !line.StartsWith("#", StringComparison.Ordinal))
                .ToArray();
            File.Delete(path);

            var action = ReadValue(lines, "action") ?? lines.FirstOrDefault() ?? "";
            var branch = ReadValue(lines, "branch") ?? SteamGameBranch.Public;
            return string.IsNullOrWhiteSpace(action)
                ? null
                : new LauncherAutomationRequest(action, branch);
        }

        private static string ReadValue(string[] lines, string key)
        {
            var prefix = key + "=";
            foreach (var line in lines)
            {
                if (line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return line[prefix.Length..].Trim();
            }

            return null;
        }
    }
}
