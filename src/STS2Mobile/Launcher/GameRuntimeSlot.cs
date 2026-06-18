using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using STS2Mobile.Patches;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal sealed class GameRuntimeSlot
{
    private const string GameAssemblyFileName = "sts2.dll";
    private const string RuntimePacksDirectory = "runtime_packs";
    private const string CompatibilityManifestFileName = "compatibility.json";

    private GameRuntimeSlot(
        string branch,
        string displayName,
        string slotKind,
        string slotDirectory,
        string gameDirectory,
        string pckPath,
        string releaseInfoPath,
        string sourceAssemblyPath,
        string activeAndroidAssemblyPath,
        string runtimePackManifestPath,
        RuntimeSlotMetadata metadata,
        RuntimePackManifest runtimePack,
        PatchCompatibilityEvidence patchCompatibility,
        bool runtimePackSlotIdMatches,
        string runtimeSlotId,
        string runtimeSlotIdentity,
        string pckSha256,
        string sourceAssemblySha256,
        string activeAndroidAssemblySha256,
        bool sourceAssemblyExists,
        bool activeAndroidAssemblyExists,
        bool runtimePackManifestExists
    )
    {
        Branch = branch;
        DisplayName = displayName;
        SlotKind = slotKind;
        SlotDirectory = slotDirectory;
        GameDirectory = gameDirectory;
        PckPath = pckPath;
        ReleaseInfoPath = releaseInfoPath;
        SourceAssemblyPath = sourceAssemblyPath;
        ActiveAndroidAssemblyPath = activeAndroidAssemblyPath;
        RuntimePackManifestPath = runtimePackManifestPath;
        Metadata = metadata;
        RuntimePack = runtimePack;
        PatchCompatibility = patchCompatibility;
        RuntimePackSlotIdMatches = runtimePackSlotIdMatches;
        RuntimeSlotId = runtimeSlotId;
        RuntimeSlotIdentity = runtimeSlotIdentity;
        PckSha256 = pckSha256;
        SourceAssemblySha256 = sourceAssemblySha256;
        ActiveAndroidAssemblySha256 = activeAndroidAssemblySha256;
        SourceAssemblyExists = sourceAssemblyExists;
        ActiveAndroidAssemblyExists = activeAndroidAssemblyExists;
        RuntimePackManifestExists = runtimePackManifestExists;
    }

    internal string Branch { get; }
    internal string DisplayName { get; }
    internal string SlotKind { get; }
    internal string SlotDirectory { get; }
    internal string GameDirectory { get; }
    internal string PckPath { get; }
    internal string ReleaseInfoPath { get; }
    internal string SourceAssemblyPath { get; }
    internal string ActiveAndroidAssemblyPath { get; }
    internal string RuntimePackManifestPath { get; }
    internal RuntimeSlotMetadata Metadata { get; }
    internal RuntimePackManifest RuntimePack { get; }
    internal PatchCompatibilityEvidence PatchCompatibility { get; }
    internal bool RuntimePackSlotIdMatches { get; }
    internal string RuntimeSlotId { get; }
    internal string RuntimeSlotIdentity { get; }
    internal string PckSha256 { get; }
    internal string SourceAssemblySha256 { get; }
    internal string ActiveAndroidAssemblySha256 { get; }
    internal bool SourceAssemblyExists { get; }
    internal bool ActiveAndroidAssemblyExists { get; }
    internal bool RuntimePackManifestExists { get; }
    internal bool RuntimePackUsable => RuntimePack?.Usable == true && RuntimePackSlotIdMatches;

    internal string RuntimePackUsabilityStatus
    {
        get
        {
            if (RuntimePack == null)
                return "not inspected";
            if (!RuntimePack.Usable)
                return RuntimePack.Status;
            if (string.IsNullOrWhiteSpace(RuntimePack.SourceRuntimeSlotId))
                return "missing source runtime slot ID";
            if (!RuntimePackSlotIdMatches)
                return "runtime slot ID mismatch";
            return "usable";
        }
    }

    internal bool SourceMatchesActiveAndroidAssembly =>
        !string.IsNullOrWhiteSpace(SourceAssemblySha256)
        && !string.IsNullOrWhiteSpace(ActiveAndroidAssemblySha256)
        && string.Equals(SourceAssemblySha256, ActiveAndroidAssemblySha256, StringComparison.OrdinalIgnoreCase);

    internal bool BranchMatchedAndroidRuntimePrepared =>
        SourceAssemblyExists
        && ActiveAndroidAssemblyExists
        && SourceMatchesActiveAndroidAssembly;

    internal bool BranchRuntimeAvailable =>
        RuntimePackUsable
        || (!RequiresRuntimePackOrPreparedCache && BranchMatchedAndroidRuntimePrepared);

    internal bool UsesLegacyPackagedPublicRuntime =>
        string.Equals(Branch, SteamGameBranch.Public, StringComparison.OrdinalIgnoreCase)
        && (ActiveAndroidAssemblyExists || SourceAssemblyExists);

    internal bool RequiresRuntimePackOrPreparedCache =>
        !string.Equals(Branch, SteamGameBranch.Public, StringComparison.OrdinalIgnoreCase);

    internal bool RuntimeCompatible =>
        BranchRuntimeAvailable
        || UsesLegacyPackagedPublicRuntime;

    internal bool PatchCompatible => PatchCompatibility?.Passed == true;

    internal bool Playable => RuntimeCompatible && PatchCompatible;

    internal string RuntimePairingStatus
    {
        get
        {
            if (!ActiveAndroidAssemblyExists)
            {
                if (UsesLegacyPackagedPublicRuntime)
                    return "legacy public runtime available; Android cache will be prepared at launch";
                if (RuntimePackUsable)
                    return "runtime pack available; Android cache will be prepared at launch";
                return "missing Android runtime assembly";
            }
            if (SourceMatchesActiveAndroidAssembly && !RequiresRuntimePackOrPreparedCache)
                return "branch-matched runtime";
            if (RuntimePackUsable)
                return "runtime pack available; Android cache will be prepared at launch";
            if (UsesLegacyPackagedPublicRuntime)
                return "legacy packaged public runtime";
            if (!SourceAssemblyExists)
                return RuntimePackManifestExists
                    ? $"runtime pack not usable: {RuntimePackUsabilityStatus}"
                    : "missing selected-branch source assembly";
            if (SourceAssemblyExists)
                return RuntimePackManifestExists
                    ? $"runtime pack not usable: {RuntimePackUsabilityStatus}"
                    : RequiresRuntimePackOrPreparedCache
                        ? "selected branch source assembly is present, but non-public versions require a usable runtime pack"
                        : "selected branch source assembly is present, but no usable Android runtime pack or prepared branch-matched cache exists";
            return "runtime pack required";
        }
    }

    internal string ReadinessProblem()
    {
        if (!RuntimeCompatible)
            return RuntimeReadinessProblem();

        var patchProblem = PatchCompatibility?.Problem;
        if (!string.IsNullOrWhiteSpace(patchProblem))
            return patchProblem;

        return null;
    }

    private string RuntimeReadinessProblem()
    {
        if (!HasUsableHash(PckSha256))
            return "Selected game version is not downloaded or the downloaded PCK is invalid. Download selected version to continue.";

        if (!SourceAssemblyExists)
        {
            if (RuntimePackManifestExists && !RuntimePackUsable)
                return $"Selected game version is missing its source game-code assembly and its runtime pack is not usable ({RuntimePackUsabilityStatus}). Redownload selected version or install a matching runtime pack.";

            return "Selected game version is missing its source game-code assembly. Redownload selected version.";
        }

        if (RequiresRuntimePackOrPreparedCache && !RuntimePackUsable)
            return RuntimePackManifestExists
                ? $"Selected game version requires a usable runtime pack, but its runtime pack is not usable ({RuntimePackUsabilityStatus}). Redownload selected version."
                : "Selected game version requires a usable runtime pack. Redownload selected version to regenerate runtime-pack evidence.";

        if (!ActiveAndroidAssemblyExists && !RuntimePackUsable)
            return "Android game-code runtime cache is missing and no usable runtime pack exists. Redownload selected version to rebuild runtime evidence.";

        return "Selected game version is downloaded, but its Android game-code runtime does not match the selected Steam branch. Install a matching runtime pack or select a compatible version.";
    }

    internal static GameRuntimeSlot Inspect(string dataDir, string branch)
    {
        branch = SteamGameBranch.Normalize(branch);
        PatchHelper.Log($"[Launcher] Runtime slot inspect phase: paths for branch '{branch}'");
        var gameDirectory = SteamGameInstallPaths.GameDirectory(dataDir, branch);
        var pckPath = Path.Combine(gameDirectory, LauncherStorageNames.GamePck);
        var sourceAssemblyPath = FindSourceAssemblyPath(gameDirectory);
        var activeAssemblyPath = FindActiveAndroidAssemblyPath(dataDir);
        var releaseInfoPath = Path.Combine(gameDirectory, "release_info.json");
        var runtimePackManifestPath = BuildRuntimePackManifestPath(dataDir, branch);
        PatchHelper.Log($"[Launcher] Runtime slot inspect phase complete: paths pck='{pckPath}' source='{sourceAssemblyPath}' active='{activeAssemblyPath}' manifest='{runtimePackManifestPath}'");
        PatchHelper.Log("[Launcher] Runtime slot inspect phase: PCK SHA256");
        var pckSha256 = PckSha256OrMissing(dataDir, branch, gameDirectory, pckPath, runtimePackManifestPath);
        PatchHelper.Log($"[Launcher] Runtime slot inspect phase complete: PCK SHA256 -> {pckSha256}");
        if (!HasUsableHash(pckSha256))
        {
            PatchHelper.Log("[Launcher] Runtime slot inspect phase: selected PCK missing or invalid; skipping source/runtime file probes");
            var incompleteMetadata = RuntimeSlotMetadata.Inspect(
                releaseInfoPath,
                SteamGameInstallPaths.BranchMarkerPath(dataDir, branch)
            );
            var incompleteRuntimePack = RuntimePackManifest.NotInstalled(runtimePackManifestPath, branch);
            var incompletePatchCompatibility = PatchCompatibilityEvidence.Missing(
                branch,
                Path.Combine(gameDirectory, PatchCompatibilityEvidence.GameDirectoryMarkerFileName),
                "selected game directory validation marker"
            );
            var incompleteRuntimeSlotIdentity = BuildRuntimeSlotIdentity(branch, incompleteMetadata, incompleteRuntimePack, false, incompletePatchCompatibility, pckSha256, "<missing>");
            var incompleteRuntimeSlotId = BuildRuntimeSlotId(branch, incompleteRuntimeSlotIdentity);
            PatchHelper.Log($"[Launcher] Runtime slot inspect phase complete: incomplete files -> {incompleteRuntimeSlotId}");
            return new GameRuntimeSlot(
                branch,
                SteamGameBranch.DisplayName(branch),
                SteamGameInstallPaths.VersionSlotKind(branch),
                SteamGameInstallPaths.VersionSlotDirectory(dataDir, branch),
                gameDirectory,
                pckPath,
                releaseInfoPath,
                sourceAssemblyPath,
                activeAssemblyPath,
                runtimePackManifestPath,
                incompleteMetadata,
                incompleteRuntimePack,
                incompletePatchCompatibility,
                false,
                incompleteRuntimeSlotId,
                incompleteRuntimeSlotIdentity,
                pckSha256,
                "<missing>",
                "<missing>",
                false,
                false,
                false
            );
        }
        PatchHelper.Log("[Launcher] Runtime slot inspect phase: source assembly SHA256");
        var sourceAssemblySha256 = SourceAssemblySha256OrMissing(dataDir, branch, sourceAssemblyPath);
        PatchHelper.Log($"[Launcher] Runtime slot inspect phase complete: source assembly SHA256 -> {sourceAssemblySha256}");
        PatchHelper.Log("[Launcher] Runtime slot inspect phase: active Android assembly SHA256");
        var activeAndroidAssemblySha256 = ActiveAndroidAssemblySha256OrMissing(dataDir, branch, activeAssemblyPath);
        PatchHelper.Log($"[Launcher] Runtime slot inspect phase complete: active Android assembly SHA256 -> {activeAndroidAssemblySha256}");
        PatchHelper.Log("[Launcher] Runtime slot inspect phase: metadata");
        var metadata = RuntimeSlotMetadata.Inspect(
            releaseInfoPath,
            SteamGameInstallPaths.BranchMarkerPath(dataDir, branch)
        );
        PatchHelper.Log("[Launcher] Runtime slot inspect phase complete: metadata");
        PatchHelper.Log("[Launcher] Runtime slot inspect phase: runtime pack manifest");
        var runtimePack = RuntimePackManifest.Inspect(
            runtimePackManifestPath,
            branch,
            pckSha256,
            sourceAssemblySha256,
            pckPath
        );
        PatchHelper.Log($"[Launcher] Runtime slot inspect phase complete: runtime pack manifest -> {runtimePack?.Status ?? "<none>"}");
        if (runtimePack?.Usable == true
            && HasUsableHash(runtimePack.SourcePckSha256)
            && !string.Equals(runtimePack.SourcePckSha256, pckSha256, StringComparison.OrdinalIgnoreCase))
        {
            PatchHelper.Log($"[Launcher] Runtime slot inspect phase: canonicalizing Android-patched PCK hash {pckSha256} to runtime source PCK hash {runtimePack.SourcePckSha256}");
            pckSha256 = runtimePack.SourcePckSha256.ToLowerInvariant();
        }
        PatchHelper.Log("[Launcher] Runtime slot inspect phase: runtime pack slot ID");
        var runtimePackSlotIdMatches = RuntimePackSlotIdMatchesFor(metadata, runtimePack, branch, pckSha256, sourceAssemblySha256);
        PatchHelper.Log($"[Launcher] Runtime slot inspect phase complete: runtime pack slot ID -> {runtimePackSlotIdMatches}");
        PatchHelper.Log("[Launcher] Runtime slot inspect phase: patch compatibility");
        var patchCompatibility = PatchCompatibilityEvidence.Inspect(
            dataDir,
            branch,
            gameDirectory,
            pckSha256,
            sourceAssemblySha256,
            runtimePack,
            runtimePackSlotIdMatches
        );
        PatchHelper.Log($"[Launcher] Runtime slot inspect phase complete: patch compatibility -> {patchCompatibility?.Status ?? "<none>"}");
        PatchHelper.Log("[Launcher] Runtime slot inspect phase: runtime slot identity");
        var runtimeSlotIdentity = BuildRuntimeSlotIdentity(branch, metadata, runtimePack, runtimePackSlotIdMatches, patchCompatibility, pckSha256, sourceAssemblySha256);
        var runtimeSlotId = BuildRuntimeSlotId(branch, runtimeSlotIdentity);
        PatchHelper.Log($"[Launcher] Runtime slot inspect phase complete: runtime slot identity -> {runtimeSlotId}");

        return new GameRuntimeSlot(
            branch,
            SteamGameBranch.DisplayName(branch),
            SteamGameInstallPaths.VersionSlotKind(branch),
            SteamGameInstallPaths.VersionSlotDirectory(dataDir, branch),
            gameDirectory,
            pckPath,
            releaseInfoPath,
            sourceAssemblyPath,
            activeAssemblyPath,
            runtimePackManifestPath,
            metadata,
            runtimePack,
            patchCompatibility,
            runtimePackSlotIdMatches,
            runtimeSlotId,
            runtimeSlotIdentity,
            pckSha256,
            sourceAssemblySha256,
            activeAndroidAssemblySha256,
            File.Exists(sourceAssemblyPath),
            File.Exists(activeAssemblyPath),
            File.Exists(runtimePackManifestPath)
        );
    }

    private static string BuildRuntimeSlotIdentity(
        string branch,
        RuntimeSlotMetadata metadata,
        RuntimePackManifest runtimePack,
        bool runtimePackSlotIdMatches,
        PatchCompatibilityEvidence patchCompatibility,
        string pckSha256,
        string sourceAssemblySha256
    )
    {
        var runtimePackUsable = runtimePack?.Usable == true && runtimePackSlotIdMatches;
        var requiresRuntimePack = !string.Equals(branch, SteamGameBranch.Public, StringComparison.OrdinalIgnoreCase);
        var runtimeSource = runtimePackUsable
            ? "runtime-pack"
            : requiresRuntimePack
                ? "no-usable-runtime"
                : "selected-game";
        var androidAssemblySha256 = runtimePackUsable
            ? runtimePack.ActualAndroidAssemblySha256
            : sourceAssemblySha256;
        return string.Join(
            "\n",
            $"branch={SteamGameBranch.Normalize(branch)}",
            $"stateDirectory={SteamGameBranch.StateDirectoryName(branch)}",
            $"releaseVersion={metadata?.ReleaseVersion ?? string.Empty}",
            $"releaseCommit={metadata?.ReleaseCommit ?? string.Empty}",
            $"releaseBuildId={metadata?.ReleaseBuildId ?? string.Empty}",
            $"depotManifestFingerprint={metadata?.DepotManifestFingerprint ?? string.Empty}",
            $"pckSha256={pckSha256}",
            $"sourceAssemblySha256={sourceAssemblySha256}",
            $"runtimeSource={runtimeSource}",
            $"runtimePackId={runtimePack?.PackId ?? string.Empty}",
            $"androidAssemblySha256={androidAssemblySha256}",
            $"patchSetVersion={patchCompatibility?.PatchSetVersion ?? runtimePack?.PatchSetVersion ?? string.Empty}",
            $"patchValidationStatus={patchCompatibility?.Status ?? runtimePack?.PatchValidationStatus ?? string.Empty}"
        );
    }

    private static bool RuntimePackSlotIdMatchesFor(
        RuntimeSlotMetadata metadata,
        RuntimePackManifest runtimePack,
        string branch,
        string pckSha256,
        string sourceAssemblySha256
    )
    {
        if (runtimePack == null || !runtimePack.Exists || !runtimePack.Readable)
            return false;
        if (string.IsNullOrWhiteSpace(runtimePack.SourceRuntimeSlotId))
            return false;
        if (string.IsNullOrWhiteSpace(runtimePack.PackId))
            return false;

        var expectedIdentity = string.Join(
            "\n",
            $"branch={SteamGameBranch.Normalize(branch)}",
            $"stateDirectory={SteamGameBranch.StateDirectoryName(branch)}",
            $"releaseVersion={metadata?.ReleaseVersion ?? string.Empty}",
            $"releaseCommit={metadata?.ReleaseCommit ?? string.Empty}",
            $"releaseBuildId={metadata?.ReleaseBuildId ?? string.Empty}",
            $"depotManifestFingerprint={metadata?.DepotManifestFingerprint ?? string.Empty}",
            $"pckSha256={pckSha256}",
            $"sourceAssemblySha256={sourceAssemblySha256}",
            "runtimeSource=runtime-pack",
            $"runtimePackId={runtimePack.PackId}",
            $"androidAssemblySha256={runtimePack.ActualAndroidAssemblySha256}",
            $"patchSetVersion={runtimePack.PatchSetVersion}",
            $"patchValidationStatus={runtimePack.PatchValidationStatus}"
        );
        var expectedId = BuildRuntimeSlotId(branch, expectedIdentity);
        return string.Equals(runtimePack.SourceRuntimeSlotId, expectedId, StringComparison.OrdinalIgnoreCase);
    }

    internal static string BuildRuntimePackSlotIdentity(GameRuntimeSlot slot, string patchSetVersion, string runtimePackId)
    {
        return string.Join(
            "\n",
            $"branch={SteamGameBranch.Normalize(slot.Branch)}",
            $"stateDirectory={SteamGameBranch.StateDirectoryName(slot.Branch)}",
            $"releaseVersion={slot.Metadata?.ReleaseVersion ?? string.Empty}",
            $"releaseCommit={slot.Metadata?.ReleaseCommit ?? string.Empty}",
            $"releaseBuildId={slot.Metadata?.ReleaseBuildId ?? string.Empty}",
            $"depotManifestFingerprint={slot.Metadata?.DepotManifestFingerprint ?? string.Empty}",
            $"pckSha256={slot.PckSha256}",
            $"sourceAssemblySha256={slot.SourceAssemblySha256}",
            "runtimeSource=runtime-pack",
            $"runtimePackId={runtimePackId}",
            $"androidAssemblySha256={slot.SourceAssemblySha256}",
            $"patchSetVersion={patchSetVersion}",
            "patchValidationStatus=passed"
        );
    }

    internal static string BuildRuntimePackSlotId(GameRuntimeSlot slot, string patchSetVersion, string runtimePackId)
        => BuildRuntimeSlotId(
            slot.Branch,
            BuildRuntimePackSlotIdentity(slot, patchSetVersion, runtimePackId)
        );

    private static string BuildRuntimeSlotId(string branch, string runtimeSlotIdentity)
        => $"{SteamGameBranch.StateDirectoryName(branch)}-{StableHash16(runtimeSlotIdentity)}";

    private static string StableHash16(string value)
    {
        unchecked
        {
            const ulong offset = 14695981039346656037UL;
            const ulong prime = 1099511628211UL;
            var hash = offset;
            foreach (var b in Encoding.UTF8.GetBytes(value ?? string.Empty))
            {
                hash ^= b;
                hash *= prime;
            }

            return hash.ToString("x16");
        }
    }

    private static string BuildRuntimePackManifestPath(string dataDir, string branch)
        => Path.Combine(
            RuntimePackDirectoryPath(dataDir, branch),
            CompatibilityManifestFileName
        );

    internal static string RuntimePackDirectoryPath(string dataDir, string branch)
        => Path.Combine(
            dataDir,
            RuntimePacksDirectory,
            SteamGameBranch.StateDirectoryName(branch)
        );

    private static string FindSourceAssemblyPath(string gameDirectory)
    {
        if (string.IsNullOrWhiteSpace(gameDirectory) || !Directory.Exists(gameDirectory))
            return Path.Combine(gameDirectory ?? string.Empty, "data_sts2_windows_x86_64", GameAssemblyFileName);

        foreach (var directory in Directory.EnumerateDirectories(gameDirectory, "data_*", SearchOption.TopDirectoryOnly))
        {
            var candidate = Path.Combine(directory, GameAssemblyFileName);
            if (File.Exists(candidate))
                return candidate;
        }

        return Path.Combine(gameDirectory, "data_sts2_windows_x86_64", GameAssemblyFileName);
    }

    private static string FindActiveAndroidAssemblyPath(string dataDir)
    {
        var publishRoot = Path.Combine(dataDir, ".godot", "mono", "publish");
        if (Directory.Exists(publishRoot))
        {
            foreach (var directory in Directory.EnumerateDirectories(publishRoot, "*", SearchOption.TopDirectoryOnly))
            {
                var candidate = Path.Combine(directory, GameAssemblyFileName);
                if (File.Exists(candidate))
                    return candidate;
            }
        }

        return Path.Combine(publishRoot, "arm64", GameAssemblyFileName);
    }

    private static string Sha256OrMissing(string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return "<missing>";

            byte[] hash;
            if (OperatingSystem.IsAndroid())
            {
                hash = AndroidJavaCrypto.Sha256FileHashData(path);
            }
            else
            {
                using var stream = File.OpenRead(path);
                hash = SHA256.HashData(stream);
            }
            return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
        }
        catch (Exception ex)
        {
            return $"<unavailable:{ex.GetType().Name}>";
        }
    }

    private static string PckSha256OrMissing(string dataDir, string branch, string gameDirectory, string pckPath, string runtimePackManifestPath)
    {
        var cached = CachedSelectedPckSha256(dataDir, branch, pckPath);
        if (HasUsableHash(cached))
            return cached;

        if (!string.Equals(branch, SteamGameBranch.Public, StringComparison.OrdinalIgnoreCase))
        {
            var validated = ValidatedGameDirectoryPckSha256(gameDirectory, branch);
            if (HasUsableHash(validated))
                return validated;

            var runtimePack = RuntimePackSourcePckSha256(runtimePackManifestPath, branch);
            return HasUsableHash(runtimePack)
                ? runtimePack
                : "<missing>";
        }

        return Sha256OrMissing(pckPath);
    }

    private static string SourceAssemblySha256OrMissing(string dataDir, string branch, string sourceAssemblyPath)
    {
        var cached = CachedSelectedSourceAssemblySha256(dataDir, branch, sourceAssemblyPath);
        if (HasUsableHash(cached))
            return cached;

        if (!string.Equals(branch, SteamGameBranch.Public, StringComparison.OrdinalIgnoreCase))
        {
            var gameDirectory = Directory.GetParent(Path.GetDirectoryName(sourceAssemblyPath) ?? string.Empty)?.FullName;
            var validated = ValidatedGameDirectorySourceAssemblySha256(gameDirectory, branch);
            if (HasUsableHash(validated))
                return validated;

            var runtimePackManifestPath = BuildRuntimePackManifestPath(dataDir, branch);
            var runtimePack = RuntimePackSourceAssemblySha256(runtimePackManifestPath, branch);
            if (HasUsableHash(runtimePack))
                return runtimePack;
        }

        return Sha256OrMissing(sourceAssemblyPath);
    }

    private static string ActiveAndroidAssemblySha256OrMissing(string dataDir, string branch, string activeAssemblyPath)
    {
        var cached = CachedActiveAndroidAssemblySha256(dataDir, branch, activeAssemblyPath);
        return HasUsableHash(cached)
            ? cached
            : Sha256OrMissing(activeAssemblyPath);
    }

    private static string CachedSelectedPckSha256(string dataDir, string branch, string pckPath)
    {
        try
        {
            var cachedBranch = LauncherRuntimeCacheEvidence.SelectedBranch(dataDir);
            if (HasMarkerValue(cachedBranch)
                && !string.Equals(
                    SteamGameBranch.Normalize(cachedBranch),
                    SteamGameBranch.Normalize(branch),
                    StringComparison.OrdinalIgnoreCase
                ))
            {
                return null;
            }

            var cachedPath = LauncherRuntimeCacheEvidence.SelectedPckPath(dataDir);
            if (!PathsEquivalent(cachedPath, pckPath, dataDir))
                return null;

            var cachedRuntimeSource = LauncherRuntimeCacheEvidence.RuntimeSource(dataDir);
            if (HasMarkerValue(cachedRuntimeSource)
                && string.Equals(cachedRuntimeSource, "no-usable-runtime", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var cachedHash = LauncherRuntimeCacheEvidence.SelectedPckSha256(dataDir);
            return HasUsableHash(cachedHash)
                ? cachedHash.ToLowerInvariant()
                : null;
        }
        catch
        {
            return null;
        }
    }

    private static string ValidatedGameDirectoryPckSha256(string gameDirectory, string branch)
        => ValidatedGameDirectoryHash(
            gameDirectory,
            branch,
            "pckSha256",
            "pck_sha256",
            "sourcePckSha256",
            "source_pck_sha256"
        );

    private static string ValidatedGameDirectorySourceAssemblySha256(string gameDirectory, string branch)
        => ValidatedGameDirectoryHash(
            gameDirectory,
            branch,
            "sourceAssemblySha256",
            "source_assembly_sha256",
            "desktopAssemblySha256",
            "desktop_assembly_sha256"
        );

    private static string ValidatedGameDirectoryHash(string gameDirectory, string branch, params string[] hashProperties)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(gameDirectory))
                return null;

            var markerPath = Path.Combine(gameDirectory, PatchCompatibilityEvidence.GameDirectoryMarkerFileName);
            if (!File.Exists(markerPath))
                return null;

            using var document = JsonDocument.Parse(File.ReadAllText(markerPath));
            var root = document.RootElement;
            var status = ReadJsonString(root, "status", "result", "patchValidationStatus", "patch_validation_status");
            if (!string.Equals(status, "passed", StringComparison.OrdinalIgnoreCase))
                return null;

            var validatedBranch = ReadJsonString(root, "branch", "sourceBranch", "source_branch", "validatedBranch", "validated_branch");
            if (!string.Equals(SteamGameBranch.Normalize(validatedBranch), SteamGameBranch.Normalize(branch), StringComparison.OrdinalIgnoreCase))
                return null;

            var hash = ReadJsonString(root, hashProperties);
            return HasUsableHash(hash)
                ? hash.ToLowerInvariant()
                : null;
        }
        catch
        {
            return null;
        }
    }

    private static string RuntimePackSourcePckSha256(string runtimePackManifestPath, string branch)
        => RuntimePackSourceHash(
            runtimePackManifestPath,
            branch,
            "sourcePckSha256",
            "source_pck_sha256",
            "pckSha256",
            "pck_sha256"
        );

    private static string RuntimePackSourceAssemblySha256(string runtimePackManifestPath, string branch)
        => RuntimePackSourceHash(
            runtimePackManifestPath,
            branch,
            "sourceAssemblySha256",
            "source_assembly_sha256",
            "desktopAssemblySha256",
            "desktop_assembly_sha256"
        );

    private static string RuntimePackSourceHash(string runtimePackManifestPath, string branch, params string[] hashProperties)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(runtimePackManifestPath) || !File.Exists(runtimePackManifestPath))
                return null;

            using var document = JsonDocument.Parse(File.ReadAllText(runtimePackManifestPath));
            var root = document.RootElement;
            var sourceBranch = ReadJsonString(root, "sourceBranch", "source_branch", "branch");
            if (!string.Equals(SteamGameBranch.Normalize(sourceBranch), SteamGameBranch.Normalize(branch), StringComparison.OrdinalIgnoreCase))
                return null;

            var patchStatus = ReadJsonString(root, "patchValidationStatus", "patch_validation_status", "patchStatus", "patch_status");
            if (!string.Equals(patchStatus, "passed", StringComparison.OrdinalIgnoreCase))
                return null;

            var hash = ReadJsonString(root, hashProperties);
            return HasUsableHash(hash)
                ? hash.ToLowerInvariant()
                : null;
        }
        catch
        {
            return null;
        }
    }

    private static string ReadJsonString(JsonElement root, params string[] properties)
    {
        foreach (var property in properties)
        {
            if (root.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String)
                return value.GetString() ?? string.Empty;
        }

        return string.Empty;
    }

    private static string CachedSelectedSourceAssemblySha256(string dataDir, string branch, string sourceAssemblyPath)
    {
        try
        {
            var cachedBranch = LauncherRuntimeCacheEvidence.SelectedBranch(dataDir);
            if (HasMarkerValue(cachedBranch)
                && !string.Equals(
                    SteamGameBranch.Normalize(cachedBranch),
                    SteamGameBranch.Normalize(branch),
                    StringComparison.OrdinalIgnoreCase
                ))
            {
                return null;
            }

            var cachedPath = LauncherRuntimeCacheEvidence.SelectedSourceAssembly(dataDir);
            if (!PathsEquivalent(cachedPath, sourceAssemblyPath, dataDir))
                return null;

            var cachedHash = LauncherRuntimeCacheEvidence.SelectedSourceAssemblySha256(dataDir);
            return HasUsableHash(cachedHash)
                ? cachedHash.ToLowerInvariant()
                : null;
        }
        catch
        {
            return null;
        }
    }

    private static string CachedActiveAndroidAssemblySha256(string dataDir, string branch, string activeAssemblyPath)
    {
        try
        {
            var cachedBranch = LauncherRuntimeCacheEvidence.SelectedBranch(dataDir);
            if (HasMarkerValue(cachedBranch)
                && !string.Equals(
                    SteamGameBranch.Normalize(cachedBranch),
                    SteamGameBranch.Normalize(branch),
                    StringComparison.OrdinalIgnoreCase
                ))
            {
                return null;
            }

            var publishDirectory = LauncherRuntimeCacheEvidence.PublishCacheDirectory(dataDir);
            if (!HasMarkerValue(publishDirectory) || string.IsNullOrWhiteSpace(activeAssemblyPath))
                return null;

            var cachedPath = Path.Combine(publishDirectory, GameAssemblyFileName);
            if (!PathsEquivalent(cachedPath, activeAssemblyPath, dataDir))
                return null;

            var cachedHash = LauncherRuntimeCacheEvidence.PublishCacheActiveAssemblySha256(dataDir);
            return HasUsableHash(cachedHash)
                ? cachedHash.ToLowerInvariant()
                : null;
        }
        catch
        {
            return null;
        }
    }

    private static bool PathsEquivalent(string left, string right, string dataDir)
    {
        left = NormalizePath(left);
        right = NormalizePath(right);
        if (string.Equals(left, right, StringComparison.OrdinalIgnoreCase))
            return true;

        var leftAlias = AndroidAppPrivatePathAlias(left, dataDir);
        return !string.IsNullOrWhiteSpace(leftAlias)
            && string.Equals(leftAlias, right, StringComparison.OrdinalIgnoreCase);
    }

    private static string AndroidAppPrivatePathAlias(string path, string dataDir)
    {
        var normalizedDataDir = NormalizePath(dataDir);
        var packageName = Path.GetFileName(normalizedDataDir);
        if (string.IsNullOrWhiteSpace(packageName))
            return null;

        var dataDataPrefix = $"/data/data/{packageName}";
        var dataUserPrefix = $"/data/user/0/{packageName}";
        if (path.StartsWith(dataDataPrefix, StringComparison.OrdinalIgnoreCase))
            return dataUserPrefix + path.Substring(dataDataPrefix.Length);
        if (path.StartsWith(dataUserPrefix, StringComparison.OrdinalIgnoreCase))
            return dataDataPrefix + path.Substring(dataUserPrefix.Length);

        return null;
    }

    private static string NormalizePath(string path)
        => string.IsNullOrWhiteSpace(path)
            ? string.Empty
            : path.Trim().Replace('\\', '/').TrimEnd('/');

    private static bool HasMarkerValue(string value)
        => !string.IsNullOrWhiteSpace(value) && !value.StartsWith("<", StringComparison.Ordinal);

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
}
