using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace STS2Mobile.Launcher;

internal static partial class LauncherRuntimePatchValidationEvidence
{
    internal static string Status(string dataDir)
        => ReadString(dataDir, "status");

    internal static string Utc(string dataDir)
        => ReadString(dataDir, "utc");

    internal static bool UtcParseable(string dataDir)
        => DateTime.TryParse(
            Utc(dataDir),
            CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal,
            out _
        );

    internal static string SelectedBranch(string dataDir)
        => ReadString(dataDir, "selectedBranch");

    internal static string SelectedVersion(string dataDir)
        => ReadString(dataDir, "selectedVersion");

    internal static string SelectedPckSha256(string dataDir)
        => ReadString(dataDir, "selectedPckSha256");

    internal static string SelectedSourceAssemblySha256(string dataDir)
        => ReadString(dataDir, "selectedSourceAssemblySha256");

    internal static string RuntimeSlotId(string dataDir)
        => ReadString(dataDir, "runtimeSlotId");

    internal static string ActiveAndroidAssemblySha256(string dataDir)
        => ReadString(dataDir, "activeAndroidAssemblySha256");

    internal static string RuntimePackId(string dataDir)
        => ReadString(dataDir, "runtimePackId");

    internal static string RuntimePackStatus(string dataDir)
        => ReadString(dataDir, "runtimePackStatus");

    internal static string AppliedPatchCount(string dataDir)
        => ReadString(dataDir, "appliedPatchCount");

    internal static string FailedPatchCount(string dataDir)
        => ReadString(dataDir, "failedPatchCount");

    internal static string TotalPatchCount(string dataDir)
        => ReadString(dataDir, "totalPatchCount");

    internal static string FailureMessages(string dataDir)
    {
        try
        {
            if (!File.Exists(MarkerPath(dataDir)))
                return "<none>";

            using var document = JsonDocument.Parse(File.ReadAllText(MarkerPath(dataDir)));
            if (!document.RootElement.TryGetProperty("failureMessages", out var failures)
                || failures.ValueKind != JsonValueKind.Array)
            {
                return "<missing>";
            }

            var selected = failures
                .EnumerateArray()
                .Where(x => x.ValueKind == JsonValueKind.String)
                .Select(x => x.GetString())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Take(10)
                .ToArray();
            return selected.Length == 0 ? "<none>" : string.Join(" | ", selected);
        }
        catch
        {
            return "<read failed>";
        }
    }

    private static string ReadString(string dataDir, string property)
    {
        try
        {
            if (!File.Exists(MarkerPath(dataDir)))
                return "<none>";

            using var document = JsonDocument.Parse(File.ReadAllText(MarkerPath(dataDir)));
            if (!document.RootElement.TryGetProperty(property, out var value))
                return "<missing>";

            return value.ValueKind switch
            {
                JsonValueKind.String => value.GetString() ?? "<missing>",
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Number => value.ToString(),
                _ => "<unsupported>"
            };
        }
        catch
        {
            return "<read failed>";
        }
    }
}
