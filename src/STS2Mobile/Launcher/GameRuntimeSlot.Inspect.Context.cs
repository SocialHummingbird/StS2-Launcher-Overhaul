using System.IO;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal sealed partial class GameRuntimeSlot
{
    private readonly struct GameRuntimeSlotInspectionContext
    {
        internal GameRuntimeSlotInspectionContext(string dataDir, string branch)
        {
            DataDir = dataDir;
            Branch = SteamGameBranch.Normalize(branch);
            DisplayName = SteamGameBranch.DisplayName(Branch);
            SlotKind = SteamGameInstallPaths.VersionSlotKind(Branch);
            SlotDirectory = SteamGameInstallPaths.VersionSlotDirectory(dataDir, Branch);
            GameDirectory = SteamGameInstallPaths.GameDirectory(dataDir, Branch);
            PckPath = Path.Combine(GameDirectory, LauncherStorageNames.GamePck);
            SourceAssemblyPath = FindSourceAssemblyPath(GameDirectory);
            ActiveAndroidAssemblyPath = FindActiveAndroidAssemblyPath(dataDir);
            ReleaseInfoPath = Path.Combine(GameDirectory, "release_info.json");
            BranchMarkerPath = SteamGameInstallPaths.BranchMarkerPath(dataDir, Branch);
            RuntimePackManifestPath = BuildRuntimePackManifestPath(dataDir, Branch);
        }

        internal string DataDir { get; }
        internal string Branch { get; }
        internal string DisplayName { get; }
        internal string SlotKind { get; }
        internal string SlotDirectory { get; }
        internal string GameDirectory { get; }
        internal string PckPath { get; }
        internal string SourceAssemblyPath { get; }
        internal string ActiveAndroidAssemblyPath { get; }
        internal string ReleaseInfoPath { get; }
        internal string BranchMarkerPath { get; }
        internal string RuntimePackManifestPath { get; }
    }
}
