using System.IO;

namespace STS2Mobile.Patches
{
    internal static class PatchHelper
    {
        internal static void Log(string message)
        {
        }
    }
}

namespace STS2Mobile.Steam
{
    internal static class SteamGameBranch
    {
        internal const string SelectorMode = "test dropdown";

        internal static string Normalize(string branch)
            => string.IsNullOrWhiteSpace(branch)
                ? "public"
                : branch.Trim();

        internal static string SelectionKind(string branch)
            => Normalize(branch) == "public"
                ? "default"
                : "non-public";

        internal static string DisplayName(string branch)
            => Normalize(branch) == "public"
                ? "Default"
                : Normalize(branch);

        internal static string SelectorHelpText(string branch)
            => $"Test branch {Normalize(branch)}";
    }

    internal static class SteamGameInstallPaths
    {
        internal static string VersionSlotKind(string branch)
            => NormalizeSlot(branch) == "public"
                ? "default slot"
                : "version slot";

        internal static string VersionSlotDirectory(
            string dataDir,
            string branch
        )
            => Path.Combine(dataDir, "versions", NormalizeSlot(branch));

        private static string NormalizeSlot(string branch)
            => SteamGameBranch.Normalize(branch).ToLowerInvariant();
    }
}

namespace STS2Mobile.Launcher
{
    internal static class LauncherSaveOriginEvidence
    {
        internal static bool OriginWriteSucceeds = true;

        internal static bool TryWriteManualPullOrigin(
            string dataDir,
            string selectedBranch
        )
            => OriginWriteSucceeds;

        internal static bool CurrentLocalSavesMatchSelectedRuntime(
            string dataDir,
            string selectedBranch
        )
            => OriginWriteSucceeds;
    }

    internal static class LauncherLocalSaveEvidence
    {
        internal static bool HasImportantSaveEvidence(string dataDir)
            => true;
    }

    internal static class LauncherBranchSwitchSafety
    {
        internal static string MarkerPath(string dataDir)
            => Path.Combine(dataDir, "test_branch_switch.txt");
    }
}
