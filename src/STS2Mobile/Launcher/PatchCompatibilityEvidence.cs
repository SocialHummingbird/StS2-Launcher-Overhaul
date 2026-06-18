using System;
using System.IO;
using System.Text.Json;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal sealed class PatchCompatibilityEvidence
{
    internal const string GameDirectoryMarkerFileName = ".android_patch_validation.json";
    private const string RuntimePackReportFileName = "patch_validation.json";
    private const string PassedStatus = "passed";

    private PatchCompatibilityEvidence(
        string branch,
        string source,
        string markerPath,
        string status,
        string detail,
        string validatedBranch,
        string validatedPckSha256,
        string validatedSourceAssemblySha256,
        string patchSetVersion,
        string validationMode,
        string validationSurfaceVersion,
        int requiredSymbolCount,
        int checkedSymbolCount,
        int presentSymbolCount,
        int missingSymbolCount,
        bool required,
        bool exists,
        bool readable,
        bool branchMatches,
        bool pckMatches,
        bool sourceAssemblyMatches
    )
    {
        Branch = branch;
        Source = source;
        MarkerPath = markerPath;
        Status = status;
        Detail = detail;
        ValidatedBranch = validatedBranch;
        ValidatedPckSha256 = validatedPckSha256;
        ValidatedSourceAssemblySha256 = validatedSourceAssemblySha256;
        PatchSetVersion = patchSetVersion;
        ValidationMode = validationMode;
        ValidationSurfaceVersion = validationSurfaceVersion;
        RequiredSymbolCount = requiredSymbolCount;
        CheckedSymbolCount = checkedSymbolCount;
        PresentSymbolCount = presentSymbolCount;
        MissingSymbolCount = missingSymbolCount;
        Required = required;
        Exists = exists;
        Readable = readable;
        BranchMatches = branchMatches;
        PckMatches = pckMatches;
        SourceAssemblyMatches = sourceAssemblyMatches;
    }

    internal string Branch { get; }
    internal string Source { get; }
    internal string MarkerPath { get; }
    internal string Status { get; }
    internal string Detail { get; }
    internal string ValidatedBranch { get; }
    internal string ValidatedPckSha256 { get; }
    internal string ValidatedSourceAssemblySha256 { get; }
    internal string PatchSetVersion { get; }
    internal string ValidationMode { get; }
    internal string ValidationSurfaceVersion { get; }
    internal int RequiredSymbolCount { get; }
    internal int CheckedSymbolCount { get; }
    internal int PresentSymbolCount { get; }
    internal int MissingSymbolCount { get; }
    internal bool Required { get; }
    internal bool Exists { get; }
    internal bool Readable { get; }
    internal bool BranchMatches { get; }
    internal bool PckMatches { get; }
    internal bool SourceAssemblyMatches { get; }

    internal bool Passed =>
        string.Equals(Status, PassedStatus, StringComparison.OrdinalIgnoreCase)
        && BranchMatches
        && PckMatches
        && SourceAssemblyMatches;

    internal string Problem
    {
        get
        {
            if (Passed)
                return null;
            if (!Required)
                return null;
            if (!Exists)
                return "Selected game version has no Android patch compatibility validation evidence.";
            if (!Readable)
                return $"Selected game version has unreadable Android patch validation evidence ({Detail}).";
            if (!BranchMatches)
                return "Selected game version has Android patch validation evidence for a different Steam branch.";
            if (string.IsNullOrWhiteSpace(ValidatedPckSha256))
                return "Selected game version has Android patch validation evidence that does not declare the validated PCK.";
            if (!PckMatches)
                return "Selected game version has Android patch validation evidence for a different PCK.";
            if (string.IsNullOrWhiteSpace(ValidatedSourceAssemblySha256))
                return "Selected game version has Android patch validation evidence that does not declare the validated game-code assembly.";
            if (!SourceAssemblyMatches)
                return "Selected game version has Android patch validation evidence for a different game-code assembly.";

            return $"Selected game version failed Android patch compatibility validation ({Status}).";
        }
    }

    internal static PatchCompatibilityEvidence Inspect(
        string dataDir,
        string branch,
        string gameDirectory,
        string selectedPckSha256,
        string selectedSourceAssemblySha256,
        RuntimePackManifest runtimePack,
        bool runtimePackSlotIdMatches = true
    )
    {
        branch = SteamGameBranch.Normalize(branch);
        if (string.Equals(branch, SteamGameBranch.Public, StringComparison.OrdinalIgnoreCase))
        {
            return new PatchCompatibilityEvidence(
                branch,
                "legacy public APK baseline",
                string.Empty,
                PassedStatus,
                "public branch uses the APK-bundled patch compatibility baseline",
                branch,
                selectedPckSha256,
                selectedSourceAssemblySha256,
                runtimePack?.PatchSetVersion ?? string.Empty,
                "legacy-public-baseline",
                "legacy-public-baseline",
                0,
                0,
                0,
                0,
                required: false,
                exists: true,
                readable: true,
                branchMatches: true,
                pckMatches: true,
                sourceAssemblyMatches: true
            );
        }

        if (runtimePack?.Usable == true && runtimePack.PatchValidationPassed && runtimePackSlotIdMatches)
        {
            return new PatchCompatibilityEvidence(
                branch,
                "runtime pack manifest",
                runtimePack.Path,
                runtimePack.PatchValidationStatus,
                "runtime pack declares passed Android patch compatibility validation",
                runtimePack.SourceBranch,
                runtimePack.SourcePckSha256,
                runtimePack.SourceAssemblySha256,
                runtimePack.PatchSetVersion,
                runtimePack.ValidationMode,
                runtimePack.ValidationSurfaceVersion,
                runtimePack.CheckedSymbolCount,
                runtimePack.CheckedSymbolCount,
                runtimePack.PresentSymbolCount,
                runtimePack.MissingSymbolCount,
                required: true,
                exists: runtimePack.Exists,
                readable: runtimePack.Readable,
                branchMatches: runtimePack.BranchMatches,
                pckMatches: MatchesDeclared(runtimePack.SourcePckSha256, selectedPckSha256),
                sourceAssemblyMatches: MatchesDeclared(runtimePack.SourceAssemblySha256, selectedSourceAssemblySha256)
            );
        }

        var runtimePackReportPath = string.IsNullOrWhiteSpace(runtimePack?.DirectoryPath) || !runtimePackSlotIdMatches
            ? string.Empty
            : Path.Combine(runtimePack.DirectoryPath, RuntimePackReportFileName);
        var runtimePackReport = ReadValidationMarker(
            runtimePackReportPath,
            branch,
            selectedPckSha256,
            selectedSourceAssemblySha256,
            "runtime pack validation report"
        );
        if (runtimePackReport.Exists)
            return runtimePackReport;

        return ReadValidationMarker(
            Path.Combine(gameDirectory ?? string.Empty, GameDirectoryMarkerFileName),
            branch,
            selectedPckSha256,
            selectedSourceAssemblySha256,
            "selected game directory validation marker"
        );
    }

    private static PatchCompatibilityEvidence ReadValidationMarker(
        string path,
        string branch,
        string selectedPckSha256,
        string selectedSourceAssemblySha256,
        string source
    )
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return new PatchCompatibilityEvidence(
                branch,
                source,
                path,
                "missing",
                "validation evidence not found",
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                0,
                0,
                0,
                0,
                required: true,
                exists: false,
                readable: false,
                branchMatches: false,
                pckMatches: false,
                sourceAssemblyMatches: false
            );
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var root = document.RootElement;
            var status = ReadString(root, "status", "result", "patchValidationStatus", "patch_validation_status");
            var validatedBranch = ReadString(root, "branch", "sourceBranch", "source_branch", "validatedBranch", "validated_branch");
            var validatedPckSha256 = ReadString(root, "pckSha256", "pck_sha256", "sourcePckSha256", "source_pck_sha256");
            var validatedSourceAssemblySha256 = ReadString(root, "sourceAssemblySha256", "source_assembly_sha256", "desktopAssemblySha256", "desktop_assembly_sha256");
            var patchSetVersion = ReadString(root, "patchSetVersion", "patch_set_version", "patchVersion", "patch_version");
            var validationMode = ReadString(root, "validationMode", "validation_mode");
            var validationSurfaceVersion = ReadString(root, "validationSurfaceVersion", "validation_surface_version");
            return new PatchCompatibilityEvidence(
                branch,
                source,
                path,
                string.IsNullOrWhiteSpace(status) ? "unknown" : status,
                ReadString(root, "detail", "message", "summary"),
                validatedBranch,
                validatedPckSha256,
                validatedSourceAssemblySha256,
                patchSetVersion,
                validationMode,
                validationSurfaceVersion,
                ReadInt(root, "requiredSymbolCount", "required_symbol_count"),
                ReadInt(root, "checkedSymbolCount", "checked_symbol_count"),
                ReadInt(root, "presentSymbolCount", "present_symbol_count"),
                ReadInt(root, "missingSymbolCount", "missing_symbol_count"),
                required: true,
                exists: true,
                readable: true,
                branchMatches: MatchesBranch(validatedBranch, branch),
                pckMatches: MatchesDeclared(validatedPckSha256, selectedPckSha256),
                sourceAssemblyMatches: MatchesDeclared(validatedSourceAssemblySha256, selectedSourceAssemblySha256)
            );
        }
        catch (Exception ex)
        {
            return new PatchCompatibilityEvidence(
                branch,
                source,
                path,
                "unreadable",
                ex.GetType().Name,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                0,
                0,
                0,
                0,
                required: true,
                exists: true,
                readable: false,
                branchMatches: false,
                pckMatches: false,
                sourceAssemblyMatches: false
            );
        }
    }

    private static bool MatchesBranch(string declaredBranch, string expectedBranch)
        => !string.IsNullOrWhiteSpace(declaredBranch)
        && string.Equals(
            SteamGameBranch.Normalize(declaredBranch),
            SteamGameBranch.Normalize(expectedBranch),
            StringComparison.OrdinalIgnoreCase
        );

    private static bool MatchesDeclared(string declaredValue, string actualValue)
        => !string.IsNullOrWhiteSpace(declaredValue)
        && !string.IsNullOrWhiteSpace(actualValue)
        && !actualValue.StartsWith("<", StringComparison.Ordinal)
        && string.Equals(declaredValue, actualValue, StringComparison.OrdinalIgnoreCase);

    private static string ReadString(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
                return value.GetString() ?? string.Empty;
        }

        return string.Empty;
    }

    private static int ReadInt(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (!root.TryGetProperty(name, out var value))
                continue;

            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var intValue))
                return intValue;

            if (value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), out intValue))
                return intValue;
        }

        return 0;
    }
}
