using System;
using System.IO;
using System.Text.Json;

namespace STS2Mobile.Launcher;

internal sealed partial class RuntimePackManifest
{
    private static bool SourcePckMatches(string declaredSourcePckSha256, string selectedPckSha256, string selectedPckPath)
    {
        if (MatchesDeclared(declaredSourcePckSha256, selectedPckSha256))
            return true;

        if (string.IsNullOrWhiteSpace(declaredSourcePckSha256)
            || string.IsNullOrWhiteSpace(selectedPckPath)
            || !File.Exists(selectedPckPath))
        {
            return false;
        }

        var markerPath = System.IO.Path.Combine(
            System.IO.Path.GetDirectoryName(selectedPckPath) ?? string.Empty,
            AndroidPckPatchMarkerFileName
        );
        if (!File.Exists(markerPath))
            return false;

        try
        {
            if (File.GetLastWriteTimeUtc(markerPath) < File.GetLastWriteTimeUtc(selectedPckPath))
                return false;

            var markerText = File.ReadAllText(markerPath);
            if (string.IsNullOrWhiteSpace(markerText))
                return true;

            using var document = JsonDocument.Parse(markerText);
            if (!document.RootElement.TryGetProperty("sourcePckSha256", out var sourceHash)
                || sourceHash.ValueKind != JsonValueKind.String)
            {
                return true;
            }

            return string.Equals(sourceHash.GetString(), declaredSourcePckSha256, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static bool MatchesDeclared(string declaredValue, string actualValue)
        => !string.IsNullOrWhiteSpace(declaredValue)
        && !string.IsNullOrWhiteSpace(actualValue)
        && !actualValue.StartsWith("<", StringComparison.Ordinal)
        && string.Equals(declaredValue, actualValue, StringComparison.OrdinalIgnoreCase);
}
