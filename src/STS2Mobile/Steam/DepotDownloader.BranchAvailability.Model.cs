using System;
using System.Collections.Generic;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private readonly struct BranchAvailability
    {
        internal BranchAvailability(
            string name,
            bool metadataVisible,
            string description,
            string buildId,
            string passwordRequired,
            int windowsManifestDepotCount
        )
        {
            Name = SteamGameBranch.Normalize(name);
            MetadataVisible = metadataVisible;
            Description = description ?? "";
            BuildId = buildId ?? "";
            PasswordRequired = passwordRequired ?? "unknown";
            WindowsManifestDepotCount = windowsManifestDepotCount;
        }

        internal string Name { get; }
        private bool MetadataVisible { get; }
        private string Description { get; }
        private string BuildId { get; }
        private string PasswordRequired { get; }
        internal int WindowsManifestDepotCount { get; }
        internal bool Downloadable => WindowsManifestDepotCount > 0
            && !PasswordRequired.Equals("true", StringComparison.OrdinalIgnoreCase);
        internal string DownloadabilityText
            => PasswordRequired.Equals("true", StringComparison.OrdinalIgnoreCase)
                ? "password-protected"
                : WindowsManifestDepotCount > 0
                    ? "downloadable"
                    : "no Windows manifest";

        internal string VisibilityText
            => MetadataVisible ? "visible in Steam branch metadata" : "not listed in Steam branch metadata";

        internal static BranchAvailability Missing(string branch)
            => new(branch, false, "", "", "unknown", 0);

        internal string Summary()
        {
            var details = new List<string>
            {
                $"windowsManifestDepots={WindowsManifestDepotCount}",
                $"metadataVisible={MetadataVisible.ToString().ToLowerInvariant()}",
                $"passwordRequired={SafeMarkerValue(PasswordRequired)}",
            };

            if (!string.IsNullOrWhiteSpace(BuildId))
                details.Add($"buildId={SafeMarkerValue(BuildId)}");

            if (!string.IsNullOrWhiteSpace(Description))
                details.Add($"description={SafeMarkerValue(Description)}");

            return $"{SafeMarkerValue(Name)} [{string.Join(", ", details)}]";
        }

        private static string SafeMarkerValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "";

            value = value
                .Replace('\r', ' ')
                .Replace('\n', ' ')
                .Replace('\t', ' ')
                .Trim();

            while (value.Contains("  ", StringComparison.Ordinal))
                value = value.Replace("  ", " ", StringComparison.Ordinal);

            return value.Length <= MaxBranchAvailabilityMarkerValueLength
                ? value
                : value[..MaxBranchAvailabilityMarkerValueLength] + "...";
        }
    }
}
