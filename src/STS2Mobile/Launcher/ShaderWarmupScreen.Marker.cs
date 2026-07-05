using System;
using System.IO;
using Godot;

namespace STS2Mobile.Launcher;

internal sealed partial class ShaderWarmupScreen
{
    private static string MarkerPath =>
        Path.Combine(OS.GetUserDataDir(), LauncherStorageNames.ShaderWarmupVersion);

    private static string StatusMarkerPath =>
        Path.Combine(OS.GetUserDataDir(), LauncherStorageNames.ShaderWarmupStatus);

    internal static bool NeedsWarmup()
    {
        try
        {
            if (File.Exists(MarkerPath))
            {
                var content = File.ReadAllText(MarkerPath).Trim();
                if (content == WarmupVersion.ToString())
                {
                    PatchHelper.Log(Message.MarkerMatches(content));
                    return false;
                }
                PatchHelper.Log(Message.MarkerMismatch(content, WarmupVersion));
            }
            else
            {
                PatchHelper.Log(Message.MarkerMissing());
            }

            return true;
        }
        catch (Exception ex)
        {
            PatchHelper.Log(Message.MarkerCheckFailed(ex));
            return true;
        }
    }

    private static void WriteWarmupVersion()
    {
        try
        {
            File.WriteAllText(MarkerPath, WarmupVersion.ToString());
        }
        catch (Exception ex)
        {
            PatchHelper.Log(Message.MarkerWriteFailed(ex));
        }
    }

    private static void WriteWarmupStatus(string status, string detail)
    {
        try
        {
            var text = new[]
            {
                "StS2 Mobile shader warmup status",
                $"UTC: {DateTime.UtcNow:O}",
                $"Status: {SanitizeStatus(status)}",
                $"Detail: {SanitizeStatus(detail)}",
                $"Warmup version: {WarmupVersion}",
            }.JoinLines();
            File.WriteAllText(StatusMarkerPath, text);
        }
        catch (Exception ex)
        {
            PatchHelper.Log(Message.StatusMarkerWriteFailed(ex));
        }
    }

    private static string SanitizeStatus(string value)
        => string.IsNullOrWhiteSpace(value)
            ? "<none>"
            : value.Replace('\r', ' ').Replace('\n', ' ').Trim();
}
