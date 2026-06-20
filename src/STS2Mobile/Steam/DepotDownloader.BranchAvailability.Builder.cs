using System;
using SteamKit2;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private sealed class BranchAvailabilityBuilder
    {
        private int _windowsManifestDepotCount;
        private string _description = "";
        private string _buildId = "";
        private string _passwordRequired = "unknown";
        private bool _metadataVisible;

        internal BranchAvailabilityBuilder(string name)
        {
            Name = SteamGameBranch.Normalize(name);
        }

        private string Name { get; }

        internal void ApplyMetadata(KeyValue branch)
        {
            _metadataVisible = true;
            _description = FirstValue(branch, "description", "desc");
            _buildId = FirstValue(branch, "buildid", "build_id");
            _passwordRequired = BoolValue(branch, "pwdrequired", "passwordrequired", "password_required", "protected");
        }

        internal void ApplyManifest(ulong? manifestId)
        {
            if (manifestId.HasValue && manifestId.Value != 0)
                _windowsManifestDepotCount++;
        }

        internal BranchAvailability Build()
            => new(
                Name,
                _metadataVisible,
                _description,
                _buildId,
                _passwordRequired,
                _windowsManifestDepotCount
            );

        private static string FirstValue(KeyValue branch, params string[] keys)
        {
            foreach (var key in keys)
            {
                var value = branch[key];
                if (value != KeyValue.Invalid && !string.IsNullOrWhiteSpace(value.Value))
                    return value.Value.Trim();
            }

            return "";
        }

        private static string BoolValue(KeyValue branch, params string[] keys)
        {
            var value = FirstValue(branch, keys);
            if (string.IsNullOrWhiteSpace(value))
                return "unknown";

            return value.Equals("1", StringComparison.OrdinalIgnoreCase)
                || value.Equals("true", StringComparison.OrdinalIgnoreCase)
                || value.Equals("yes", StringComparison.OrdinalIgnoreCase)
                ? "true"
                : "false";
        }
    }
}
