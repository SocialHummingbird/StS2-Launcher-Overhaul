using System;
using System.Collections.Generic;
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
            if (PreviousWarmupStatusSuggestsRenderCrash())
            {
                PatchHelper.Log("[ShaderWarmup] NeedsWarmup=true (previous status marker stopped during rendering)");
                return true;
            }

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

    internal static bool PreviousWarmupStatusSuggestsRenderCrash()
    {
        try
        {
            if (!File.Exists(StatusMarkerPath))
                return false;

            var status = File.ReadAllText(StatusMarkerPath);
            return ContainsStatus(status, "Status: rendering")
                || ContainsStatus(status, "Status: rendering-batch")
                || ContainsStatus(status, "Status: watchdog-warning");
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[ShaderWarmup] Failed to read previous warmup status marker: {ex.Message}");
            return false;
        }
    }

    private static void WriteWarmupStatus(string status, string detail, params string[] evidence)
    {
        try
        {
            var lines = new List<string>
            {
                "StS2 Launcher shader warmup status",
                $"UTC: {DateTime.UtcNow:O}",
                $"Status: {SanitizeStatus(status)}",
                $"Detail: {SanitizeStatus(detail)}",
                $"Warmup version: {WarmupVersion}",
                $"Warmup time budget seconds: {WarmupTimeBudgetSeconds}",
            };

            AppendDeviceDiagnostics(lines);

            foreach (var item in evidence)
                lines.Add(SanitizeStatus(item));

            File.WriteAllText(StatusMarkerPath, lines.ToArray().JoinLines());
        }
        catch (Exception ex)
        {
            PatchHelper.Log(Message.StatusMarkerWriteFailed(ex));
        }
    }

    private static string[] MergeEvidence(params string[][] groups)
    {
        var lines = new List<string>();
        foreach (var group in groups)
            lines.AddRange(group);
        return lines.ToArray();
    }

    private static string SanitizeStatus(string value)
        => string.IsNullOrWhiteSpace(value)
            ? "<none>"
            : value.Replace('\r', ' ').Replace('\n', ' ').Trim();

    private static bool ContainsStatus(string status, string value)
        => status?.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;

    private static void AppendDeviceDiagnostics(List<string> lines)
    {
        if (!OperatingSystem.IsAndroid())
            return;

        try
        {
            var diagnostics = AndroidGodotAppBridge.GetDeviceDiagnostics();
            if (string.IsNullOrWhiteSpace(diagnostics))
                return;

            foreach (var line in diagnostics.Split('\n'))
            {
                var clean = SanitizeStatus(line);
                if (clean != "<none>")
                    lines.Add(clean);
            }
        }
        catch (Exception ex)
        {
            lines.Add($"Device diagnostics: <unavailable:{ex.GetType().Name}>");
        }
    }
}
