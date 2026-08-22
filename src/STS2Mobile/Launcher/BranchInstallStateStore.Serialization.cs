using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace STS2Mobile.Launcher;

internal sealed partial class BranchInstallStateStore
{
    private static readonly JsonSerializerOptions StateJsonOptions = new()
    {
        WriteIndented = true,
    };

    private static byte[] Serialize(BranchInstallState state)
    {
        var depots = state.Depots.Select(depot => new
        {
            depotId = depot.DepotId,
            manifestId = depot.ManifestId,
            manifestSource = depot.ManifestSource,
        }).ToArray();

        if (state.Status == BranchInstallStatus.Updating)
        {
            return JsonSerializer.SerializeToUtf8Bytes(
                new
                {
                    schemaVersion = BranchInstallState.SchemaVersion,
                    branch = state.Branch,
                    status = "updating",
                    transactionId = state.TransactionId.ToString("D"),
                    phase = state.Phase,
                    startedUtc = state.TransitionUtc.ToString("O"),
                    targetDepots = depots,
                    lastError = state.LastError,
                },
                StateJsonOptions
            );
        }

        return JsonSerializer.SerializeToUtf8Bytes(
            new
            {
                schemaVersion = BranchInstallState.SchemaVersion,
                branch = state.Branch,
                status = "ready",
                transactionId = state.TransactionId.ToString("D"),
                completedUtc = state.TransitionUtc.ToString("O"),
                depots,
                pckPreparationVersion = state.PckPreparationVersion,
                gameIdentity = new
                {
                    schemaVersion = GameIdentity.SchemaVersion,
                    branch = state.GameIdentity.Branch,
                    installGeneration = state.GameIdentity.InstallGeneration,
                    pckSha256 = state.GameIdentity.PckSha256,
                    sourceAssemblySha256 = state.GameIdentity.SourceAssemblySha256,
                },
                runtimePack = state.RuntimePack == null
                    ? null
                    : new
                    {
                        packId = state.RuntimePack.PackId,
                        patchSetVersion = state.RuntimePack.PatchSetVersion,
                        validationSurfaceVersion = state.RuntimePack.ValidationSurfaceVersion,
                        androidAssemblySha256 = state.RuntimePack.AndroidAssemblySha256,
                    },
            },
            StateJsonOptions
        );
    }

