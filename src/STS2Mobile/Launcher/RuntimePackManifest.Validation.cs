using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace STS2Mobile.Launcher;
internal sealed partial class RuntimePackManifest
{
    private RuntimePackManifest WithStatus(string status) => new(Path, ExpectedBranch, ExpectedGameIdentity, PackId, SourceBranch, InstallGeneration, SourcePckSha256, SourceAssemblySha256, GameIdentityId, AndroidAssemblySha256, PatchSetVersion, PatchValidationStatus, PatchValidationReport, ValidationMode, ValidationSurfaceVersion, SupportAssemblies, SupportAssemblySha256, SupportAssembliesDeclared, SupportAssemblySha256Declared, CheckedSymbolCount, PresentSymbolCount, MissingSymbolCount, MinimumLauncherVersion, GeneratedFromCleanDirectory, status, Exists, Readable, AndroidAssemblyExists, AndroidAssemblyPath, ActualAndroidAssemblySha256);
    private static string RuntimePackStatus(RuntimePackManifest manifest)
    {
        if (!manifest.Exists)
            return "not installed";
        if (!manifest.Readable)
            return manifest.Status;
        if (manifest.SourceGameIdentity == null)
            return "missing or invalid source game identity";
        if (manifest.ExpectedGameIdentity == null)
            return "current game identity unavailable";
        if (!string.Equals(manifest.GameIdentityId, manifest.SourceGameIdentity.Id, StringComparison.OrdinalIgnoreCase))
            return "game identity ID mismatch";
        if (manifest.SourceGameIdentity != manifest.ExpectedGameIdentity)
            return "game identity mismatch";
        if (string.IsNullOrWhiteSpace(manifest.PackId))
            return "missing runtime pack ID";
        if (!string.Equals(manifest.PatchSetVersion, PatchCompatibilityValidator.PatchSetVersion, StringComparison.OrdinalIgnoreCase))
            return $"runtime pack patch-set mismatch: {manifest.PatchSetVersion}";
        if (!manifest.GeneratedFromCleanDirectory)
            return "runtime pack was not generated from a clean directory";
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

    private static bool StringPropertyMatches(JsonElement root, string property, string expected) => !string.IsNullOrWhiteSpace(expected) && root.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String && string.Equals(value.GetString(), expected, StringComparison.OrdinalIgnoreCase);
    private static bool BoolPropertyMatches(JsonElement root, string property, bool expected) => root.TryGetProperty(property, out var value) && (value.ValueKind == JsonValueKind.True) == expected;
    private static bool StringArrayPropertyMatches(JsonElement root, string property, IReadOnlyList<string> expected)
    {
        if (!root.TryGetProperty(property, out var value) || value.ValueKind != JsonValueKind.Array)
            return expected == null || expected.Count == 0;
        var actual = value.EnumerateArray().Where(item => item.ValueKind == JsonValueKind.String).Select(item => item.GetString() ?? string.Empty).ToArray();
        expected ??= Array.Empty<string>();
        return actual.Length == expected.Count && actual.Zip(expected, (left, right) => string.Equals(left, right, StringComparison.OrdinalIgnoreCase)).All(matches => matches);
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

        return actual.Count == expected.Count && expected.All(pair => actual.TryGetValue(pair.Key, out var actualValue) && string.Equals(actualValue, pair.Value, StringComparison.OrdinalIgnoreCase));
    }

    private static bool PatchValidationReportMatches(string reportPath, RuntimePackManifest manifest)
    {
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(reportPath));
            var root = document.RootElement;
            if (!StringPropertyMatches(root, "status", "passed"))
                return false;
            return StringPropertyMatches(root, "runtimePackId", manifest.PackId) && StringPropertyMatches(root, "branch", manifest.SourceBranch) && StringPropertyMatches(root, "installGeneration", manifest.InstallGeneration) && StringPropertyMatches(root, "gameIdentityId", manifest.GameIdentityId) && StringPropertyMatches(root, "pckSha256", manifest.SourcePckSha256) && StringPropertyMatches(root, "sourceAssemblySha256", manifest.SourceAssemblySha256) && StringPropertyMatches(root, "androidAssemblySha256", manifest.AndroidAssemblySha256) && StringPropertyMatches(root, "patchSetVersion", manifest.PatchSetVersion) && StringPropertyMatches(root, "validationSurfaceVersion", manifest.ValidationSurfaceVersion) && StringArrayPropertyMatches(root, "supportAssemblies", manifest.SupportAssemblies) && StringDictionaryPropertyMatches(root, "supportAssemblySha256", manifest.SupportAssemblySha256) && BoolPropertyMatches(root, "generatedFromCleanDirectory", manifest.GeneratedFromCleanDirectory);
        }
        catch
        {
            return false;
        }
    }

    private static string RuntimePackSupportAssemblyProblem(RuntimePackManifest manifest)
    {
        var declared = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            AndroidAssemblyFileName
        };
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
}
