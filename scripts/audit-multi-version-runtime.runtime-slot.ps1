function Add-MultiVersionRuntimeSlotChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\GameRuntimeSlot.cs" `
        "defines immutable branch runtime slot state" `
        @(
            "internal sealed partial class GameRuntimeSlot",
            "GameAssemblyFileName",
            "RuntimePacksDirectory",
            "CompatibilityManifestFileName",
            "RuntimePackManifest",
            "PatchCompatibilityEvidence",
            "RuntimeSlotMetadata",
            "PckSha256",
            "SourceAssemblySha256",
            "ActiveAndroidAssemblySha256",
            "RuntimeSlotId",
            "RuntimeSlotIdentity",
            "RuntimePackSlotIdMatches",
            "RuntimePackManifestPath"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\GameRuntimeSlot.Readiness.cs" `
        "keeps runtime-pack and patch playability gates explicit" `
        @(
            "RuntimePackUsabilityStatus",
            "RuntimePackUsable",
            "BranchMatchedAndroidRuntimePrepared",
            "BranchRuntimeAvailable",
            "non-public versions require a usable runtime pack",
            "UsesLegacyPackagedPublicRuntime",
            "RequiresRuntimePackOrPreparedCache",
            "RuntimeCompatible",
            "PatchCompatible",
            "Playable",
            "missing source runtime slot ID",
            "ReadinessProblem",
            "RuntimeReadinessProblem",
            "requires a usable runtime pack",
            "regenerate runtime-pack evidence"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\GameRuntimeSlot.Inspect.cs" `
        "orchestrates selected runtime slot inspection phases" `
        @(
            "GameRuntimeSlotInspectionContext",
            "PckSha256OrMissing",
            "SourceAssemblySha256OrMissing",
            "ActiveAndroidAssemblySha256OrMissing",
            "RuntimeSlotMetadata\.Inspect",
            "RuntimePackManifest\.Inspect",
            "PatchCompatibilityEvidence\.Inspect",
            "RuntimePackSlotIdMatchesFor",
            "BuildRuntimeSlotIdentity",
            "BuildIncompleteRuntimeSlot",
            "BuildRuntimeSlot",
            "CanonicalizeRuntimePackSourcePckSha256",
            "Runtime slot inspect phase: runtime pack manifest",
            "Runtime slot inspect phase: patch compatibility",
            "Runtime slot inspect phase: runtime slot identity"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\GameRuntimeSlot.Inspect.Context.cs" `
        "keeps selected runtime slot inspection paths typed and normalized" `
        @(
            "GameRuntimeSlotInspectionContext",
            "SteamGameBranch\.Normalize",
            "SteamGameInstallPaths\.GameDirectory",
            "SteamGameInstallPaths\.VersionSlotKind",
            "SteamGameInstallPaths\.VersionSlotDirectory",
            "SteamGameInstallPaths\.BranchMarkerPath",
            "Path\.Combine\(GameDirectory, LauncherStorageNames\.GamePck\)",
            "FindSourceAssemblyPath",
            "FindActiveAndroidAssemblyPath",
            "BuildRuntimePackManifestPath",
            "RuntimePackManifestPath"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\GameRuntimeSlot.Inspect.Build.cs" `
        "builds complete and incomplete runtime slot inspection results in one place" `
        @(
            "BuildIncompleteRuntimeSlot",
            "RuntimePackManifest\.NotInstalled",
            "PatchCompatibilityEvidence\.Missing",
            "BuildRuntimeSlotIdentity",
            "BuildRuntimeSlotId",
            "BuildRuntimeSlot",
            "new GameRuntimeSlot",
            "File\.Exists\(context\.SourceAssemblyPath\)",
            "File\.Exists\(context\.ActiveAndroidAssemblyPath\)",
            "File\.Exists\(context\.RuntimePackManifestPath\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\GameRuntimeSlot.Inspect.Canonicalize.cs" `
        "canonicalizes patched PCK hashes to the runtime-pack source hash when the pack is usable" `
        @(
            "CanonicalizeRuntimePackSourcePckSha256",
            "runtimePack\?\.Usable == true",
            "HasUsableHash\(runtimePack\.SourcePckSha256\)",
            "canonicalizing Android-patched PCK hash",
            "runtimePack\.SourcePckSha256\.ToLowerInvariant\(\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\GameRuntimeSlot.Readiness.cs" `
        "does not treat NativeFallback/runtime gating as launch success" `
        @(
            "RuntimePackUsabilityStatus",
            "BranchMatchedAndroidRuntimePrepared",
            "BranchRuntimeAvailable",
            "non-public versions require a usable runtime pack",
            "Selected game version requires a usable runtime pack",
            "Android game-code runtime cache is missing and no usable runtime pack exists"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\GameRuntimeSlot.Identity.cs" `
        "builds stable runtime slot identities and runtime pack slot IDs" `
        @(
            "BuildRuntimeSlotIdentity",
            "BuildRuntimePackSlotIdentity",
            "BuildRuntimePackSlotId",
            "BuildRuntimeSlotId",
            "StableHash16",
            "no-usable-runtime",
            "runtimeSource=runtime-pack",
            "patchValidationStatus=passed"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\GameRuntimeSlot.Paths.cs" `
        "resolves runtime slot source, active Android assembly, and runtime-pack paths" `
        @(
            "BuildRuntimePackManifestPath",
            "RuntimePackDirectoryPath",
            "FindSourceAssemblyPath",
            "FindActiveAndroidAssemblyPath",
            "data_sts2_windows_x86_64",
            "RuntimePacksDirectory"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\GameRuntimeSlot.Hashes.cs" `
        "selects runtime hashes from cache, validation markers, runtime packs, or files in priority order" `
        @(
            "PckSha256OrMissing",
            "SourceAssemblySha256OrMissing",
            "ActiveAndroidAssemblySha256OrMissing",
            "CachedSelectedPckSha256",
            "ValidatedGameDirectoryPckSha256",
            "RuntimePackSourcePckSha256",
            "Sha256OrMissing"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\GameRuntimeSlot.HashFiles.cs" `
        "hashes runtime files on Android through Java and validates SHA-256 shape" `
        @(
            "AndroidJavaCrypto\.Sha256FileHashData",
            "SHA256\.HashData",
            "Sha256OrMissing",
            "OperatingSystem\.IsAndroid",
            "HasUsableHash",
            "Uri\.IsHexDigit"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\GameRuntimeSlot.HashCache.cs" `
        "trusts cached runtime hashes only when branch, path, identity, and runtime source match" `
        @(
            "CachedSelectedPckSha256",
            "CachedSelectedSourceAssemblySha256",
            "CachedActiveAndroidAssemblySha256",
            "SelectedBranch",
            "SelectedPckPath",
            "SelectedPckIdentity",
            "RuntimeSource",
            "no-usable-runtime",
            "PublishCacheDirectory",
            "PathsEquivalent",
            "FileIdentityMatches"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\GameRuntimeSlot.HashValidation.cs" `
        "reads selected runtime hashes from passed patch validation and runtime-pack manifests" `
        @(
            "ValidatedGameDirectoryPckSha256",
            "ValidatedGameDirectorySourceAssemblySha256",
            "ValidatedGameDirectoryHash",
            "RuntimePackSourcePckSha256",
            "RuntimePackSourceAssemblySha256",
            "RuntimePackSourceHash",
            "PatchCompatibilityEvidence\.GameDirectoryMarkerFileName",
            "patchValidationStatus",
            "sourcePckSha256",
            "sourceAssemblySha256",
            "ReadJsonString",
            "HasUsableHash"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\GameRuntimeSlot.HashIdentity.cs" `
        "matches cached runtime hash paths across Android private path aliases and file identity markers" `
        @(
            "PathsEquivalent",
            "LauncherAndroidAppPrivatePath\.PathMatchesOrLeftAliasMatches",
            "FileIdentityMatches",
            "FileIdentity",
            "HasMarkerValue"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherAndroidAppPrivatePath.cs" `
        "centralizes Android private path normalization and alias matching" `
        @(
            "internal static class LauncherAndroidAppPrivatePath",
            "NormalizePath",
            "NormalizeMarkerPath",
            "MarkerPathMatchesExpectedPath",
            "PathMatchesOrLeftAliasMatches",
            "NormalizedMarkerPathsEqual",
            "AndroidAppPrivatePathAlias",
            "DataDataPrefix = ""/data/data/""",
            "DataUserPrefix = ""/data/user/0/""",
            "sourceRootPrefix",
            "aliasRootPrefix"
        )

    Add-Check `
        "src\STS2Mobile\Steam\AndroidJavaCrypto.Sha256.cs" `
        "hashes large Android files through Java SHA-256 instead of Mono file-stream SHA-256" `
        @(
            "Sha256FileHashData",
            "sha256FileBase64",
            "CallBase64Bridge",
            "file SHA-256"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\RuntimeSlotMetadata.cs" `
        "keeps runtime-slot metadata shape and identity summary immutable" `
        @(
            "internal sealed partial class RuntimeSlotMetadata",
            "ReleaseVersion",
            "ReleaseCommit",
            "ReleaseBuildId",
            "DepotManifestCount",
            "DepotManifestFingerprint",
            "IdentitySummary"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherBranchMarkerFields.cs" `
        "centralizes branch-marker depot provenance prefixes used in runtime-slot identity" `
        @(
            "internal static class LauncherBranchMarkerFields",
            "DepotManifestCount = ""Depot manifest count:""",
            "DepotsMatchingPublic = ""Depot manifests matching public count:""",
            "DepotsDifferingFromPublic = ""Depot manifests differing from public count:""",
            "DepotsInheritedFromPublic = ""Depot manifests inherited from public count:""",
            "DepotsMissingSelectedManifest = ""Depot manifests missing selected branch manifest count:""",
            "DepotManifestRow = ""Depot manifest:"""
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherBranchMarkerIntegrityProvenance.cs" `
        "reads typed branch-marker depot comparison evidence used by runtime-slot checks" `
        @(
            "readonly record struct LauncherBranchMarkerIntegrityProvenance",
            "MatchingPublic",
            "DifferingFromPublic",
            "WithoutPublicComparison",
            "InheritedFromPublic",
            "MissingSelectedManifest",
            "IsComplete",
            "LauncherMarkerFile\.ReadInt",
            "LauncherBranchMarkerFields\.DepotsMatchingPublic",
            "LauncherBranchMarkerFields\.DepotsDifferingFromPublic",
            "LauncherBranchMarkerFields\.DepotsWithoutPublicComparison",
            "LauncherBranchMarkerFields\.DepotsInheritedFromPublic",
            "LauncherBranchMarkerFields\.DepotsMissingSelectedManifest"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\RuntimeSlotMetadata.Inspect.cs" `
        "combines release-info and depot marker provenance into runtime-slot metadata" `
        @(
            "Inspect",
            "ReadReleaseInfo",
            "ReadMarkerValue",
            "LauncherBranchMarkerFields\.DepotManifestCount",
            "LauncherBranchMarkerFields\.DepotsDifferingFromPublic",
            "BuildDepotManifestFingerprint"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\RuntimeSlotMetadata.ReleaseInfo.cs" `
        "reads release-info JSON variants into runtime-slot identity" `
        @(
            "ReadReleaseInfo",
            "JsonDocument\.Parse",
            "ReadString",
            "releaseVersion",
            "release_version",
            "gitCommit",
            "steamBuildId"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\RuntimeSlotMetadata.BranchMarker.cs" `
        "reads branch-marker depot provenance and builds a sorted depot manifest fingerprint" `
        @(
            "ReadMarkerValue",
            "LauncherMarkerFile\.ReadValue",
            "missingFileValue: LauncherMarkerFile\.MissingLineValue",
            "File\.ReadLines",
            "StringComparison\.OrdinalIgnoreCase",
            "BuildDepotManifestFingerprint",
            "LauncherBranchMarkerFields\.DepotManifestRow",
            "OrderBy",
            "StableHash16"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\RuntimeSlotMetadata.Hash.cs" `
        "keeps runtime-slot metadata stable hash helper isolated" `
        @(
            "StableHash16",
            "14695981039346656037UL",
            "1099511628211UL",
            "Encoding\.UTF8\.GetBytes"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherGameFiles.Readiness.cs" `
        "keeps selected-file readiness lightweight while surfacing runtime playability through readiness problems" `
        @(
            "IsValidPck\(PckPath\(dataDir, branch\)\)",
            "BranchMarkerReady\(dataDir, branch\)",
            "SourceAssemblyExists\(GameDirectoryPath\(dataDir, branch\)\)",
            "GameRuntimeSlot\.Inspect\(dataDir, branch\)\.ReadinessProblem\(\)",
            "ReadinessProblem\(string dataDir, string branch\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherGameFiles.Redownload.cs" `
        "clears selected game, download, and runtime-pack state with evidence markers" `
        @(
            "DeleteDownloadedState\(string dataDir, string branch\)",
            "GameRuntimeSlot\.RuntimePackDirectoryPath\(dataDir, branch\)",
            "WriteRedownloadMarker",
            "DeleteDirectory\(runtimePackDirectory\)",
            "LauncherRuntimeSlotEvidence\.Clear\(dataDir\)",
            "LauncherRuntimeCacheEvidence\.Clear\(dataDir\)",
            "LauncherRuntimePatchValidationEvidence\.Clear\(dataDir\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherGameFiles.CacheCleanup.cs" `
        "orchestrates selected-version cache cleanup without deleting selected branch state" `
        @(
            "CacheCleanupMarkerRemovedRuntimePackCountPrefix",
            "DeleteInactiveRuntimePacks",
            "NewCacheCleanupMarkerLines",
            "CacheCleanupMarkerPreservedSelectedCachePrefix"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherGameFiles.CacheCleanup.Marker.Fields.cs" `
        "centralizes selected-version cache cleanup marker prefixes" `
        @(
            "CacheCleanupMarkerRuntimePacksDirectoryPresentPrefix = ""Runtime packs directory present:""",
            "CacheCleanupMarkerSelectedRuntimePackPresentBeforeCleanupPrefix = ""Selected runtime pack present before cleanup:""",
            "CacheCleanupMarkerRemovedRuntimePackCountPrefix = ""Removed runtime pack count:""",
            "CacheCleanupMarkerPreservedSelectedCachePrefix = ""Preserved selected cache:""",
            "CacheCleanupMarkerPreservedSelectedRuntimePackPrefix = ""Preserved selected runtime pack:""",
            "CacheCleanupMarkerRemovedOrphanRuntimePackPrefix = ""Removed orphan runtime pack:"""
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherGameFiles.CacheCleanup.Markers.cs" `
        "records selected runtime-pack preservation evidence in cache cleanup markers" `
        @(
            "CacheCleanupMarkerRuntimePacksDirectoryPresentPrefix",
            "CacheCleanupMarkerSelectedRuntimePackPresentBeforeCleanupPrefix",
            "CacheCleanupMarkerSelectedRuntimePackPreservedWhereApplicable",
            "CacheCleanupMarkerPreservedSelectedRuntimePackPrefix"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherGameFiles.CacheCleanup.RuntimePacks.cs" `
        "preserves selected runtime-pack cache while removing inactive runtime packs" `
        @(
            "RuntimePackDirectoryPathForStateDirectory",
            "DeleteInactiveRuntimePacks",
            "CacheCleanupMarkerPreservedSelectedRuntimePackPrefix",
            "CacheCleanupMarkerRemovedOrphanRuntimePackPrefix"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherLaunchCoordinator.cs" `
        "shows selected-runtime readiness blockers instead of generic download-required status after login" `
        @(
            "LauncherGameFiles\.ReadinessProblem\(_model\.DataDir, branch\)",
            "SelectedVersionDownloadRequiredStatus"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherLaunchCoordinator.cs" `
        "blocks normal and safe launch actions when selected runtime is not playable" `
        @(
            "LaunchPressed",
            "SafeLaunchPressed",
            "LauncherGameFiles\.Ready\(_model\.DataDir, branch\)",
            "LauncherGameFiles\.ReadinessProblem\(_model\.DataDir, branch\)",
            "Selected game version is not ready to safe launch"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherModel.Launch.cs" `
        "prevents in-process or restart launch paths from bypassing selected-runtime readiness" `
        @(
            "SelectedGameVersionReadyForLaunch",
            "LauncherGameFiles\.Ready\(_dataDir\)",
            "LauncherGameFiles\.ReadinessProblem\(_dataDir, branch\)",
            "Launch blocked"
        )
}