    private static BranchInstallState ReadStateFile(string path, string expectedBranch)
    {
        if (!File.Exists(path))
        {
            throw Failure(
                BranchInstallStateFailureKind.Missing,
                $"Installation state is missing for branch '{expectedBranch}'.",
                path
            );
        }

        try
        {
            using var stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                64 * 1024,
                FileOptions.SequentialScan
            );
            using var document = JsonDocument.Parse(stream);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                throw Corrupt(path, "root must be a JSON object");

            if (!root.TryGetProperty("schemaVersion", out var schemaElement))
            {
                throw Failure(
                    BranchInstallStateFailureKind.LegacyRequiresRecovery,
                    "Legacy installation state has no trustworthy schema or identity and requires recovery.",
                    path
                );
            }
            if (schemaElement.ValueKind != JsonValueKind.Number
                || !schemaElement.TryGetInt32(out var schemaVersion))
            {
                throw Corrupt(path, "schemaVersion must be an integer");
            }
            if (schemaVersion != BranchInstallState.SchemaVersion)
            {
                throw Failure(
                    BranchInstallStateFailureKind.UnknownSchema,
                    $"Unknown installation-state schema {schemaVersion}; branch is not ready.",
                    path
                );
            }

            var branch = RequiredString(root, "branch", path);
            var normalizedBranch = STS2Mobile.Steam.SteamGameBranch.StorageIdentity(branch);
            if (!string.Equals(branch, normalizedBranch, StringComparison.Ordinal)
                || !string.Equals(normalizedBranch, expectedBranch, StringComparison.Ordinal))
            {
                throw Failure(
                    BranchInstallStateFailureKind.BranchMismatch,
                    $"Installation state branch '{branch}' does not match normalized slot '{expectedBranch}'.",
                    path
                );
            }

            var transactionText = RequiredString(root, "transactionId", path);
            if (!Guid.TryParseExact(transactionText, "D", out var transactionId)
                || transactionId == Guid.Empty)
            {
                throw Corrupt(path, "transactionId must be a non-empty canonical GUID");
            }

            return RequiredString(root, "status", path) switch
            {
                "updating" => ReadUpdating(root, path, branch, transactionId),
                "ready" => ReadReady(root, path, branch, transactionId),
                var status => throw Corrupt(path, $"unknown status '{status}'"),
            };
        }
        catch (BranchInstallStateException)
        {
            throw;
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException or ArgumentException)
        {
            throw Failure(
                BranchInstallStateFailureKind.Corrupt,
                $"Installation state is unreadable or corrupt ({ex.GetType().Name}: {ex.Message}).",
                path,
                ex
            );
        }
    }

    private static BranchInstallState ReadUpdating(
        JsonElement root,
        string path,
        string branch,
        Guid transactionId
    )
    {
        RejectProperties(
            root,
            path,
            "completedUtc",
            "depots",
            "pckPreparationVersion",
            "gameIdentity",
            "runtimePack"
        );
        return BranchInstallState.Updating(
            branch,
            transactionId,
            RequiredString(root, "phase", path),
            RequiredUtc(root, "startedUtc", path),
            ReadDepots(root, "targetDepots", path),
            RequiredString(root, "lastError", path, allowEmpty: true)
        );
    }

    private static BranchInstallState ReadReady(
        JsonElement root,
        string path,
        string branch,
        Guid transactionId
    )
    {
        RejectProperties(root, path, "phase", "startedUtc", "targetDepots", "lastError");
        if (!root.TryGetProperty("gameIdentity", out var identityElement)
            || identityElement.ValueKind != JsonValueKind.Object)
        {
            throw Corrupt(path, "ready state requires a gameIdentity object");
        }
        if (!identityElement.TryGetProperty("schemaVersion", out var identitySchema)
            || identitySchema.ValueKind != JsonValueKind.Number
            || !identitySchema.TryGetInt32(out var identitySchemaVersion)
            || identitySchemaVersion != GameIdentity.SchemaVersion)
        {
            throw Corrupt(path, "ready state gameIdentity has an unknown or missing schema");
        }

        var identity = new GameIdentity(
            RequiredString(identityElement, "branch", path),
            RequiredString(identityElement, "installGeneration", path),
            RequiredString(identityElement, "pckSha256", path),
            RequiredString(identityElement, "sourceAssemblySha256", path)
        );
        if (!string.Equals(identity.Branch, branch, StringComparison.Ordinal))
            throw Corrupt(path, "ready gameIdentity belongs to a different branch");

        BranchInstallRuntimePack runtimePack = null;
        if (root.TryGetProperty("runtimePack", out var runtimePackElement)
            && runtimePackElement.ValueKind != JsonValueKind.Null)
        {
            if (runtimePackElement.ValueKind != JsonValueKind.Object)
                throw Corrupt(path, "ready state runtimePack must be an object or null");
            runtimePack = new BranchInstallRuntimePack(
                RequiredString(runtimePackElement, "packId", path),
                RequiredString(runtimePackElement, "patchSetVersion", path),
                RequiredString(runtimePackElement, "validationSurfaceVersion", path),
                RequiredString(runtimePackElement, "androidAssemblySha256", path)
            );
        }

        return BranchInstallState.Ready(
            branch,
            transactionId,
            RequiredUtc(root, "completedUtc", path),
            ReadDepots(root, "depots", path),
            RequiredString(root, "pckPreparationVersion", path),
            identity,
            runtimePack
        );
    }

    private static IReadOnlyList<BranchInstallDepot> ReadDepots(
        JsonElement root,
        string property,
        string path
    )
    {
        if (!root.TryGetProperty(property, out var array)
            || array.ValueKind != JsonValueKind.Array)
        {
            throw Corrupt(path, $"{property} must be an array");
        }

        var depots = new List<BranchInstallDepot>();
        foreach (var row in array.EnumerateArray())
        {
            if (row.ValueKind != JsonValueKind.Object
                || !row.TryGetProperty("depotId", out var depotElement)
                || !depotElement.TryGetUInt64(out var depotId)
                || !row.TryGetProperty("manifestId", out var manifestElement)
                || !manifestElement.TryGetUInt64(out var manifestId))
            {
                throw Corrupt(path, $"{property} contains an invalid depot row");
            }
            depots.Add(new BranchInstallDepot(
                depotId,
                manifestId,
                RequiredString(row, "manifestSource", path)
            ));
        }
        return depots;
    }

    private static DateTimeOffset RequiredUtc(
        JsonElement root,
        string property,
        string path
    )
    {
        var value = RequiredString(root, property, path);
        if (!DateTimeOffset.TryParseExact(
                value,
                "O",
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var parsed)
            || parsed.Offset != TimeSpan.Zero)
        {
            throw Corrupt(path, $"{property} must be a round-trip UTC timestamp");
        }
        return parsed;
    }

    private static string RequiredString(
        JsonElement root,
        string property,
        string path,
        bool allowEmpty = false
    )
    {
        if (!root.TryGetProperty(property, out var element)
            || element.ValueKind != JsonValueKind.String)
        {
            throw Corrupt(path, $"{property} must be a string");
        }

        var value = element.GetString() ?? string.Empty;
        if (!allowEmpty && string.IsNullOrWhiteSpace(value))
            throw Corrupt(path, $"{property} cannot be empty");
        return allowEmpty ? value : value.Trim();
    }

    private static void RejectProperties(
        JsonElement root,
        string path,
        params string[] properties
    )
    {
        foreach (var property in properties)
        {
            if (root.TryGetProperty(property, out _))
                throw Corrupt(path, $"status-specific field '{property}' contradicts the state status");
        }
    }

    private static BranchInstallStateException Corrupt(string path, string detail)
        => Failure(
            BranchInstallStateFailureKind.Corrupt,
            $"Installation state is corrupt: {detail}.",
            path
        );
}
