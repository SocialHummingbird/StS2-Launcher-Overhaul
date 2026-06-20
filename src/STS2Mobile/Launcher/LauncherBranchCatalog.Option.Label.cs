using System;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static partial class LauncherBranchCatalog
{
    internal readonly partial struct BranchOption
    {
        internal string Label
            => DropdownLabelWithMetadata();

        private string DropdownLabelWithMetadata()
        {
            var label = SteamGameBranch.DropdownLabel(Branch);
            if (string.Equals(Source, "local install", StringComparison.OrdinalIgnoreCase))
                return $"{label} (installed)";

            if (!string.Equals(Source, "Steam app-info", StringComparison.OrdinalIgnoreCase))
                return label;

            if (string.Equals(Branch, SteamGameBranch.Public, StringComparison.OrdinalIgnoreCase))
                return WindowsManifestDepotCount > 0 ? $"{label} (ready)" : label;

            if (PasswordRequired.Equals("true", StringComparison.OrdinalIgnoreCase))
                return $"{label} (password)";

            if (WindowsManifestDepotCount <= 0)
                return $"{label} (unavailable)";

            return !string.IsNullOrWhiteSpace(BuildId)
                ? $"{label} (build {BuildId})"
                : $"{label} (ready)";
        }
    }
}
