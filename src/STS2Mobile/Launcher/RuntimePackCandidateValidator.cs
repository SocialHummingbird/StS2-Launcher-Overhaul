using System;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace STS2Mobile.Launcher;

internal static class RuntimePackCandidateValidator
{
    private const int CandidateSchemaVersion = 3;
    private const string CompatibilityManifestFileName = "compatibility.json";
    private const string PatchValidationReportFileName = "patch_validation.json";

    internal static RuntimePackCandidateValidationResult Validate(
        string stagingDirectory,
        GameIdentity expectedIdentity,
        string expectedPatchSetVersion = null,
        string expectedValidationMode = null,
        string expectedValidationSurfaceVersion = null
    )
    {
        if (expectedIdentity == null)
            return RuntimePackCandidateValidationResult.Rejected(
                "Candidate validation requires an authoritative GameIdentity."
            );
        if (string.IsNullOrWhiteSpace(stagingDirectory))
            return RuntimePackCandidateValidationResult.Rejected(
                "Candidate staging directory is missing."
            );

        try
        {
            var fullDirectory = Path.GetFullPath(stagingDirectory);
            if (!Directory.Exists(fullDirectory))
            {
                return RuntimePackCandidateValidationResult.Rejected(
                    $"Candidate staging directory does not exist: {fullDirectory}."
                );
            }

            var manifestPath = Path.Combine(
                fullDirectory,
                CompatibilityManifestFileName
            );
            var reportPath = Path.Combine(
                fullDirectory,
                PatchValidationReportFileName
            );
            if (!File.Exists(manifestPath))
            {
                return RuntimePackCandidateValidationResult.Rejected(
                    "Candidate compatibility manifest is missing."
                );
            }
            if (!File.Exists(reportPath))
            {
                return RuntimePackCandidateValidationResult.Rejected(
                    "Candidate patch validation report is missing."
                );
            }

            using var manifestDocument = JsonDocument.Parse(File.ReadAllText(manifestPath));
            using var reportDocument = JsonDocument.Parse(File.ReadAllText(reportPath));
            var manifestRoot = manifestDocument.RootElement;
            var reportRoot = reportDocument.RootElement;
            RequireObject(manifestRoot, "compatibility manifest");
            RequireObject(reportRoot, "patch validation report");
            RequireSchema(manifestRoot, "compatibility manifest");
            RequireSchema(reportRoot, "patch validation report");
            RequireDeclaredIdentity(
                manifestRoot,
                expectedIdentity,
                "compatibility manifest"
            );
            RequireDeclaredIdentity(
                reportRoot,
                expectedIdentity,
                "patch validation report"
            );

            var manifest = RuntimePackManifest.Inspect(manifestPath, expectedIdentity);
            if (!manifest.Usable)
            {
                return RuntimePackCandidateValidationResult.Rejected(
                    $"Candidate runtime-pack manifest is not usable: {manifest.Status}."
                );
            }

            RequireExactString(
                manifestRoot,
                "patchValidationReport",
                PatchValidationReportFileName,
                "compatibility manifest"
            );
            RequireExactString(
                manifestRoot,
                "androidAssemblyFile",
                RuntimePackManifest.AndroidAssemblyFileName,
                "compatibility manifest"
            );
            RequireOptionalExpected(
                manifest.PatchSetVersion,
                expectedPatchSetVersion,
                "patch-set version"
            );
            RequireOptionalExpected(
                manifest.ValidationMode,
                expectedValidationMode,
                "validation mode"
            );
            RequireOptionalExpected(
                manifest.ValidationSurfaceVersion,
                expectedValidationSurfaceVersion,
                "validation-surface version"
            );

            var expectedPackId = RuntimePackWriter.RuntimePackId(
                expectedIdentity,
                manifest.PatchSetVersion,
                manifest.ValidationSurfaceVersion,
                manifest.AndroidAssemblySha256,
                manifest.SupportAssemblySha256
            );
            if (!string.Equals(manifest.PackId, expectedPackId, StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    $"Candidate pack ID is inconsistent with its identity and generated assembly hashes: declared={manifest.PackId}; expected={expectedPackId}."
                );
            }

            RequireExactString(
                reportRoot,
                "validationMode",
                manifest.ValidationMode,
                "patch validation report"
            );
            RequireExactString(
                reportRoot,
                "patchSetVersion",
                manifest.PatchSetVersion,
                "patch validation report"
            );
            RequireExactString(
                reportRoot,
                "validationSurfaceVersion",
                manifest.ValidationSurfaceVersion,
                "patch validation report"
            );
            RequireReportCounts(reportRoot, manifest);
            return RuntimePackCandidateValidationResult.Valid(manifest);
        }
        catch (Exception ex) when (
            ex is IOException
                or UnauthorizedAccessException
                or JsonException
                or InvalidDataException
                or ArgumentException
        )
        {
            return RuntimePackCandidateValidationResult.Rejected(
                $"Candidate read-back validation failed: {ex.GetBaseException().Message}"
            );
        }
    }

