using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal sealed class RuntimePackManifest
{
    private const string AndroidAssemblyFileName = "sts2.dll";

    private RuntimePackManifest(
        string path,
        string expectedBranch,
        string packId,
        string sourceRuntimeSlotId,
        string sourceBranch,
        string sourcePckSha256,
        string sourceAssemblySha256,
        string androidAssemblySha256,
        string patchSetVersion,
        string patchValidationStatus,
        string patchValidationReport,
        string validationMode,
        string validationSurfaceVersion,
        string[] supportAssemblies,
        IReadOnlyDictionary<string, string> supportAssemblySha256,
        bool supportAssembliesDeclared,
        bool supportAssemblySha256Declared,
        int checkedSymbolCount,
        int presentSymbolCount,
        int missingSymbolCount,
        string minimumLauncherVersion,
        bool generatedFromCleanDirectory,
        string status,
        bool exists,
        bool readable,
        bool androidAssemblyExists,
        string androidAssemblyPath,
        string actualAndroidAssemblySha256
    )
    {
        Path = path;
        DirectoryPath = System.IO.Path.GetDirectoryName(path) ?? string.Empty;
        ExpectedBranch = expectedBranch;
        PackId = packId;
        SourceRuntimeSlotId = sourceRuntimeSlotId;
        SourceBranch = sourceBranch;
        SourcePckSha256 = sourcePckSha256;
        SourceAssemblySha256 = sourceAssemblySha256;
        AndroidAssemblySha256 = androidAssemblySha256;
        PatchSetVersion = patchSetVersion;
        PatchValidationStatus = patchValidationStatus;
        PatchValidationReport = patchValidationReport;
        ValidationMode = validationMode;
        ValidationSurfaceVersion = validationSurfaceVersion;
        SupportAssemblies = supportAssemblies ?? Array.Empty<string>();
        SupportAssemblySha256 = supportAssemblySha256 ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        SupportAssembliesDeclared = supportAssembliesDeclared;
        SupportAssemblySha256Declared = supportAssemblySha256Declared;
        CheckedSymbolCount = checkedSymbolCount;
        PresentSymbolCount = presentSymbolCount;
        MissingSymbolCount = missingSymbolCount;
        MinimumLauncherVersion = minimumLauncherVersion;
        GeneratedFromCleanDirectory = generatedFromCleanDirectory;
        Status = status;
        Exists = exists;
        Readable = readable;
        AndroidAssemblyExists = androidAssemblyExists;
        AndroidAssemblyPath = androidAssemblyPath;
        ActualAndroidAssemblySha256 = actualAndroidAssemblySha256;
    }

    internal string Path { get; }
    internal string DirectoryPath { get; }
    internal string ExpectedBranch { get; }
    internal string PackId { get; }
    internal string SourceRuntimeSlotId { get; }
    internal string SourceBranch { get; }
    internal string SourcePckSha256 { get; }
    internal string SourceAssemblySha256 { get; }
    internal string AndroidAssemblySha256 { get; }
    internal string PatchSetVersion { get; }
    internal string PatchValidationStatus { get; }
    internal string PatchValidationReport { get; }
    internal string ValidationMode { get; }
    internal string ValidationSurfaceVersion { get; }
    internal string[] SupportAssemblies { get; }
    internal IReadOnlyDictionary<string, string> SupportAssemblySha256 { get; }
    internal bool SupportAssembliesDeclared { get; }
    internal bool SupportAssemblySha256Declared { get; }
    internal int CheckedSymbolCount { get; }
    internal int PresentSymbolCount { get; }
    internal int MissingSymbolCount { get; }
    internal string MinimumLauncherVersion { get; }
    internal bool GeneratedFromCleanDirectory { get; }
    internal string Status { get; }
    internal bool Exists { get; }
    internal bool Readable { get; }
    internal bool AndroidAssemblyExists { get; }
    internal string AndroidAssemblyPath { get; }
    internal string ActualAndroidAssemblySha256 { get; }

    internal bool BranchMatches =>
        !string.IsNullOrWhiteSpace(SourceBranch)
        && string.Equals(
            SteamGameBranch.Normalize(SourceBranch),
            SteamGameBranch.Normalize(ExpectedBranch),
            StringComparison.OrdinalIgnoreCase
        );

    internal bool AndroidAssemblyHashMatches =>
        !string.IsNullOrWhiteSpace(AndroidAssemblySha256)
        && !string.IsNullOrWhiteSpace(ActualAndroidAssemblySha256)
        && !ActualAndroidAssemblySha256.StartsWith("<", StringComparison.Ordinal)
        && string.Equals(AndroidAssemblySha256, ActualAndroidAssemblySha256, StringComparison.OrdinalIgnoreCase);

    internal bool Usable =>
        string.Equals(Status, "usable", StringComparison.OrdinalIgnoreCase);

    internal bool PatchValidationPassed =>
        string.Equals(PatchValidationStatus, "passed", StringComparison.OrdinalIgnoreCase);

    internal static RuntimePackManifest Inspect(
        string path,
        string expectedBranch,
        string selectedPckSha256,
        string selectedSourceAssemblySha256
    )
    {
        expectedBranch = SteamGameBranch.Normalize(expectedBranch);
        var androidAssemblyPath = System.IO.Path.Combine(
            System.IO.Path.GetDirectoryName(path) ?? string.Empty,
            AndroidAssemblyFileName
        );
        var androidAssemblyExists = File.Exists(androidAssemblyPath);

        if (!File.Exists(path))
        {
            return new RuntimePackManifest(
                path,
                expectedBranch,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                Array.Empty<string>(),
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
                supportAssembliesDeclared: false,
                supportAssemblySha256Declared: false,
                0,
                0,
                0,
                string.Empty,
                generatedFromCleanDirectory: false,
                "not installed",
                exists: false,
                readable: false,
                androidAssemblyExists,
                androidAssemblyPath,
                androidAssemblyExists ? "<not inspected>" : "<missing>"
            );
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var root = document.RootElement;
            var declaredAndroidAssemblySha256 = ReadString(root, "androidAssemblySha256", "android_assembly_sha256", "sts2DllSha256", "sts2_dll_sha256");
            var manifest = new RuntimePackManifest(
                path,
                expectedBranch,
                ReadString(root, "packId", "pack_id", "id"),
                ReadString(root, "sourceRuntimeSlotId", "source_runtime_slot_id", "runtimeSlotId", "runtime_slot_id"),
                ReadString(root, "sourceBranch", "source_branch", "branch"),
                ReadString(root, "sourcePckSha256", "source_pck_sha256", "pckSha256", "pck_sha256"),
                ReadString(root, "sourceAssemblySha256", "source_assembly_sha256", "desktopAssemblySha256", "desktop_assembly_sha256"),
                declaredAndroidAssemblySha256,
                ReadString(root, "patchSetVersion", "patch_set_version", "patchVersion", "patch_version"),
                ReadString(root, "patchValidationStatus", "patch_validation_status", "patchStatus", "patch_status"),
                ReadString(root, "patchValidationReport", "patch_validation_report", "patchReport", "patch_report"),
                ReadString(root, "validationMode", "validation_mode"),
                ReadString(root, "validationSurfaceVersion", "validation_surface_version"),
                ReadStringArray(root, "supportAssemblies", "support_assemblies"),
                ReadStringDictionary(root, "supportAssemblySha256", "support_assembly_sha256"),
                HasProperty(root, "supportAssemblies", "support_assemblies"),
                HasProperty(root, "supportAssemblySha256", "support_assembly_sha256"),
                ReadInt(root, "checkedSymbolCount", "checked_symbol_count"),
                ReadInt(root, "presentSymbolCount", "present_symbol_count"),
                ReadInt(root, "missingSymbolCount", "missing_symbol_count"),
                ReadString(root, "minimumLauncherVersion", "minimum_launcher_version", "minLauncherVersion"),
                ReadBool(root, "generatedFromCleanDirectory", "generated_from_clean_directory"),
                "pending validation",
                exists: true,
                readable: true,
                androidAssemblyExists,
                androidAssemblyPath,
                androidAssemblyExists ? declaredAndroidAssemblySha256 : "<missing>"
            );

            return manifest.WithStatus(RuntimePackStatus(manifest, selectedPckSha256, selectedSourceAssemblySha256));
        }
        catch (Exception ex)
        {
            return new RuntimePackManifest(
                path,
                expectedBranch,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                Array.Empty<string>(),
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
                supportAssembliesDeclared: false,
                supportAssemblySha256Declared: false,
                0,
                0,
                0,
                string.Empty,
                generatedFromCleanDirectory: false,
                $"unreadable: {ex.GetType().Name}",
                exists: true,
                readable: false,
                androidAssemblyExists,
                androidAssemblyPath,
                androidAssemblyExists ? "<not inspected>" : "<missing>"
            );
        }
    }

    private RuntimePackManifest WithStatus(string status)
        => new(
            Path,
            ExpectedBranch,
            PackId,
            SourceRuntimeSlotId,
            SourceBranch,
            SourcePckSha256,
            SourceAssemblySha256,
            AndroidAssemblySha256,
            PatchSetVersion,
            PatchValidationStatus,
            PatchValidationReport,
            ValidationMode,
            ValidationSurfaceVersion,
            SupportAssemblies,
            SupportAssemblySha256,
            SupportAssembliesDeclared,
            SupportAssemblySha256Declared,
            CheckedSymbolCount,
            PresentSymbolCount,
            MissingSymbolCount,
            MinimumLauncherVersion,
            GeneratedFromCleanDirectory,
            status,
            Exists,
            Readable,
            AndroidAssemblyExists,
            AndroidAssemblyPath,
            ActualAndroidAssemblySha256
        );

    private static string RuntimePackStatus(RuntimePackManifest manifest, string selectedPckSha256, string selectedSourceAssemblySha256)
    {
        if (!manifest.Exists)
            return "not installed";
        if (!manifest.Readable)
            return manifest.Status;
        if (!manifest.BranchMatches)
            return "branch mismatch";
        if (string.IsNullOrWhiteSpace(manifest.PackId))
            return "missing runtime pack ID";
        if (!manifest.GeneratedFromCleanDirectory)
            return "runtime pack was not generated from a clean directory";
        if (string.IsNullOrWhiteSpace(manifest.SourceRuntimeSlotId))
            return "missing source runtime slot ID";
        if (!manifest.AndroidAssemblyExists)
            return "missing Android sts2.dll";
        if (string.IsNullOrWhiteSpace(manifest.AndroidAssemblySha256))
            return "missing Android assembly hash";
        if (!manifest.AndroidAssemblyHashMatches)
            return "Android sts2.dll hash mismatch";
        if (!manifest.SupportAssembliesDeclared)
            return "missing runtime pack support assembly declaration";
        if (!manifest.SupportAssemblySha256Declared)
            return "missing runtime pack support assembly hashes";
        var supportAssemblyProblem = RuntimePackSupportAssemblyProblem(manifest);
        if (!string.IsNullOrWhiteSpace(supportAssemblyProblem))
            return supportAssemblyProblem;
        if (string.IsNullOrWhiteSpace(manifest.SourcePckSha256))
            return "missing source PCK hash";
        if (!MatchesDeclared(manifest.SourcePckSha256, selectedPckSha256))
            return "PCK hash mismatch";
        if (string.IsNullOrWhiteSpace(manifest.SourceAssemblySha256))
            return "missing source assembly hash";
        if (!MatchesDeclared(manifest.SourceAssemblySha256, selectedSourceAssemblySha256))
            return "source assembly hash mismatch";
        if (string.IsNullOrWhiteSpace(manifest.PatchValidationStatus))
            return "missing patch validation status";
        if (!manifest.PatchValidationPassed)
            return $"patch validation not passed: {manifest.PatchValidationStatus}";
        if (string.IsNullOrWhiteSpace(manifest.PatchValidationReport))
            return "missing patch validation report";
        var reportPath = System.IO.Path.Combine(manifest.DirectoryPath, manifest.PatchValidationReport);
        if (!File.Exists(reportPath))
            return "missing patch validation report file";
        if (!PatchValidationReportMatches(reportPath, manifest))
            return "patch validation report mismatch";
        return "usable";
    }

    private static bool PatchValidationReportMatches(string reportPath, RuntimePackManifest manifest)
    {
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(reportPath));
            var root = document.RootElement;
            if (!StringPropertyMatches(root, "status", "passed"))
                return false;
            return StringPropertyMatches(root, "runtimePackId", manifest.PackId)
                && StringPropertyMatches(root, "sourceRuntimeSlotId", manifest.SourceRuntimeSlotId)
                && StringPropertyMatches(root, "branch", manifest.SourceBranch)
                && StringPropertyMatches(root, "pckSha256", manifest.SourcePckSha256)
                && StringPropertyMatches(root, "sourceAssemblySha256", manifest.SourceAssemblySha256)
                && StringPropertyMatches(root, "androidAssemblySha256", manifest.AndroidAssemblySha256)
                && StringPropertyMatches(root, "patchSetVersion", manifest.PatchSetVersion)
                && StringPropertyMatches(root, "validationSurfaceVersion", manifest.ValidationSurfaceVersion)
                && StringArrayPropertyMatches(root, "supportAssemblies", manifest.SupportAssemblies)
                && StringDictionaryPropertyMatches(root, "supportAssemblySha256", manifest.SupportAssemblySha256)
                && BoolPropertyMatches(root, "generatedFromCleanDirectory", manifest.GeneratedFromCleanDirectory);
        }
        catch
        {
            return false;
        }
    }

    private static bool StringPropertyMatches(JsonElement root, string property, string expected)
        => !string.IsNullOrWhiteSpace(expected)
        && root.TryGetProperty(property, out var value)
        && value.ValueKind == JsonValueKind.String
        && string.Equals(value.GetString(), expected, StringComparison.OrdinalIgnoreCase);

    private static bool BoolPropertyMatches(JsonElement root, string property, bool expected)
        => root.TryGetProperty(property, out var value)
        && (value.ValueKind == JsonValueKind.True) == expected;

    private static string RuntimePackSupportAssemblyProblem(RuntimePackManifest manifest)
    {
        var declared = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { AndroidAssemblyFileName };
        var declaredSupportAssemblies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var supportAssembly in manifest.SupportAssemblies)
        {
            if (string.IsNullOrWhiteSpace(supportAssembly))
                return "runtime pack declares a blank support assembly";
            if (supportAssembly.IndexOfAny(new[] { '/', '\\' }) >= 0 || !string.Equals(System.IO.Path.GetFileName(supportAssembly), supportAssembly, StringComparison.Ordinal))
                return $"runtime pack support assembly has unsafe name: {supportAssembly}";
            if (!supportAssembly.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                return $"runtime pack support assembly is not a DLL: {supportAssembly}";
            if (string.Equals(supportAssembly, AndroidAssemblyFileName, StringComparison.OrdinalIgnoreCase))
                return "runtime pack support assemblies must not redeclare sts2.dll";
            if (!declared.Add(supportAssembly))
                return $"runtime pack support assembly is duplicated: {supportAssembly}";
            declaredSupportAssemblies.Add(supportAssembly);

            var supportPath = System.IO.Path.Combine(manifest.DirectoryPath, supportAssembly);
            if (!File.Exists(supportPath))
                return $"runtime pack support assembly missing: {supportAssembly}";
            if (!manifest.SupportAssemblySha256.TryGetValue(supportAssembly, out var declaredSha256) || string.IsNullOrWhiteSpace(declaredSha256))
                return $"runtime pack support assembly hash missing: {supportAssembly}";
        }

        foreach (var supportHash in manifest.SupportAssemblySha256.Keys)
        {
            if (!declaredSupportAssemblies.Contains(supportHash))
                return $"runtime pack support assembly hash is undeclared: {supportHash}";
        }

        if (Directory.Exists(manifest.DirectoryPath))
        {
            foreach (var dll in Directory.EnumerateFiles(manifest.DirectoryPath, "*.dll", SearchOption.TopDirectoryOnly))
            {
                var fileName = System.IO.Path.GetFileName(dll);
                if (!declared.Contains(fileName))
                    return $"runtime pack contains undeclared DLL: {fileName}";
            }
        }

        return string.Empty;
    }

    private static bool StringArrayPropertyMatches(JsonElement root, string property, IReadOnlyList<string> expected)
    {
        if (!root.TryGetProperty(property, out var value) || value.ValueKind != JsonValueKind.Array)
            return expected == null || expected.Count == 0;

        var actual = value.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => item.GetString() ?? string.Empty)
            .ToArray();
        expected ??= Array.Empty<string>();
        return actual.Length == expected.Count
            && actual.Zip(expected, (left, right) => string.Equals(left, right, StringComparison.OrdinalIgnoreCase)).All(matches => matches);
    }

    private static bool StringDictionaryPropertyMatches(JsonElement root, string property, IReadOnlyDictionary<string, string> expected)
    {
        expected ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!root.TryGetProperty(property, out var value) || value.ValueKind != JsonValueKind.Object)
            return expected.Count == 0;

        var actual = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in value.EnumerateObject())
        {
            if (item.Value.ValueKind != JsonValueKind.String)
                return false;
            actual[item.Name] = item.Value.GetString() ?? string.Empty;
        }

        return actual.Count == expected.Count
            && expected.All(pair => actual.TryGetValue(pair.Key, out var actualValue)
                && string.Equals(actualValue, pair.Value, StringComparison.OrdinalIgnoreCase));
    }

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

    private static string[] ReadStringArray(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Array)
                return value.EnumerateArray()
                    .Where(item => item.ValueKind == JsonValueKind.String)
                    .Select(item => item.GetString() ?? string.Empty)
                    .ToArray();
        }

        return Array.Empty<string>();
    }

    private static bool HasProperty(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (root.TryGetProperty(name, out _))
                return true;
        }

        return false;
    }

    private static IReadOnlyDictionary<string, string> ReadStringDictionary(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (!root.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.Object)
                continue;

            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in value.EnumerateObject())
            {
                if (item.Value.ValueKind == JsonValueKind.String)
                    result[item.Name] = item.Value.GetString() ?? string.Empty;
            }
            return result;
        }

        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    private static bool ReadBool(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (root.TryGetProperty(name, out var value))
            {
                if (value.ValueKind == JsonValueKind.True)
                    return true;
                if (value.ValueKind == JsonValueKind.False)
                    return false;
            }
        }

        return false;
    }

}
