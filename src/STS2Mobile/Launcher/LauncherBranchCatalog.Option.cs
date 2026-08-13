using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static partial class LauncherBranchCatalog
{
    internal readonly partial struct BranchOption
    {
        internal BranchOption(
            string branch,
            bool metadataVisible = false,
            int windowsManifestDepotCount = 0,
            string passwordRequired = "",
            string buildId = "",
            string description = "",
            string source = "fallback",
            bool isInstalled = false
        )
        {
            Branch = SteamGameBranch.Normalize(branch);
            MetadataVisible = metadataVisible;
            WindowsManifestDepotCount = windowsManifestDepotCount;
            PasswordRequired = passwordRequired ?? "";
            BuildId = buildId ?? "";
            Description = description ?? "";
            Source = source ?? "fallback";
            IsInstalled = isInstalled;
        }

        internal string Branch { get; }
        internal bool MetadataVisible { get; }
        internal int WindowsManifestDepotCount { get; }
        internal string PasswordRequired { get; }
        internal string BuildId { get; }
        internal string Description { get; }
        internal string Source { get; }
        internal bool IsInstalled { get; }

        internal BranchOption WithInstalled(bool isInstalled = true)
            => new(
                Branch,
                MetadataVisible,
                WindowsManifestDepotCount,
                PasswordRequired,
                BuildId,
                Description,
                Source,
                isInstalled
            );
    }
}
