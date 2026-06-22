using System;
using System.IO;
using System.Linq;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
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
        internal bool WorkshopClear => Action.Contains("workshopclear", StringComparison.OrdinalIgnoreCase);
        internal bool WorkshopSync => Action.Contains("workshopsync", StringComparison.OrdinalIgnoreCase);

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
