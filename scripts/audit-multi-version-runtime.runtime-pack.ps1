function Add-MultiVersionRuntimePackChecks {
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
}
