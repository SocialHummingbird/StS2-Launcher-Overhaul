using STS2Mobile.Patches;
using System;
using System.IO;
using System.Security.Cryptography;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;
internal sealed partial class GameRuntimeSlot
{
    internal static GameRuntimeSlot Inspect(string dataDir, string branch)
    {
        var context = new GameRuntimeSlotInspectionContext(dataDir, branch);
        PatchHelper.Log($"[Launcher] Runtime slot inspect phase: paths for branch '{branch}'");
        PatchHelper.Log($"[Launcher] Runtime slot inspect phase complete: paths pck='{context.PckPath}' source='{context.SourceAssemblyPath}' active='{context.ActiveAndroidAssemblyPath}' manifest='{context.RuntimePackManifestPath}'");
        PatchHelper.Log("[Launcher] Runtime slot inspect phase: authoritative installed game identity");
        GameIdentity gameIdentity;
        try
        {
            gameIdentity = GameIdentityReader.ReadInstalled(dataDir, context.Branch);
        }
        catch (GameIdentityException ex)
        {
            PatchHelper.Log($"[Launcher] Runtime slot inspect phase failed: authoritative installed game identity -> {ex.Kind}: {ex.Message}");
            var incompleteSlot = BuildIncompleteRuntimeSlot(context, ex.Message);
            PatchHelper.Log("[Launcher] Runtime slot inspect phase complete: current game identity unavailable");
            return incompleteSlot;
        }

        return Inspect(dataDir, context, gameIdentity);
    }

    internal static GameRuntimeSlot Inspect(string dataDir, GameIdentity gameIdentity)
    {
        if (gameIdentity == null)
            throw new System.ArgumentNullException(nameof(gameIdentity));
        var context = new GameRuntimeSlotInspectionContext(dataDir, gameIdentity.Branch);
        PatchHelper.Log($"[Launcher] Runtime slot inspect using supplied authoritative identity {gameIdentity.Id} for branch '{gameIdentity.Branch}'");
        return Inspect(dataDir, context, gameIdentity);
    }

    private static GameRuntimeSlot Inspect(string dataDir, GameRuntimeSlotInspectionContext context, GameIdentity gameIdentity)
    {
        var pckSha256 = gameIdentity.PckSha256;
        var sourceAssemblySha256 = gameIdentity.SourceAssemblySha256;
        PatchHelper.Log($"[Launcher] Runtime slot inspect phase complete: authoritative installed game identity -> {gameIdentity.Id} generation={gameIdentity.InstallGeneration} PCK={pckSha256} source={sourceAssemblySha256}");
        PatchHelper.Log("[Launcher] Runtime slot inspect phase: active Android assembly SHA256");
        var activeAndroidAssemblySha256 = Sha256OrMissing(context.ActiveAndroidAssemblyPath);
        PatchHelper.Log($"[Launcher] Runtime slot inspect phase complete: active Android assembly SHA256 -> {activeAndroidAssemblySha256}");
        PatchHelper.Log("[Launcher] Runtime slot inspect phase: metadata");
        var metadata = RuntimeSlotMetadata.Inspect(context.ReleaseInfoPath, context.BranchMarkerPath);
        PatchHelper.Log("[Launcher] Runtime slot inspect phase complete: metadata");
        PatchHelper.Log("[Launcher] Runtime slot inspect phase: runtime pack manifest");
        var runtimePack = RuntimePackManifest.Inspect(context.RuntimePackManifestPath, gameIdentity);
        PatchHelper.Log($"[Launcher] Runtime slot inspect phase complete: runtime pack manifest -> {runtimePack?.Status ?? "<none>"}");
        PatchHelper.Log("[Launcher] Runtime slot inspect phase: patch compatibility");
        var patchCompatibility = PatchCompatibilityEvidence.Inspect(gameIdentity, runtimePack);
        PatchHelper.Log($"[Launcher] Runtime slot inspect phase complete: patch compatibility -> {patchCompatibility?.Status ?? "<none>"}");
        return BuildRuntimeSlot(context, metadata, runtimePack, patchCompatibility, gameIdentity, string.Empty, activeAndroidAssemblySha256);
    }

    private static string Sha256OrMissing(string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return "<missing>";
            var hashText = GameIdentityFileHasher.Instance.Sha256(path);
            return hashText;
        }
        catch (Exception ex)
        {
            return $"<unavailable:{ex.GetType().Name}>";
        }
    }

    private static bool HasUsableHash(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length != 64)
            return false;
        foreach (var c in value)
        {
            if (!Uri.IsHexDigit(c))
                return false;
        }

        return true;
    }

    private static GameRuntimeSlot BuildIncompleteRuntimeSlot(GameRuntimeSlotInspectionContext context, string gameIdentityProblem)
    {
        var metadata = RuntimeSlotMetadata.Inspect(context.ReleaseInfoPath, context.BranchMarkerPath);
        var runtimePack = RuntimePackManifest.NotInstalled(context.RuntimePackManifestPath, context.Branch);
        var patchCompatibility = PatchCompatibilityEvidence.Missing(context.Branch, context.RuntimePackManifestPath, "runtime pack compatibility evidence");
        return BuildRuntimeSlot(context, metadata, runtimePack, patchCompatibility, null, gameIdentityProblem, "<missing>");
    }

    private static GameRuntimeSlot BuildRuntimeSlot(GameRuntimeSlotInspectionContext context, RuntimeSlotMetadata metadata, RuntimePackManifest runtimePack, PatchCompatibilityEvidence patchCompatibility, GameIdentity gameIdentity, string gameIdentityProblem, string activeAndroidAssemblySha256) => new GameRuntimeSlot(context.Branch, context.DisplayName, context.SlotKind, context.SlotDirectory, context.GameDirectory, context.PckPath, context.ReleaseInfoPath, context.SourceAssemblyPath, context.ActiveAndroidAssemblyPath, context.RuntimePackManifestPath, metadata, gameIdentity, gameIdentityProblem, runtimePack, patchCompatibility, activeAndroidAssemblySha256, File.Exists(context.SourceAssemblyPath), File.Exists(context.ActiveAndroidAssemblyPath), File.Exists(context.RuntimePackManifestPath));
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

    internal static GameRuntimeSlot RefreshDerivedEvidence(string dataDir, GameRuntimeSlot current)
    {
        if (current?.GameIdentity == null)
            return current;
        var context = new GameRuntimeSlotInspectionContext(dataDir, current.Branch);
        var metadata = RuntimeSlotMetadata.Inspect(context.ReleaseInfoPath, context.BranchMarkerPath);
        var runtimePack = RuntimePackManifest.Inspect(context.RuntimePackManifestPath, current.GameIdentity);
        var patchCompatibility = PatchCompatibilityEvidence.Inspect(current.GameIdentity, runtimePack);
        return BuildRuntimeSlot(context, metadata, runtimePack, patchCompatibility, current.GameIdentity, current.GameIdentityProblem, Sha256OrMissing(context.ActiveAndroidAssemblyPath));
    }
}
