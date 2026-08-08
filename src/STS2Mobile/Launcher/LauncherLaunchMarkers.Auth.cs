using System;
using System.IO;

namespace STS2Mobile.Launcher;

internal static partial class LauncherLaunchMarkers
{
    private static string SteamAuthFailurePath =>
        MarkerPath(LauncherStorageNames.SteamAuthFailure);

    internal static void RecordSteamAuthFailure(
        SteamAuthFailureReport report,
        string context
    )
    {
        TryWriteMarker(
            SteamAuthFailurePath,
            new[]
            {
                "StS2 Launcher Steam auth failure",
                $"UTC: {DateTime.UtcNow:O}",
                $"Context: {Sanitize(context)}",
                $"Category: {Sanitize(report.Category)}",
                $"User message: {Sanitize(report.UserMessage)}",
                $"Technical message: {Sanitize(report.TechnicalMessage)}",
                $"Selected branch: {Sanitize(SafeAuthBranch())}",
            }.JoinLines(),
            "Failed to write Steam auth failure marker"
        );

        RecordPhase("steam auth failure classified", $"{report.Category}: {report.TechnicalMessage}");
    }

    internal static string ReadSteamAuthFailure()
    {
        try
        {
            return File.Exists(SteamAuthFailurePath)
                ? File.ReadAllText(SteamAuthFailurePath)
                : null;
        }
        catch (Exception ex)
        {
            return $"Steam auth failure marker could not be read: {ex.Message}";
        }
    }

    private static string Sanitize(string value)
        => string.IsNullOrWhiteSpace(value)
            ? "<none>"
            : value.Replace('\r', ' ').Replace('\n', ' ').Trim();

    private static string SafeAuthBranch()
    {
        try
        {
            return LauncherPreferences.ReadGameBranch();
        }
        catch (Exception ex)
        {
            return $"<unavailable:{ex.GetType().Name}>";
        }
    }
}
