function Add-MultiVersionRuntimeNativeCacheChecks {
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
}
