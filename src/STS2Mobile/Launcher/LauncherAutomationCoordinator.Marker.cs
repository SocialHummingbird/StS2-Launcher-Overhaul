using System;
using System.IO;
using STS2Mobile.Patches;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherAutomationCoordinator
{
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
}
