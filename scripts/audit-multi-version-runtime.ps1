param(
    [switch]$Quiet
)

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "static-audit-utils.ps1")
Initialize-StaticAudit -ScriptRoot $PSScriptRoot -Quiet:$Quiet

Add-Check `
    "scripts\static-audit-utils.ps1" `
    "keeps shared static audit harness isolated from runtime-contract checks" `
    @(
        "Initialize-StaticAudit",
        "Resolve-RepoPath",
        "Read-RepoFile",
        "Add-Check",
        "Add-ForbiddenCheck",
        "Complete-StaticAudit",
        "StaticAuditFailures",
        "StaticAuditPasses",
        "StaticAuditQuiet",
        "ThrowOnFailure"
    )

Add-Check `
    "scripts\evidence-marker-utils.ps1" `
    "centralizes marker-text parsing for runtime evidence scripts" `
    @(
        "Read-MarkerValueFromText",
        "Read-MarkerIntFromText",
        "Read-MarkerRowsFromText",
        "Read-BranchFromMarkerText",
        "MissingValue",
        "OrdinalIgnoreCase",
        "\[int\]::TryParse",
        'return @\(\$MissingValue\)'
    )

Add-Check `
    "scripts\evidence-report-utils.ps1" `
    "centralizes Markdown table row formatting for runtime evidence reports" `
    @(
        "Format-Cell",
        "Add-ValidationRow",
        "Add-HypothesisRow",
        "RequiredNextAction",
        "NextProof",
        '\$Lines\.Add',
        '\| \$\(Format-Cell'
    )

Add-Check `
    "scripts\android-shell-utils.ps1" `
    "centralizes Android shell quoting for runtime evidence capture" `
    @(
        "ConvertTo-AndroidShellSingleQuoted",
        "ConvertTo-AndroidShellPathSingleQuoted",
        "Unsupported single quote in device path",
        "-split",
        "-join",
        "return ConvertTo-AndroidShellSingleQuoted"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherMarkerFile.cs" `
    "declares shared marker-file sentinel values" `
    @(
        "internal static partial class LauncherMarkerFile",
        "MissingFileValue = ""<none>""",
        "MissingLineValue = ""<missing>""",
        "ReadFailedValue = ""<read failed>"""
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherMarkerFile.Read.cs" `
    "centralizes scalar marker-file value parsing" `
    @(
        "ReadValue",
        "ReadOptionalValue",
        "File\.ReadLines",
        "StringComparison\.OrdinalIgnoreCase"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherMarkerFile.Typed.cs" `
    "centralizes typed marker-file parsing" `
    @(
        "ReadInt",
        "NumberStyles\.Integer",
        "CultureInfo\.InvariantCulture",
        "ReadUtc",
        "UtcParseable",
        "DateTimeStyles\.AdjustToUniversal",
        "ReadBoolFlag"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherMarkerFile.Values.cs" `
    "centralizes marker-file repeated-value reads" `
    @(
        "ReadJoinedValues",
        "ReadValues",
        "File\.ReadLines",
        "valueFormatter",
        "maxValues"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherMarkerFile.Predicates.cs" `
    "centralizes marker-file predicates and counts" `
    @(
        "CountLines",
        "HasLine",
        "HasConcreteValue"
    )

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
        "FileIdentityMatches",
        "FileIdentity",
        "AndroidAppPrivatePathAlias",
        "/data/data/",
        "/data/user/0/",
        "NormalizePath",
        "HasMarkerValue"
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
    "src\STS2Mobile\Launcher\RuntimeSlotMetadata.Fields.cs" `
    "centralizes branch-marker depot provenance prefixes used in runtime-slot identity" `
    @(
        "DepotManifestCountPrefix = ""Depot manifest count:""",
        "DepotsMatchingPublicPrefix = ""Depot manifests matching public count:""",
        "DepotsDifferingFromPublicPrefix = ""Depot manifests differing from public count:""",
        "DepotsInheritedFromPublicPrefix = ""Depot manifests inherited from public count:""",
        "DepotsMissingSelectedManifestPrefix = ""Depot manifests missing selected branch manifest count:""",
        "DepotManifestRowPrefix = ""Depot manifest:"""
    )

Add-Check `
    "src\STS2Mobile\Launcher\RuntimeSlotMetadata.Inspect.cs" `
    "combines release-info and depot marker provenance into runtime-slot metadata" `
    @(
        "Inspect",
        "ReadReleaseInfo",
        "ReadMarkerValue",
        "DepotManifestCountPrefix",
        "DepotsDifferingFromPublicPrefix",
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
        "DepotManifestRowPrefix",
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
        "Removed runtime pack count",
        "DeleteInactiveRuntimePacks",
        "NewCacheCleanupMarkerLines",
        "Preserved selected cache"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherGameFiles.CacheCleanup.Markers.cs" `
    "records selected runtime-pack preservation evidence in cache cleanup markers" `
    @(
        "Runtime packs directory present",
        "Selected runtime pack present before cleanup",
        "CacheCleanupMarkerSelectedRuntimePackPreservedWhereApplicable",
        "Preserved selected runtime pack"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherGameFiles.CacheCleanup.RuntimePacks.cs" `
    "preserves selected runtime-pack cache while removing inactive runtime packs" `
    @(
        "RuntimePackDirectoryPathForStateDirectory",
        "DeleteInactiveRuntimePacks",
        "Preserved selected runtime pack",
        "Removed orphan runtime pack"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherController.Session.Actions.cs" `
    "shows selected-runtime readiness blockers instead of generic download-required status after login" `
    @(
        "LauncherGameFiles\.ReadinessProblem\(_model\.DataDir, branch\)",
        "SelectedVersionDownloadRequiredStatus"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherController.Startup.Launch.cs" `
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

Add-Check `
    "src\STS2Mobile\Launcher\RuntimePackManifest.cs" `
    "declares runtime-pack compatibility metadata and hash evidence shape" `
    @(
        "internal sealed partial class RuntimePackManifest",
        "AndroidAssemblySha256",
        "SourceRuntimeSlotId",
        "PatchValidationStatus",
        "PatchValidationReport",
        "ValidationMode",
        "ValidationSurfaceVersion",
        "GeneratedFromCleanDirectory",
        "SupportAssembliesDeclared",
        "SupportAssemblySha256Declared",
        "CheckedSymbolCount",
        "PresentSymbolCount",
        "MissingSymbolCount",
        "PatchValidationPassed",
        "BranchMatches",
        "AndroidAssemblyHashMatches"
    )

Add-Check `
    "src\STS2Mobile\Launcher\RuntimePackManifest.Inspect.cs" `
    "orchestrates runtime-pack compatibility manifest inspection" `
    @(
        "Inspect",
        "RuntimePackManifestInspectionContext",
        "File\.Exists\(context\.ManifestPath\)",
        "NotInstalled\(context\)",
        "InspectReadable",
        "Unreadable\(context, ex\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\RuntimePackManifest.Inspect.Context.cs" `
    "keeps runtime-pack inspection path and Android assembly context typed" `
    @(
        "RuntimePackManifestInspectionContext",
        "ManifestPath",
        "ExpectedBranch",
        "AndroidAssemblyPath",
        "AndroidAssemblyExists",
        "SteamGameBranch\.Normalize",
        "RuntimePackManifest\.AndroidAssemblyFileName",
        "File\.Exists\(AndroidAssemblyPath\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\RuntimePackManifest.Inspect.Missing.cs" `
    "builds not-installed runtime-pack inspection results in one place" `
    @(
        "NotInstalled",
        "RuntimePackManifestInspectionContext",
        """not installed""",
        "exists: false",
        "readable: false",
        "androidAssemblyExists: false",
        """<missing>"""
    )

Add-Check `
    "src\STS2Mobile\Launcher\RuntimePackManifest.Inspect.Readable.cs" `
    "reads runtime-pack compatibility metadata from compatibility.json" `
    @(
        "InspectReadable",
        "ReadManifest",
        "JsonDocument\.Parse",
        "RuntimePackStatus\(manifest, selectedPckSha256, selectedSourceAssemblySha256, selectedPckPath\)",
        "ReadString\(root, ""packId""",
        "ReadString\(root, ""sourceRuntimeSlotId""",
        "ReadString\(root, ""sourcePckSha256""",
        "ReadString\(root, ""sourceAssemblySha256""",
        """androidAssemblySha256""",
        "ReadStringArray\(root, ""supportAssemblies""",
        "ReadStringDictionary\(root, ""supportAssemblySha256""",
        "generatedFromCleanDirectory"
    )

Add-Check `
    "src\STS2Mobile\Launcher\RuntimePackManifest.Inspect.Unreadable.cs" `
    "builds unreadable runtime-pack inspection results in one place" `
    @(
        "Unreadable",
        "RuntimePackManifestInspectionContext",
        "Exception exception",
        "\$""unreadable: \{exception\.GetType\(\)\.Name\}""",
        "exists: true",
        "readable: false",
        """<not inspected>""",
        """<missing>"""
    )

Add-Check `
    "src\STS2Mobile\Launcher\RuntimePackManifest.Validation.cs" `
    "orchestrates runtime-pack usability status checks" `
    @(
        "RuntimePackStatus",
        "runtime pack was not generated from a clean directory",
        "missing runtime pack support assembly declaration",
        "missing runtime pack support assembly hashes",
        "missing Android assembly hash",
        "missing patch validation status",
        "patch validation not passed",
        "missing patch validation report",
        "missing patch validation report file",
        "patch validation report mismatch",
        "missing runtime pack ID",
        "missing source runtime slot ID",
        "missing source PCK hash",
        "missing source assembly hash"
    )

Add-Check `
    "src\STS2Mobile\Launcher\RuntimePackManifest.Validation.SourcePck.cs" `
    "validates runtime-pack source PCK provenance including patched PCK markers" `
    @(
        "SourcePckMatches",
        "MatchesDeclared",
        "AndroidPckPatchMarkerFileName",
        "sourcePckSha256",
        "GetLastWriteTimeUtc",
        "JsonDocument\.Parse"
    )

Add-Check `
    "src\STS2Mobile\Launcher\RuntimePackManifest.Validation.PatchReport.cs" `
    "validates runtime-pack patch validation report identity" `
    @(
        "PatchValidationReportMatches",
        "StringPropertyMatches",
        "BoolPropertyMatches",
        "runtimePackId",
        "sourceRuntimeSlotId",
        "sourceAssemblySha256",
        "supportAssemblies",
        "supportAssemblySha256",
        "generatedFromCleanDirectory"
    )

Add-Check `
    "src\STS2Mobile\Launcher\RuntimePackManifest.Validation.SupportAssemblies.cs" `
    "validates runtime-pack support assembly closure" `
    @(
        "RuntimePackSupportAssemblyProblem",
        "runtime pack contains undeclared DLL",
        "runtime pack support assembly hash missing",
        "runtime pack support assembly is duplicated",
        "runtime pack support assemblies must not redeclare sts2\.dll",
        "EnumerateFiles\(manifest\.DirectoryPath, ""\*\.dll"""
    )

Add-Check `
    "src\STS2Mobile\Launcher\RuntimePackManifest.Validation.JsonMatches.cs" `
    "keeps runtime-pack patch-report JSON comparison helpers isolated" `
    @(
        "StringPropertyMatches",
        "BoolPropertyMatches",
        "StringArrayPropertyMatches",
        "StringDictionaryPropertyMatches",
        "JsonElement",
        "OrdinalIgnoreCase"
    )

Add-Check `
    "src\STS2Mobile\Launcher\RuntimePackManifest.Json.cs" `
    "keeps runtime-pack manifest JSON field readers isolated" `
    @(
        "ReadString",
        "ReadInt",
        "ReadStringArray",
        "ReadStringDictionary",
        "HasProperty",
        "ReadBool"
    )

Add-Check `
    "src\STS2Mobile\Launcher\RuntimePackWriter.cs" `
    "orchestrates branch-local runtime pack generation after selected-version validation passes" `
    @(
        "WriteValidatedRuntimePack",
        "compatibility\.json",
        "patch_validation\.json",
        "sts2\.dll",
        "ValidationSurfaceVersion",
        "PreparePackDirectory",
        "CopyRuntimeAssembly",
        "BuildRuntimePackWriteContext",
        "BuildCompatibilityManifestPayload",
        "BuildPatchValidationReportPayload",
        "WriteJson",
        "Wrote runtime pack"
    )

Add-Check `
    "src\STS2Mobile\Launcher\RuntimePackWriter.Assemblies.cs" `
    "prepares a clean runtime-pack directory and copies branch-specific runtime assemblies" `
    @(
        "RuntimeSupportAssemblyFileNames",
        "PreparePackDirectory",
        "Directory\.Delete\(packDirectory, recursive: true\)",
        "Directory\.CreateDirectory\(packDirectory\)",
        "CopyRuntimeAssembly",
        "File\.Copy",
        "RuntimeAssemblyFileName",
        "CopyRuntimeSupportAssemblies",
        "Runtime packs only swap the branch-specific game assembly"
    )

Add-Check `
    "src\STS2Mobile\Launcher\RuntimePackWriter.Context.cs" `
    "builds typed runtime-pack writer context and symbol summaries" `
    @(
        "RuntimePackWriteContext",
        "BuildRuntimePackWriteContext",
        "symbolChecks \?\?= Array\.Empty",
        "missingSymbols",
        "BuildCategorySummaries",
        "categorySummaries",
        "checkedCount",
        "presentCount",
        "missingCount",
        "GameRuntimeSlot\.BuildRuntimePackSlotId",
        "GameRuntimeSlot\.BuildRuntimePackSlotIdentity"
    )

Add-Check `
    "src\STS2Mobile\Launcher\RuntimePackWriter.Payloads.cs" `
    "builds runtime-pack compatibility manifest and patch validation report payloads" `
    @(
        "BuildCompatibilityManifestPayload",
        "BuildPatchValidationReportPayload",
        "sourceRuntimeSlotId",
        "sourceRuntimeSlotIdentity",
        "sourceBranch",
        "releaseVersion",
        "releaseCommit",
        "releaseBuildId",
        "depotManifestFingerprint",
        "sourcePckSha256",
        "sourceAssemblySha256",
        "androidAssemblySha256",
        "patchValidationStatus\s*=\s*""passed""",
        "patchSetVersion",
        "validationSurfaceVersion",
        "checkedSymbolCount",
        "categorySummaries",
        "generatedFromCleanDirectory",
        "supportAssemblies",
        "supportAssemblySha256",
        "missingSymbols",
        "symbolChecks"
    )

Add-Check `
    "src\STS2Mobile\Launcher\RuntimePackWriter.Identity.cs" `
    "keeps runtime-pack IDs stable from selected PCK and source assembly hashes" `
    @(
        "RuntimePackId",
        "ShortHash",
        "slot\.PckSha256",
        "slot\.SourceAssemblySha256",
        "patchSetVersion",
        "unknown"
    )

Add-Check `
    "src\STS2Mobile\Launcher\RuntimePackWriter.Delete.cs" `
    "deletes invalid runtime packs without touching unrelated branch state" `
    @(
        "DeleteRuntimePack",
        "Path\.GetDirectoryName\(slot\.RuntimePackManifestPath\)",
        "Directory\.Delete\(packDirectory, recursive: true\)",
        "Deleted runtime pack",
        "Failed to delete runtime pack"
    )

Add-Check `
    "src\STS2Mobile\Launcher\PatchCompatibilityValidator.cs" `
    "defines static patch compatibility validation identity" `
    @(
        "internal static partial class PatchCompatibilityValidator",
        "PatchSetVersion",
        "ValidationMode",
        "ValidationSurfaceVersion",
        "static-critical-symbol-scan"
    )

Add-Check `
    "src\STS2Mobile\Launcher\PatchCompatibilityValidator.Symbols.cs" `
    "declares critical startup/save/model/platform symbols" `
    @(
        "RequiredCriticalSymbols",
        "SymbolCheck",
        "GameStartupWrapper",
        "SaveManager",
        "ModelDb",
        "PlatformUtil"
    )

Add-Check `
    "src\STS2Mobile\Launcher\PatchCompatibilityValidator.Scan.cs" `
    "scans selected source assembly for critical symbols" `
    @(
        "CheckSymbols",
        "File\.ReadAllBytes",
        "ContainsAscii",
        "RequiredCriticalSymbols",
        "source sts2\.dll could not be read"
    )

Add-Check `
    "src\STS2Mobile\Launcher\PatchCompatibilityValidator.Marker.cs" `
    "writes patch compatibility marker payload with symbol summaries" `
    @(
        "WriteMarker",
        "categorySummaries",
        "checkedSymbolCount",
        "presentSymbolCount",
        "ValidationSurfaceVersion",
        "PatchSetVersion"
    )

Add-Check `
    "src\STS2Mobile\Launcher\PatchCompatibilityValidator.Validation.cs" `
    "creates or deletes runtime packs after non-public selected-version validation" `
    @(
        "ValidateSelectedVersion",
        "RuntimePackWriter\.WriteValidatedRuntimePack",
        "RuntimePackWriter\.DeleteRuntimePack",
        "RuntimePackSlotIdMatches",
        "CheckSymbols",
        "WriteMarker",
        "BranchMarkerReady"
    )

Add-Check `
    "src\STS2Mobile\Launcher\PatchCompatibilityEvidence.cs" `
    "models patch validation evidence identity, hashes, symbol counts, and match state" `
    @(
        "GameDirectoryMarkerFileName",
        "internal sealed partial class PatchCompatibilityEvidence",
        "PatchCompatibilityEvidence\(",
        "ValidatedPckSha256",
        "ValidatedSourceAssemblySha256",
        "CheckedSymbolCount",
        "MissingSymbolCount",
        "PckMatches",
        "SourceAssemblyMatches"
    )

Add-Check `
    "src\STS2Mobile\Launcher\PatchCompatibilityEvidence.Status.cs" `
    "requires branch, PCK, and source assembly matches before patch evidence can pass" `
    @(
        "PassedStatus",
        "Passed",
        "StringComparison\.OrdinalIgnoreCase",
        "BranchMatches",
        "PckMatches",
        "SourceAssemblyMatches"
    )

Add-Check `
    "src\STS2Mobile\Launcher\PatchCompatibilityEvidence.Problem.cs" `
    "formats patch validation blockers for missing, unreadable, mismatched, and failed evidence" `
    @(
        "Problem",
        "Selected game version has no Android patch compatibility validation evidence",
        "unreadable Android patch validation evidence",
        "different Steam branch",
        "does not declare the validated PCK",
        "different PCK",
        "does not declare the validated game-code assembly",
        "different game-code assembly",
        "failed Android patch compatibility validation"
    )

Add-Check `
    "src\STS2Mobile\Launcher\PatchCompatibilityEvidence.Factory.cs" `
    "creates normalized missing patch validation evidence states" `
    @(
        "Missing",
        "SteamGameBranch\.Normalize",
        "validation evidence not found",
        "required: true",
        "exists: false",
        "readable: false",
        "branchMatches: false",
        "pckMatches: false",
        "sourceAssemblyMatches: false"
    )

Add-Check `
    "src\STS2Mobile\Launcher\PatchCompatibilityEvidence.Inspect.cs" `
    "selects patch validation evidence from runtime pack or game directory sources" `
    @(
        "patch_validation\.json",
        "PatchValidationPassed",
        'runtimePack\?\.Usable == true',
        "ValidationMode",
        "ValidationSurfaceVersion",
        "CheckedSymbolCount",
        "MissingSymbolCount",
        "runtime pack validation report",
        "selected game directory validation marker",
        "RuntimePackReportFileName",
        "ReadValidationMarker"
    )

Add-Check `
    "src\STS2Mobile\Launcher\PatchCompatibilityEvidence.Markers.cs" `
    "dispatches patch validation marker reads and unreadable/missing marker states" `
    @(
        "ReadValidationMarker",
        "MissingValidationMarker",
        "JsonDocument\.Parse",
        "ReadValidationDocument",
        "UnreadableValidationMarker"
    )

Add-Check `
    "src\STS2Mobile\Launcher\PatchCompatibilityEvidence.Markers.Document.cs" `
    "parses patch validation marker identity and symbol-count evidence" `
    @(
        "ReadValidationDocument",
        "patchValidationStatus",
        "sourcePckSha256",
        "sourceAssemblySha256",
        "checkedSymbolCount",
        "missingSymbolCount",
        "ValidationMode",
        "ValidationSurfaceVersion",
        "MatchesBranch",
        "MatchesDeclared"
    )

Add-Check `
    "src\STS2Mobile\Launcher\PatchCompatibilityEvidence.Markers.Factory.cs" `
    "creates missing and unreadable patch validation marker evidence states" `
    @(
        "MissingValidationMarker",
        "validation evidence not found",
        "exists: false",
        "readable: false",
        "UnreadableValidationMarker",
        "unreadable",
        "ex\.GetType\(\)\.Name",
        "exists: true"
    )

Add-Check `
    "src\STS2Mobile\Launcher\PatchCompatibilityEvidence.Markers.Match.cs" `
    "matches patch validation marker branch, PCK, and source assembly identity" `
    @(
        "MatchesBranch",
        "SteamGameBranch\.Normalize",
        "StringComparison\.OrdinalIgnoreCase",
        "MatchesDeclared",
        "actualValue\.StartsWith",
        "<",
        "string\.Equals\(declaredValue, actualValue"
    )

Add-Check `
    "src\STS2Mobile\Launcher\PatchCompatibilityEvidence.Markers.Json.cs" `
    "reads string and integer values from patch validation JSON markers" `
    @(
        "ReadString",
        "JsonValueKind\.String",
        "value\.GetString\(\)",
        "ReadInt",
        "JsonValueKind\.Number",
        "value\.TryGetInt32",
        "int\.TryParse"
    )

Add-Check `
    "android\src\com\game\sts2launcher\GodotApp.java" `
    "uses branch/runtime-pack-aware assembly cache identity and records prepared runtime-cache evidence" `
    @(
        "KEY_ASSEMBLY_CACHE_RUNTIME_ID",
        "CURRENT_RUNTIME_CACHE_MARKER",
        "BRANCH_GAME_CODE_ASSEMBLIES",
        "hasStaleCachedBranchGameCodeAssemblies",
        "matchesPackagedAsset",
        "Launcher bootstrap assembly cache contains stale branch game-code assembly",
        "sha256FileBase64",
        "RUNTIME_PACKS_DIRECTORY",
        "RUNTIME_PACK_COMPATIBILITY_MANIFEST",
        "RUNTIME_PACK_PATCH_VALIDATION_REPORT",
        "CURRENT_RUNTIME_SLOT_MARKER",
        "findRuntimePackDir",
        "isRuntimePackManifestUsable",
        "runtimePackSupportAssembliesUsable",
        "isRuntimeSlotEvidenceReadyForLaunch",
        "Blocking selected game startup because runtime slot evidence is missing",
        "Blocking selected game startup because runtime slot evidence is not playable",
        "Runtime slot evidence ready for startup",
        "pckMatches",
        "sourceAssemblyMatches",
        "packId",
        "sourceRuntimeSlotId",
        "sourceBranch",
        "sourcePckSha256",
        "sourceAssemblySha256",
        "androidAssemblySha256",
        "generatedFromCleanDirectory",
        "patchValidationStatus",
        "Runtime pack was not generated from a clean directory",
        "Runtime pack support assembly hash set does not match declared support assemblies",
        "Runtime pack contains undeclared DLL",
        "Runtime pack patch validation report did not pass",
        "Runtime pack patch validation report does not match compatibility manifest",
        "Runtime pack branch mismatch",
        "Runtime pack selected PCK hash mismatch",
        "Runtime pack selected source assembly hash mismatch",
        "Runtime pack cannot be matched because selected PCK is missing",
        "Runtime pack cannot be matched because selected source sts2\.dll is missing",
        "Selected non-public branch requires a usable runtime pack",
        "Skipping selected-game branch code assembly without usable runtime pack",
        "no-usable-runtime",
        "Selected branch requires runtime pack:",
        "Game assembly cache is not current because selected non-public branch has no usable runtime pack",
        "runtimePackGameAssembly",
        "runtimeSource=",
        "runtimePackIdentity",
        "runtimePackDeclaredAssemblyNames",
        "manifest-declared runtime-pack assemblies",
        "appendRuntimePackFileIdentity",
        "compatibility\.json",
        "patch_validation\.json",
        "currentRuntimeCacheId",
        "writeRuntimeCacheMarker",
        "Runtime ID:",
        "Publish cache active sts2\.dll SHA256:",
        "Assembly cache runtime changed",
        "Copied .* runtime-pack assembly files",
        "shouldCopyGameAssemblyFile"
    )

Add-Check `
    "android\src\com\game\sts2launcher\NativeFallbackActivity.java" `
    "surfaces runtime-slot evidence in native fallback diagnostics" `
    @(
        "CURRENT_RUNTIME_SLOT_MARKER",
        "appendRuntimeSlotState",
        "Runtime slot evidence exists",
        "Runtime slot files ready",
        "Runtime slot playable",
        "Runtime slot runtime compatible",
        "Runtime slot patch compatible",
        "Runtime slot PCK hash matches selected file",
        "Runtime slot source sts2\.dll hash matches selected file",
        "Runtime slot readiness problem",
        "Runtime slot runtime pack usability",
        "Runtime slot patch compatibility status"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherRuntimeSlotEvidence.cs" `
    "keeps persistent selected runtime-slot marker lifecycle isolated" `
    @(
        "current_runtime_slot\.json",
        "MarkerPath",
        "MarkerPresent",
        "Clear",
        "File\.Delete",
        "Failed to clear runtime slot evidence marker"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherRuntimeSlotEvidence.Write.cs" `
    "writes persistent selected runtime-slot evidence after download validation" `
    @(
        "Write\(string dataDir, string branch, bool filesReady, string readinessProblem\)",
        "GameRuntimeSlot\.Inspect\(dataDir, branch\)",
        "BuildPayload",
        "runtimeSlotId",
        "requiresUsableRuntimePack",
        "filesReady",
        "readinessProblem",
        "runtimePackUsabilityStatus",
        "patchCompatibilityStatus",
        "patchCompatibilityValidationMode",
        "patchCompatibilityMissingSymbolCount",
        "Runtime slot evidence marker written"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherRuntimeSlotEvidence.Read.cs" `
    "reads selected runtime-slot evidence through named marker properties" `
    @(
        "RuntimeSlotIdProperty",
        "BranchProperty",
        "FilesReadyProperty",
        "ReadinessProblemProperty",
        "RuntimePackUsabilityStatusProperty",
        "PatchCompatibilityStatusProperty",
        "PckSha256Property",
        "SourceAssemblySha256Property",
        "ReadString",
        "JsonDocument\.Parse",
        "TryGetProperty",
        "JsonValueKind\.True",
        "JsonValueKind\.False"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherRuntimeSlotEvidence.Match.cs" `
    "compares runtime-slot marker evidence against the selected runtime" `
    @(
        "BranchMatchesSelectedRuntime",
        "RuntimeSlotIdMatchesSelectedRuntime",
        "PckMatchesSelectedRuntime",
        "SourceAssemblyMatchesSelectedRuntime",
        "MarkerValueMatchesSelectedRuntime",
        "GameRuntimeSlot\.Inspect\(dataDir, branch\)",
        "StringComparison\.OrdinalIgnoreCase"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherRuntimeCacheEvidence.cs" `
    "keeps native prepared-runtime cache marker lifecycle isolated" `
    @(
        "internal static partial class LauncherRuntimeCacheEvidence",
        "current_runtime_cache\.txt",
        "MarkerPath",
        "MarkerPresent",
        "Clear",
        "Failed to clear runtime cache evidence marker"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherRuntimeCacheEvidence.Fields.cs" `
    "centralizes native prepared-runtime cache marker prefixes" `
    @(
        "UtcMillisPrefix = ""UTC millis:""",
        "PackagePrefix = ""Package:""",
        "SelectedBranchPrefix = ""Selected branch:""",
        "SelectedBranchRequiresRuntimePackPrefix = ""Selected branch requires runtime pack:""",
        "RuntimeIdPrefix = ""Runtime ID:""",
        "RuntimeSourcePrefix = ""Runtime source:""",
        "RuntimePackDirectoryPrefix = ""Runtime pack directory:""",
        "SelectedPckSha256Prefix = ""Selected PCK SHA256:""",
        "SelectedSourceAssemblySha256Prefix = ""Selected source sts2\.dll SHA256:""",
        "PublishCacheActiveAssemblySha256Prefix = ""Publish cache active sts2\.dll SHA256:"""
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherRuntimeCacheEvidence.Read.cs" `
    "reads native prepared-runtime cache marker fields through named prefixes" `
    @(
        "UtcMillis",
        "Package",
        "SelectedBranch",
        "SelectedBranchRequiresRuntimePack",
        "RuntimeId",
        "RuntimeSource",
        "SelectedPckSha256",
        "SelectedSourceAssemblySha256",
        "PublishCacheActiveAssemblySha256",
        "ReadMarkerValue",
        "LauncherMarkerFile\.ReadValue",
        "LauncherMarkerFile\.HasConcreteValue",
        "HasValue"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherRuntimeCacheEvidence.Match.cs" `
    "compares native prepared-runtime cache marker values to the selected runtime slot" `
    @(
        "MatchesSelectedBranch",
        "SteamGameBranch\.Normalize",
        "PckMatchesSelectedRuntime",
        "GameRuntimeSlot\.Inspect\(dataDir, selectedBranch\)",
        "SourcePckMatchesSelectedPck",
        "SourceAssemblyMatchesSelectedRuntime",
        "PublishCacheMatchesSelectedRuntime",
        "RequiresRuntimePackOrPreparedCache",
        "UsesLegacyPackagedPublicRuntime",
        "CachePreparedForSelectedRuntime",
        "PublishCacheMatchesSelectedRuntime\(dataDir, selectedBranch\)"
    )

Add-Check `
    "src\STS2Mobile\Startup\StartupPatchOrchestrator.cs" `
    "orchestrates startup patch groups through the critical failure gate" `
    @(
        "internal static partial class StartupPatchOrchestrator",
        "Apply\(Harmony harmony\)",
        "ForceCriticalPatchFailureRequested\(\)",
        "ApplyGroup\(group, harmony\)",
        "criticalFailed = true",
        "Patch orchestration finished"
    )

Add-Check `
    "src\STS2Mobile\Startup\StartupPatchOrchestrator.ForcedFailure.cs" `
    "keeps forced critical startup patch failure available for fallback validation" `
    @(
        "ForceCriticalPatchFailureVariable",
        "STS2_FORCE_CRITICAL_PATCH_FAILURE",
        "ForcedCriticalPatchFailureResult",
        "Forced critical patch failure requested for fallback validation",
        "criticalFailed: true"
    )

Add-Check `
    "src\STS2Mobile\Startup\StartupPatchOrchestrator.Execution.cs" `
    "captures patch helper failures while timing startup patch steps" `
    @(
        "ApplyGroup",
        "ApplyStep",
        "PatchHelper\.LogEmitted \+= CaptureHelperFailure",
        "PatchHelper\.LogEmitted -= CaptureHelperFailure",
        "IsHelperPatchFailure",
        "FormatHelperFailures",
        "Starting patch step",
        "Failed patch step"
    )

Add-Check `
    "src\STS2Mobile\Startup\StartupPatchOrchestrator.Groups.cs" `
    "keeps startup patch groups ordered by criticality" `
    @(
        "Core\(\)",
        "Gameplay\(\)",
        "Optional\(\)",
        "PlatformPatches\.Apply",
        "ModelDbInitPatch\.Apply",
        "LauncherPatches\.Apply",
        "LanMultiplayerPatcher\.Apply"
    )

Add-Check `
    "src\STS2Mobile\Patches\LanMultiplayerPatcher.Apply.cs" `
    "orchestrates LAN multiplayer Harmony patch registration" `
    @(
        "internal static void Apply\(Harmony harmony\)",
        "JoinScreenReadyPostfix",
        "OnSubmenuOpenedPrefix",
        "JoinScreenClosedPostfix",
        "PatchHostService\(harmony, hostServiceType, patcherType\)",
        "PatchPlayerNameStrategy\(harmony, sts2Asm, patcherType\)",
        "LAN multiplayer patches applied"
    )

Add-Check `
    "src\STS2Mobile\Patches\LanMultiplayerPatcher.Apply.HostService.cs" `
    "keeps LAN host-service patches isolated from main Apply orchestration" `
    @(
        "PatchHostService",
        "StartENetHost",
        "Disconnect",
        "StartENetHostPostfix",
        "DisconnectPostfix",
        "BindingFlags\.Public \| BindingFlags\.Instance"
    )

Add-Check `
    "src\STS2Mobile\Patches\LanMultiplayerPatcher.Apply.PlayerName.cs" `
    "keeps LAN player-name strategy patch isolated from main Apply orchestration" `
    @(
        "PatchPlayerNameStrategy",
        "NullPlatformUtilStrategy",
        "GetPlayerName",
        "GetPlayerNamePrefix",
        "BindingFlags\.Public \| BindingFlags\.Instance"
    )

Add-Check `
    "src\STS2Mobile\Patches\UiScalePatches.cs" `
    "keeps unsafe dropdown replacement disabled while preserving window-scale runtime patches" `
    @(
        "UI scale dropdown replacement disabled",
        "cannot safely Harmony-wrap private-field-heavy resolution dropdown methods",
        "PatchWindowChangeHandlers\(harmony, sts2Asm\)",
        "UiScaleChanged",
        "UiScalePercent"
    )

Add-Check `
    "src\STS2Mobile\Patches\UiScalePatches.Dropdown.cs" `
    "keeps dormant UI scale dropdown Harmony registration isolated" `
    @(
        "PatchResolutionDropdown",
        "PatchResolutionDropdownItem",
        "PatchSettingsScreen",
        "RefreshEnabled",
        "PopulateDropdownItems",
        "RefreshCurrentlySelectedResolution",
        "OnDropdownItemSelected",
        "LocalizeLabels",
        "PatchHelper\.Method"
    )

Add-Check `
    "src\STS2Mobile\Patches\UiScalePatches.Dropdown.Selection.cs" `
    "keeps dropdown scale selection prefixes isolated" `
    @(
        "RefreshEnabledPrefix",
        "PopulateScaleItemsPrefix",
        "RefreshScaleLabelPrefix",
        "ScaleItemSelectedPrefix",
        "ClearDropdownItems",
        "PackedScene\.GenEditState\.Disabled",
        "UiScalePercent = newScale",
        "SaveUiScale\(\)",
        "ApplyUiScale\(\)"
    )

Add-Check `
    "src\STS2Mobile\Patches\UiScalePatches.Dropdown.ItemInit.cs" `
    "keeps dropdown item initialization prefix isolated" `
    @(
        "ResolutionItemInitPrefix",
        "setResolution\.Y != 0",
        "resolution",
        "SetTextAutoSize",
        "ResolutionItemInitPrefix failed"
    )

Add-Check `
    "src\STS2Mobile\Patches\UiScalePatches.Dropdown.SettingsLabel.cs" `
    "keeps settings label rename postfix isolated" `
    @(
        "LocalizeLabelsPostfix",
        "%GraphicsSettings",
        "WindowedResolution",
        "UI Scale",
        "LocalizeLabels postfix failed"
    )

Add-Check `
    "src\STS2Mobile\Patches\PlatformPatches.PlatformUtil.cs" `
    "orchestrates Android PlatformUtil patch registration" `
    @(
        "ApplyPlatformUtilPatches",
        "PatchPlatformUtilStaticConstructor\(harmony\)",
        "PatchPlatformUtilPrimaryPlatformGetter\(harmony\)",
        "PatchGetPlatformUtil\(harmony\)",
        "PatchPlatformUtilMethods\(harmony\)",
        "PlatformUtil patch failed"
    )

Add-Check `
    "src\STS2Mobile\Patches\PlatformPatches.PlatformUtil.Registration.cs" `
    "registers PlatformUtil Harmony prefixes and guards incompatible branch signatures" `
    @(
        "PatchPlatformUtilStaticConstructor",
        "PatchPlatformUtilPrimaryPlatformGetter",
        "PatchGetPlatformUtil",
        "PatchPlatformUtilMethods",
        "PatchPlatformUtilMethod",
        "Prefix\(string prefixName\)",
        "GetPlatformBranch",
        "method\.ReturnType != typeof\(string\)",
        "new HarmonyMethod\(prefix\)",
        "Patched PlatformUtil"
    )

Add-Check `
    "src\STS2Mobile\Patches\PlatformPatches.PlatformUtil.LocaleStrategy.cs" `
    "patches NullPlatformUtilStrategy language lookup without desktop initialization" `
    @(
        "ApplyNullPlatformLanguagePatch",
        "FindGetThreeLetterLanguageCode",
        "NullPlatformUtilStrategy",
        "GetThreeLetterLanguageCode",
        "BindingFlags\.Public \| BindingFlags\.Instance",
        "Patched NullPlatformUtilStrategy\.GetThreeLetterLanguageCode",
        "Locale fix failed"
    )

Add-Check `
    "src\STS2Mobile\Patches\PlatformPatches.PlatformUtil.Runtime.cs" `
    "replaces desktop PlatformUtil runtime state with Android-safe null strategy prefixes" `
    @(
        "PlatformUtilStaticConstructorPrefix",
        "PrimaryPlatformPrefix",
        "GetPlatformUtilPrefix",
        "GetAndroidNullPlatformStrategy",
        "NullPlatformField",
        "PlatformType\.None",
        "new NullPlatformUtilStrategy\(\)",
        "Skipped PlatformUtil desktop static initialization on Android",
        "NullPlatformUtilStrategy unavailable on Android"
    )

Add-Check `
    "src\STS2Mobile\Patches\ModelDbInitPatch.cs" `
    "orchestrates patched two-phase ModelDb initialization" `
    @(
        "internal static partial class ModelDbInitPatch",
        "typeof\(ModelDb\)",
        "InitPrefix",
        "TryLoadModelDbInitAccess",
        "RuntimeHelpers\.GetUninitializedObject",
        "new Harmony\(HarmonyId\)",
        "RunConstructors\(types, typeObjects\)",
        "harmony\.Unpatch\(containsMethod, containsPrefix\)",
        "LogPhase2Result\(types\.Length, phase2\.SuccessCount, phase2\.Failed\)"
    )

Add-Check `
    "src\STS2Mobile\Patches\ModelDbInitPatch.Access.cs" `
    "keeps ModelDb reflection access and fallback checks isolated" `
    @(
        "TryLoadModelDbInitAccess",
        "AllAbstractModelSubtypesProperty",
        "GetIdMethodName",
        "ContentByIdField",
        "SetItemMethod",
        "ContainsMethodName",
        "ModelDb\.Init fallback: AllAbstractModelSubtypes property missing",
        "ModelDb\.Init fallback: _contentById is null",
        "ModelDb\.Init fallback: Contains\(Type\) method missing"
    )

Add-Check `
    "src\STS2Mobile\Patches\ModelDbInitPatch.Constructors.cs" `
    "runs ModelDb constructors while guaranteeing Contains suppression cleanup" `
    @(
        "ConstructorRunResult",
        "RunConstructors",
        "_suppressContains = true",
        "try",
        "RuntimeHelpers\.RunClassConstructor",
        "ctor\.Invoke",
        "finally",
        "_suppressContains = false",
        "LogPhase2Result",
        "WARNING: \{failed\.Count\}/\{totalCount\} types had constructor errors",
        "All \{successCount\} model types registered successfully"
    )

Add-Check `
    "src\STS2Mobile\Patches\ModelDbInitPatch.Contains.cs" `
    "keeps temporary ModelDb Contains suppression isolated" `
    @(
        "internal static partial class ModelDbInitPatch",
        "_suppressContains = false",
        "ContainsPrefix",
        "__result = false",
        "return false",
        "return true"
    )

Add-Check `
    "src\STS2Mobile\Patches\SettingsPatches.cs" `
    "wires mobile settings compatibility patches into startup settings initialization" `
    @(
        "internal static partial class SettingsPatches",
        "InitSettingsDataMethod",
        "PatchGetter",
        "SkipIntroLogoProperty",
        "PatchVSyncString\(harmony\)",
        "PatchLocStringGetFormattedText\(harmony\)"
    )

Add-Check `
    "src\STS2Mobile\Patches\SettingsPatches.MobileDefaults.cs" `
    "applies first-run mobile defaults without repeatedly overwriting user settings" `
    @(
        "InitSettingsDataPostfix",
        "_mobileDefaultsChecked",
        "ApplyMobileDefaultsIfNeeded",
        "SaveManager\.Instance\.SettingsSave",
        "VSyncType\.On",
        "AspectRatioSetting\.Auto",
        "settings\.SkipIntroLogo = true",
        "MobileDefaultsMarkerPath",
        "SkipIntroLogoPrefix",
        "OperatingSystem\.IsAndroid\(\)"
    )

Add-Check `
    "src\STS2Mobile\Patches\SettingsPatches.VSync.cs" `
    "keeps VSync setting copy mapped to the corrected localization keys" `
    @(
        "PatchVSyncString",
        "GetVSyncStringPrefix",
        "GetVSyncText",
        "VSyncKeyFor",
        "VSyncOffKey",
        "VSyncOnKey",
        "VSyncAdaptiveKey"
    )

Add-Check `
    "src\STS2Mobile\Patches\SettingsPatches.LocString.cs" `
    "keeps Android LocString null fallback isolated and logged once" `
    @(
        "PatchLocStringGetFormattedText",
        "GetFormattedTextFinalizer",
        "NullReferenceException",
        "OperatingSystem\.IsAndroid\(\)",
        "GetLocStringFallback",
        "ReadStringMember",
        "Interlocked\.Exchange",
        "LocString\.GetFormattedText null fallback active"
    )

Add-Check `
    "src\STS2Mobile\Patches\LanMultiplayerPatcher.Discovery.cs" `
    "starts LAN discovery with UDP broadcast socket and Godot poll timer" `
    @(
        "private sealed partial class LanDiscovery",
        "SocketOptionName\.ReuseAddress",
        "SocketOptionName\.Broadcast",
        "Bind\(new IPEndPoint\(IPAddress\.Any, BeaconPort\)\)",
        "new Thread\(ListenLoop\)",
        "Name = ""LanDiscovery""",
        "Connect\(""timeout"", Callable\.From\(PollHosts\)\)",
        "LAN discovery started"
    )

Add-Check `
    "src\STS2Mobile\Patches\LanMultiplayerPatcher.Discovery.Network.cs" `
    "listens for LAN beacon packets while ignoring local IPv4 addresses" `
    @(
        "ListenLoop",
        "Encoding\.UTF8\.GetString",
        "BeaconPrefix",
        "_localIps\.Contains\(ip\)",
        "NetworkInterface\.GetAllNetworkInterfaces",
        "AddressFamily\.InterNetwork",
        "LAN local IP enumeration failed"
    )

Add-Check `
    "src\STS2Mobile\Patches\LanMultiplayerPatcher.Discovery.Poll.cs" `
    "polls discovered LAN hosts and keeps join-screen visibility state current" `
    @(
        "PollHosts",
        "TotalSeconds > 6\.0",
        "AddHostButton",
        "_loadingIndicatorField",
        "_noFriendsLabelField",
        "LAN discovery visibility update failed",
        "UpdateScreenContext"
    )

Add-Check `
    "src\STS2Mobile\Patches\LanMultiplayerPatcher.Discovery.HostButton.cs" `
    "creates LAN join buttons that save the target IP and connect through JoinViaIp" `
    @(
        "AddHostButton",
        "_joinFriendButtonCreate\.Invoke",
        "SaveLastIp\(capturedIp\)",
        "JoinViaIp\(_screen, capturedIp, capturedPort\)",
        "Discovered LAN host"
    )

Add-Check `
    "src\STS2Mobile\Patches\LanMultiplayerPatcher.Discovery.Stop.cs" `
    "stops LAN discovery sockets, timers, buttons, and screen state" `
    @(
        "internal void Stop\(\)",
        "_udpClient\?\.Close\(\)",
        "_listenThread\?\.Join\(500\)",
        "_pollTimer\.QueueFree\(\)",
        "_hostButtons\.Clear\(\)",
        "_cleanupDiscoveryState",
        "LAN discovery stopped"
    )

Add-Check `
    "src\STS2Mobile\Startup\StartupPatchOrchestrator.Results.cs" `
    "keeps startup patch result shape used by runtime patch validation evidence" `
    @(
        "StartupPatchResult",
        "PatchGroupResult",
        "PatchAttempt",
        "CriticalFailed",
        "FailureMessages",
        "HasFailures",
        "FailedPatchCount"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherRuntimePatchValidationEvidence.cs" `
    "tracks runtime patch validation marker lifecycle" `
    @(
        "last_runtime_patch_validation\.json",
        "current_runtime_cache\.txt",
        "MarkerPath",
        "MarkerPresent",
        "Clear",
        "Failed to clear runtime patch validation evidence"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherRuntimePatchValidationEvidence.Write.cs" `
    "records actual runtime Harmony patch results for the selected runtime slot" `
    @(
        "StartupPatchOrchestrator\.StartupPatchResult",
        "critical_failed",
        "passed_with_noncritical_failures",
        "SteamGameBranch\.Normalize",
        "SteamGameInstallPaths\.VersionSlotKind",
        "GameRuntimeSlot\.Inspect",
        "Selected PCK SHA256:",
        "Selected source sts2\.dll SHA256:",
        "Active source sts2\.dll SHA256:",
        "selectedPckSha256",
        "selectedSourceAssemblySha256",
        "activeAndroidAssemblySha256",
        "runtimePackId",
        "runtimeSlotId",
        "runtimeCacheId",
        "failureMessages",
        "Failed to write runtime patch validation evidence"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherRuntimePatchValidationEvidence.RuntimeCache.cs" `
    "reads selected runtime cache values and classifies runtime pack status" `
    @(
        "RuntimeCacheValue",
        "RuntimeCacheMarkerFileName",
        "File\.ReadLines",
        "StringComparison\.OrdinalIgnoreCase",
        "RuntimePackStatus",
        "missing runtime pack assembly",
        "runtime pack selected"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherRuntimePatchValidationEvidence.Read.cs" `
    "reads runtime patch validation marker fields for diagnostics reports" `
    @(
        "UtcParseable",
        "SelectedBranch",
        "SelectedPckSha256",
        "SelectedSourceAssemblySha256",
        "ActiveAndroidAssemblySha256",
        "RuntimePackId",
        "RuntimePackStatus",
        "FailureMessages",
        "ReadString",
        "JsonValueKind\.Number"
    )

Add-Check `
    "src\STS2Mobile\ModEntry.cs" `
    "keeps the mobile patcher entrypoint split into focused partials" `
    @(
        "public static partial class ModEntry",
        "Bootstraps GodotSharp",
        "falls back to standalone launcher mode"
    )

Add-Check `
    "src\STS2Mobile\ModEntry.Constants.cs" `
    "centralizes bootstrap probes, startup fallback state, and runtime path constants" `
    @(
        "ApplyComplete = 2",
        "ApplyInProgress = 1",
        "ApplyNotStarted = 0",
        "BootstrapProbeCode = 1729",
        "HarmonyConstructorProbeCode = 1730",
        "LauncherBootstrapVariable = `"STS2_LAUNCHER_BOOTSTRAP`"",
        "MinimumPckHeaderLength = 96",
        "TempVariableNames",
        "HasStartupFallbackReason"
    )

Add-Check `
    "src\STS2Mobile\ModEntry.Exports.cs" `
    "keeps unmanaged bootstrap exports isolated from startup orchestration" `
    @(
        "InitializeGodotSharp",
        "ApplyFromGodot",
        "BootstrapProbe",
        "HarmonyConstructorProbe",
        "ShowLauncherOnly",
        "UnmanagedCallersOnly",
        "GodotSharpBootstrap\.Initialize",
        "new Harmony\(HarmonyId\)"
    )

Add-Check `
    "src\STS2Mobile\ModEntry.Apply.cs" `
    "writes runtime patch validation evidence immediately after startup patch orchestration" `
    @(
        "ApplyInternal",
        "InstallManagedExceptionHandlers",
        "TryBeginApply",
        "ApplyStartupPatches",
        "StartupPatchOrchestrator\.Apply\(harmony\)",
        "LauncherRuntimePatchValidationEvidence\.Write\(OS\.GetDataDir\(\), patchResult\)",
        "Critical startup patches failed",
        "ScheduleStandaloneLauncher",
        "CompleteApply"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherSaveOriginEvidence.cs" `
    "defines selected-runtime save-origin marker identity" `
    @(
        "internal static partial class LauncherSaveOriginEvidence",
        "current_android_save_origin\.txt",
        "MarkerPath",
        "MarkerPresent"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherSaveOriginEvidence.Read.cs" `
    "reads selected-runtime save-origin marker fields" `
    @(
        "OriginUtcParseable",
        "ReadMarkerValue",
        "LauncherMarkerFile\.ReadValue",
        "LauncherMarkerFile\.ReadUtc",
        "LauncherMarkerFile\.HasConcreteValue",
        "SelectedRuntimeSlotId",
        "Selected PCK SHA256",
        "Selected source sts2\.dll SHA256",
        "Selected runtime playable",
        "Selected runtime readiness problem",
        "Important Android local save evidence count"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherSaveOriginEvidence.RuntimeMatch.cs" `
    "matches selected-runtime save origin to selected branch and runtime" `
    @(
        "CurrentLocalSavesMatchSelectedBranch",
        "CurrentLocalSavesMatchSelectedRuntime",
        "RuntimeSlotIdMatchesSelectedRuntime",
        "PckMatchesSelectedRuntime",
        "SourceAssemblyMatchesSelectedRuntime",
        "SelectedRuntimeCurrentlyPlayable",
        "slot\.Playable",
        "SourcePckMatchesSelectedPck"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherSaveOriginEvidence.Write.cs" `
    "writes selected-runtime save origin and invalidates saves on branch switch" `
    @(
        "WriteManualPullOrigin",
        "WriteManualPushOrigin",
        "WriteBranchSwitchPendingOrigin",
        "Selected runtime slot ID",
        "Selected PCK SHA256",
        "Selected source sts2\.dll SHA256",
        "Selected runtime playable",
        "Selected runtime readiness problem",
        "Current Android local saves verified for selected branch: false",
        "Current Android local saves verified for selected runtime"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherCloudSaveState.cs" `
    "keeps cloud-sync state summary and launch-disable toggles centralized" `
    @(
        "StatusSummary",
        "HasSavedCredentials",
        "_cloudSyncEnabled",
        "_savedCredentials",
        "SetCloudSyncEnabled",
        "DisableCloudSyncForLaunch"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherCloudSaveState.Credentials.cs" `
    "captures persisted Steam credentials for cloud sync without app password storage" `
    @(
        "SavedSteamCredentials",
        "TryUseCredentials",
        "SaveCredentials\(string accountName, string refreshToken\)",
        "SavedSteamCredentials\.FromLogin",
        "_savedCredentials = null",
        "Saved Steam credentials available for cloud sync",
        "Saved Steam credentials unavailable for cloud sync",
        "ClearCredentials",
        "CloudSaveStoreFactory\.CreateCloudSaveStore"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherCloudSaveState.SaveManager.cs" `
    "uses cloud store only when credentials are available and falls back to Android local saves" `
    @(
        "TryCreateEnabledSaveManager",
        "TryGetSavedCredentialsForCloudSync",
        "TryCreateAndroidLocalSaveManager",
        "Cloud sync disabled - using Android local-only SaveManager when available",
        "No saved credentials - using Android local-only SaveManager when available",
        "CloudSaveStoreFactory\.CreateLocalOnlyCloudSaveStore",
        "Created Android local-only SaveManager"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherCloudSaveState.ManualSync.cs" `
    "requires saved credentials for manual cloud pull and push entry points" `
    @(
        "ManualPushAllAsync",
        "ManualPullAllAsync",
        "CloudSyncCoordinator\.ManualPushAllAsync",
        "CloudSyncCoordinator\.ManualPullAllAsync",
        "RequireSavedCredentials",
        "No saved Steam credentials\. Log in again before pulling cloud saves"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherController.CloudSync.PushSafety.Context.cs" `
    "shares selected branch context across Push safety gates" `
    @(
        "CloudPushSafetyContext",
        "LauncherPreferences\.ReadGameBranch\(\)",
        "SelectedBranch",
        "SelectedVersion",
        "WriteBlockedMarker"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherController.CloudSync.PushSafety.Baseline.cs" `
    "blocks baseline Push when save-origin evidence is missing or belongs to another selected runtime" `
    @(
        "CanPushWithBaselineEvidence",
        "LauncherSaveOriginEvidence\.CurrentLocalSavesMatchSelectedRuntime",
        "Manual Push blocked: Android local save origin evidence does not match the selected runtime"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherController.CloudSync.PushSafety.BranchSwitch.cs" `
    "blocks branch-switch Push when save-origin evidence belongs to another selected runtime" `
    @(
        "CanPushAfterBranchSwitch",
        "LauncherSaveOriginEvidence\.CurrentLocalSavesMatchSelectedRuntime",
        "Manual Push blocked: save-origin evidence is missing or belongs to a different selected runtime after branch switch"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherCloudSyncEvidence.Pull.cs" `
    "records save-origin evidence on Pull and includes it in baseline Push prerequisites" `
    @(
        "LauncherSaveOriginEvidence\.WriteManualPullOrigin",
        "BaselineManualPushPrerequisitesSatisfied",
        "LauncherSaveOriginEvidence\.CurrentLocalSavesMatchSelectedRuntime"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherCloudSyncEvidence.Push.Write.cs" `
    "records save-origin evidence on successful manual Push" `
    @(
        "LauncherSaveOriginEvidence\.WriteManualPushOrigin",
        "WriteManualPushMarker",
        "Manual Push completed after branch-switch safety gates"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherBranchSwitchSafety.Write.cs" `
    "marks selected-runtime save origin pending after branch switch" `
    @(
        "WriteMarker",
        "SteamGameBranch\.Normalize",
        "SteamGameInstallPaths\.VersionSlotDirectory",
        "LauncherSaveOriginEvidence\.WriteBranchSwitchPendingOrigin"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherBranchSwitchSafety.Gates.cs" `
    "requires save-origin evidence before branch-switch Push" `
    @(
        "HasRequiredEvidence",
        "LauncherCloudSyncEvidence\.HasManualPullAfterBranchSwitch",
        "LauncherLocalSaveEvidence\.HasImportantSaveEvidence",
        "LauncherSaveOriginEvidence\.CurrentLocalSavesMatchSelectedRuntime",
        "AppPaths\.HasStoragePermission",
        "ManualPushPrerequisitesSatisfied"
    )

Add-Check `
    "src\STS2Mobile\Steam\CloudSyncCoordinator.SavePathDiscovery.Fallback.cs" `
    "keeps Android fallback save path orchestration separate from profiles and enumeration" `
    @(
        "AddFallbackProfilePaths",
        "FallbackRootFiles",
        "FallbackProfiles\(\)",
        "AddEnumeratedSavePaths"
    )

Add-Check `
    "src\STS2Mobile\Steam\CloudSyncCoordinator.SavePathDiscovery.FallbackProfiles.cs" `
    "keeps fallback profile templates and history selection isolated" `
    @(
        "FallbackHistoryDirectories",
        "FallbackProfileFiles",
        "FallbackProfilePrefixes",
        "HistoryFileSelection",
        "SelectFallbackRunHistoryFiles",
        "LimitRunHistory\(SelectRunHistoryFiles",
        "ProfileIds\(\)"
    )

Add-Check `
    "src\STS2Mobile\Steam\CloudSyncCoordinator.SavePathDiscovery.Enumeration.cs" `
    "keeps bounded Android save path enumeration isolated and failure-tolerant" `
    @(
        "EnumeratedPathLimit",
        "EnumeratedDirectoryDepthLimit",
        "IgnoredEnumerationDirectories",
        "SafeGetFiles",
        "SafeGetDirectories",
        "IsDiscoveredSavePath",
        "ShouldSkipEnumeratedDirectory",
        "Save path enumeration failed, using fallback paths only"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherDiagnostics.ReportRuntimeSlot.cs" `
    "coordinates runtime slot, runtime pack, patch validation, cache, and pairing diagnostics" `
    @(
        "AppendGameRuntimeSlot",
        "GameRuntimeSlot\.Inspect\(dataDir, branch\)",
        "AppendRuntimeSlotSummary",
        "AppendRuntimeSlotSelectedFiles",
        "AppendRuntimePackEvidence",
        "AppendPatchCompatibilityEvidence",
        "AppendRuntimePatchValidationEvidence",
        "AppendRuntimeCacheEvidence",
        "AppendRuntimePairingSummary"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherDiagnostics.ReportRuntimeSlot.Slot.cs" `
    "surfaces selected runtime slot identity and selected file evidence" `
    @(
        "AppendRuntimeSlotSummary",
        "Runtime slot evidence marker present",
        "Runtime slot evidence selected branch matches current runtime",
        "Runtime slot evidence runtime slot ID matches current runtime",
        "Runtime slot evidence selected PCK matches current runtime",
        "Runtime slot evidence selected source sts2\.dll matches current runtime",
        "AppendRuntimeSlotSelectedFiles",
        "Selected runtime PCK SHA256",
        "Selected runtime release version",
        "Selected runtime depot manifest fingerprint",
        "Selected runtime identity summary",
        "Selected runtime slot ID",
        "Selected runtime source sts2\.dll SHA256",
        "Selected runtime active Android sts2\.dll SHA256"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherDiagnostics.ReportRuntimeSlot.RuntimePack.cs" `
    "surfaces selected runtime-pack manifest, hash, support assembly, and patch validation evidence" `
    @(
        "AppendRuntimePackEvidence",
        "Selected runtime pack status",
        "Selected runtime pack usability status",
        "Selected runtime pack usable",
        "Selected runtime pack source runtime slot ID",
        "Selected runtime pack source runtime slot ID matches selected runtime",
        "Selected runtime pack Android sts2\.dll SHA256",
        "Selected runtime pack Android sts2\.dll hash matches manifest",
        "Selected runtime pack patch validation status",
        "Selected runtime pack generated from clean directory",
        "Selected runtime pack support assemblies declared",
        "Selected runtime pack support assembly hashes declared",
        "Selected runtime pack missing symbol count"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherDiagnostics.ReportRuntimeSlot.PatchCompatibility.cs" `
    "surfaces selected patch compatibility and runtime patch validation marker evidence" `
    @(
        "AppendPatchCompatibilityEvidence",
        "Selected patch compatibility status",
        "Selected patch compatibility validation surface version",
        "Selected patch compatibility checked symbol count",
        "Selected patch compatibility validated PCK SHA256",
        "Selected patch compatibility source assembly matches selected",
        "AppendRuntimePatchValidationEvidence",
        "Runtime patch validation marker filename",
        "Runtime patch validation status",
        "Runtime patch validation selected PCK SHA256",
        "Runtime patch validation active Android sts2\.dll SHA256",
        "Runtime patch validation runtime pack status"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherDiagnostics.ReportRuntimeSlot.RuntimeCache.cs" `
    "surfaces selected runtime cache marker and selected-runtime match evidence" `
    @(
        "AppendRuntimeCacheEvidence",
        "Runtime cache marker filename",
        "Runtime cache marker package",
        "Runtime cache marker selected branch requires runtime pack",
        "Runtime cache marker runtime pack directory",
        "Runtime cache marker selected PCK SHA256",
        "Runtime cache marker selected source sts2\.dll SHA256",
        "Runtime cache marker publish cache active sts2\.dll SHA256",
        "Runtime cache marker selected PCK matches selected runtime",
        "Runtime cache prepared for selected runtime",
        "CachePreparedForSelectedRuntime\(dataDir, branch\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherDiagnostics.ReportRuntimeSlot.Pairing.cs" `
    "surfaces selected runtime pairing and final playability gate evidence" `
    @(
        "AppendRuntimePairingSummary",
        "Selected runtime pairing status",
        "Selected runtime requires usable runtime pack",
        "Selected runtime branch-matched Android runtime prepared",
        "Selected runtime compatible",
        "Selected runtime playable"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherDiagnostics.ReportBranchSwitchSafety.SaveOrigin.cs" `
    "surfaces save-origin evidence used by branch-switch Push safety" `
    @(
        "AppendSaveOriginEvidence",
        "Android save-origin marker present",
        "Android save-origin selected runtime slot ID matches current runtime",
        "Android save-origin current selected runtime is playable",
        "Android local saves verified for selected branch",
        "Android local saves verified for selected runtime",
        "Android save-origin selected PCK matches current runtime",
        "Android save-origin selected source sts2\.dll matches current runtime"
    )

Add-Check `
    "scripts\capture-multi-version-runtime-evidence.ps1" `
    "captures read-only multi-version runtime evidence for future branch updates" `
    @(
        "android-shell-utils\.ps1",
        "evidence-marker-utils\.ps1",
        "evidence-report-utils\.ps1",
        "ConvertTo-AndroidShellSingleQuoted",
        "ConvertTo-AndroidShellPathSingleQuoted",
        "multi-version-runtime",
        "runtime-marker-files\.txt",
        "runtime-marker-contents\.txt",
        "runtime-hashes\.txt",
        "current_runtime_slot\.json",
        "Installed runtime slot evidence",
        "Runtime slot marker matches selected files",
        "Read-DeviceSha256",
        "Read-DeviceRuntimePackDllNames",
        "Test-RuntimePackClosedDllSet",
        "RunLabel",
        "safeRunLabel",
        "Resolve-AndroidAdbPath",
        'multi-version-runtime-\$safeRunLabel-\$timestamp',
        "run-metadata\.json",
        "artifactFolderName",
        "Run label:",
        "Collector boundary: This collector is read-only and does not mutate Steam Cloud or app data",
        "closedDllSet",
        "runtimeSlotPckActualSha256",
        "runtimeSlotSourceAssemblyActualSha256",
        "stale-or-unreadable",
        "Installed slot matches runtime patch validation",
        "last_game_version_cache_cleanup\.txt",
        "last_game_version_redownload\.txt",
        "installedPck",
        "validatedPck",
        "installedSourceAssembly",
        "validatedSourceAssembly",
        "validation-report\.md",
        "Multi-version runtime validation report",
        "Mixed/split asset hypothesis matrix",
        "Status values: confirmed, ruled out, likely, unknown, needs device-only validation",
        "Steam branch partial/shared content",
        "Stale/incomplete downloader cache",
        "Wrong launch path",
        "Shared assembly/runtime cache",
        "In-process branch switch reuse",
        "Android PCK patch side effect",
        "Godot import/resource mismatch",
        "Save/config asset reference mismatch",
        "Prepared cache matches runtime",
        "Canonical slot bound to native cache identity",
        "Selected runtime pack manifest",
        "runtimePackValidation",
        "packClean",
        "packReportMatches",
        "runtimePackReportMatches",
        "runtimePackClosedDllSet",
        "generatedFromCleanDirectory",
        "blocked-no-usable-runtime",
        "missing-or-rejected",
        "requiresPack",
        "this should be treated as invalid",
        "selected_runtime_pack_compatibility\.json",
        "selected_runtime_pack_patch_validation\.json",
        "Steam Cloud Push save-origin safety",
        "save-origin runtime slot ID",
        "Selected runtime playable:",
        "Current Android local saves verified for selected runtime:",
        "runtimePlayable",
        "savesVerified",
        "last_runtime_patch_validation\.json",
        "current_runtime_slot\.json",
        "current_android_save_origin\.txt",
        "logcat-runtime-filtered\.txt",
        "This collector is read-only and does not mutate Steam Cloud or app data"
    )

Add-ForbiddenCheck `
    "scripts\capture-multi-version-runtime-evidence.ps1" `
    "keeps runtime evidence collection read-only" `
    @(
        "rm\s+-rf",
        "adb[^\r\n]+push",
        "adb[^\r\n]+install",
        "ManualPush",
        "WriteManualPush"
    )

Add-Check `
    "scripts\review-multi-version-runtime-evidence.ps1" `
    "reviews collected multi-version runtime evidence without mutating device or cloud state" `
    @(
        "RequirePublicBeta",
        "RequireBranchSwitch",
        "RequireSaveSafety",
        "RequireResolvedClassification",
        "Require-NoPattern",
        "run-metadata\.json",
        "metadata identifies collector",
        "metadata records read-only collector boundary",
        "metadata label",
        "last_game_branch_switch",
        "has branch-switch marker file evidence",
        "has branch-switch marker content evidence",
        "Run label:\\s\*public",
        "Run label:\\s\*public-beta",
        "Run label:\\s\*branch-switch",
        "validation-report\.md",
        "Mixed/split asset hypothesis matrix",
        "diagnostics/current_runtime_slot\.json",
        "diagnostics/current_runtime_cache\.txt",
        "diagnostics/selected_runtime_pack_compatibility\.json",
        "diagnostics/selected_runtime_pack_patch_validation\.json",
        "logs/logcat-runtime-filtered\.txt",
        "runtime pack validation report passed",
        "Steam Cloud Push save-origin safety",
        "does not carry unknown classifications into release signoff",
        "public-beta slot is selected"
    )

Add-ForbiddenCheck `
    "scripts\review-multi-version-runtime-evidence.ps1" `
    "keeps evidence review local and read-only" `
    @(
        "adb",
        "run-as",
        "rm\s+-rf",
        "Remove-Item",
        "Set-Content",
        "New-Item",
        "ManualPush",
        "WriteManualPush"
    )

Add-Check `
    "scripts\run-multi-version-runtime-release-gates.ps1" `
    "runs static multi-version release gates and optional local evidence review" `
    @(
        "audit-multi-version-runtime\.ps1",
        "audit-steam-version-selection\.ps1",
        "audit-steam-branch-guidance-parity\.ps1",
        "review-multi-version-runtime-evidence\.ps1",
        "PublicEvidenceDirs",
        "PublicBetaEvidenceDirs",
        "BranchSwitchEvidenceDirs",
        "RequirePublic",
        "RequirePublicBeta",
        "RequireBranchSwitch",
        "RequireSaveSafety",
        "RequireResolvedClassification",
        "Invoke-EvidenceReview",
        "EvidenceDirs"
    )

Add-ForbiddenCheck `
    "scripts\run-multi-version-runtime-release-gates.ps1" `
    "keeps release gate runner off device and cloud mutation paths" `
    @(
        "adb",
        "run-as",
        "rm\s+-rf",
        "Remove-Item",
        "Set-Content",
        "New-Item",
        "ManualPush",
        "WriteManualPush",
        "capture-multi-version-runtime-evidence\.ps1"
    )

Add-Check `
    "docs\multi-version-runtime-architecture.md" `
    "documents the implemented runtime-slot, pack, patch-validation, save-origin, and evidence workflow" `
    @(
        "GameRuntimeSlot",
        "runtime slot ID",
        "files/runtime_packs/<branch>/sts2\.dll",
        "compatibility\.json",
        "patch_validation\.json",
        "last_runtime_patch_validation\.json",
        "current_android_save_origin\.txt",
        "capture-multi-version-runtime-evidence\.ps1",
        "review-multi-version-runtime-evidence\.ps1",
        "run-multi-version-runtime-release-gates\.ps1",
        "RequireResolvedClassification",
        "validation-report\.md",
        "read-only"
    )

Add-Check `
    "docs\multi-version-runtime-release-gates.md" `
    "documents release signoff gates for public/beta runtime coexistence" `
    @(
        "Multi-version runtime release gates",
        "audit-multi-version-runtime\.ps1",
        "audit-steam-version-selection\.ps1",
        "audit-steam-branch-guidance-parity\.ps1",
        "ARM64 physical Android device",
        "branch switch public -> public-beta -> public -> public-beta",
        "capture-multi-version-runtime-evidence\.ps1",
        "review-multi-version-runtime-evidence\.ps1",
        "run-multi-version-runtime-release-gates\.ps1",
        "RunLabel public",
        "RunLabel public-beta",
        "RunLabel branch-switch",
        "RequirePublic",
        "RequireBranchSwitch",
        "RequireResolvedClassification",
        "current_runtime_slot\.json",
        "runtime pack closed DLL set passes",
        "active publish-cache.*sts2\.dll.*hash matches",
        "Steam Cloud Push must not be used during branch-runtime investigation",
        "Steam branch partial/shared content",
        "save/config asset reference mismatch",
        "public-beta can launch without a usable runtime pack",
        "branch switch reuses the previous branch runtime cache",
        "full runtime Harmony validation is still post-startup"
    )

Complete-StaticAudit `
    -FailureHeading "Multi-version runtime audit failed:" `
    -SuccessMessage "Multi-version runtime audit passed ({0} checks)." `
    -ThrowOnFailure