    private static void RequireObject(JsonElement root, string description)
    {
        if (root.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException($"Candidate {description} root must be an object.");
    }

    private static void RequireSchema(JsonElement root, string description)
    {
        if (!root.TryGetProperty("schemaVersion", out var schema)
            || schema.ValueKind != JsonValueKind.Number
            || !schema.TryGetInt32(out var version)
            || version != CandidateSchemaVersion)
        {
            throw new InvalidDataException(
                $"Candidate {description} must use schema {CandidateSchemaVersion}."
            );
        }
    }

    private static void RequireDeclaredIdentity(
        JsonElement root,
        GameIdentity expectedIdentity,
        string description
    )
    {
        if (!root.TryGetProperty("gameIdentity", out var identityRoot)
            || identityRoot.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException(
                $"Candidate {description} is missing its complete gameIdentity object."
            );
        }
        if (!identityRoot.TryGetProperty("schemaVersion", out var schema)
            || schema.ValueKind != JsonValueKind.Number
            || !schema.TryGetInt32(out var schemaVersion)
            || schemaVersion != GameIdentity.SchemaVersion)
        {
            throw new InvalidDataException(
                $"Candidate {description} has an unknown gameIdentity schema."
            );
        }

        var branch = RequiredString(identityRoot, "branch", description);
        var installGeneration = RequiredString(
            identityRoot,
            "installGeneration",
            description
        );
        var pckSha256 = RequiredString(identityRoot, "pckSha256", description);
        var sourceAssemblySha256 = RequiredString(
            identityRoot,
            "sourceAssemblySha256",
            description
        );
        if (!GameIdentity.TryCreate(
                branch,
                installGeneration,
                pckSha256,
                sourceAssemblySha256,
                out var declaredIdentity
            )
            || declaredIdentity != expectedIdentity)
        {
            throw new InvalidDataException(
                $"Candidate {description} does not declare the exact authoritative GameIdentity."
            );
        }

        RequireExactString(
            root,
            "gameIdentityId",
            expectedIdentity.Id,
            description
        );
    }

    private static void RequireReportCounts(
        JsonElement reportRoot,
        RuntimePackManifest manifest
    )
    {
        var checkedCount = RequiredInt(reportRoot, "checkedSymbolCount");
        var presentCount = RequiredInt(reportRoot, "presentSymbolCount");
        var missingCount = RequiredInt(reportRoot, "missingSymbolCount");
        if (checkedCount != manifest.CheckedSymbolCount
            || presentCount != manifest.PresentSymbolCount
            || missingCount != manifest.MissingSymbolCount
            || presentCount + missingCount != checkedCount
            || missingCount != 0)
        {
            throw new InvalidDataException(
                "Candidate manifest and validation report symbol counts are inconsistent."
            );
        }

        if (!reportRoot.TryGetProperty("symbolChecks", out var symbolChecks)
            || symbolChecks.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException(
                "Candidate validation report is missing symbolChecks."
            );
        }
        var checks = symbolChecks.EnumerateArray().ToArray();
        if (checks.Length != checkedCount
            || checks.Any(check => check.ValueKind != JsonValueKind.Object
                || !check.TryGetProperty("Present", out var present)
                || present.ValueKind != JsonValueKind.True))
        {
            throw new InvalidDataException(
                "Candidate validation report symbolChecks do not support its passed status."
            );
        }

        if (!reportRoot.TryGetProperty("missingSymbols", out var missingSymbols)
            || missingSymbols.ValueKind != JsonValueKind.Array
            || missingSymbols.GetArrayLength() != 0)
        {
            throw new InvalidDataException(
                "Candidate validation report contains missing symbols despite declaring passed."
            );
        }
    }

    private static int RequiredInt(JsonElement root, string property)
    {
        if (!root.TryGetProperty(property, out var value)
            || value.ValueKind != JsonValueKind.Number
            || !value.TryGetInt32(out var result)
            || result < 0)
        {
            throw new InvalidDataException(
                $"Candidate report property '{property}' must be a non-negative integer."
            );
        }
        return result;
    }

    private static string RequiredString(
        JsonElement root,
        string property,
        string description
    )
    {
        if (!root.TryGetProperty(property, out var value)
            || value.ValueKind != JsonValueKind.String
            || string.IsNullOrWhiteSpace(value.GetString()))
        {
            throw new InvalidDataException(
                $"Candidate {description} property '{property}' is missing."
            );
        }
        return value.GetString().Trim();
    }

    private static void RequireExactString(
        JsonElement root,
        string property,
        string expected,
        string description
    )
    {
        var actual = RequiredString(root, property, description);
        if (!string.Equals(actual, expected, StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"Candidate {description} property '{property}' is inconsistent: declared={actual}; expected={expected}."
            );
        }
    }

    private static void RequireOptionalExpected(
        string actual,
        string expected,
        string description
    )
    {
        if (!string.IsNullOrWhiteSpace(expected)
            && !string.Equals(actual, expected, StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"Candidate {description} mismatch: declared={actual}; expected={expected}."
            );
        }
    }
}
