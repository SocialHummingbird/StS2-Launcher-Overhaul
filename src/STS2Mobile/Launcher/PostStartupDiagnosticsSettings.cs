using System;
using System.IO;
using Godot;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static class PostStartupDiagnosticsSettings
{
    internal static bool DetailedTraceEnabled()
    {
        var markerExists = false;
        try
        {
            markerExists = File.Exists(
                Path.Combine(
                    OS.GetDataDir(),
                    LauncherStorageNames.DetailedPostStartupTrace
                )
            );
        }
        catch (Exception ex)
        {
            PatchHelper.Log(
                $"Detailed post-startup trace marker check failed: {ex.Message}"
            );
        }

        return PostStartupDiagnosticsPolicy.DetailedTraceEnabled(
            System.Environment.GetEnvironmentVariable(
                PostStartupDiagnosticsPolicy.EnvironmentVariable
            ),
            markerExists
        );
    }
}
