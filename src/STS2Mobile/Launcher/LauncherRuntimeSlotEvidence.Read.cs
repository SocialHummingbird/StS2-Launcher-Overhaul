using System.IO;
using System.Text.Json;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static partial class LauncherRuntimeSlotEvidence
{
    private const string GameIdentityIdProperty = "gameIdentityId";
    private const string BranchProperty = "branch";
    private const string FilesReadyProperty = "filesReady";
    private const string ReadinessProblemProperty = "readinessProblem";
    private const string RuntimePackUsabilityStatusProperty = "runtimePackUsabilityStatus";
    private const string PatchCompatibilityStatusProperty = "patchCompatibilityStatus";
    private const string PckSha256Property = "pckSha256";
    private const string SourceAssemblySha256Property = "sourceAssemblySha256";

    internal static bool IsAuthorized(
        string dataDir,
        GameIdentity expectedIdentity,
        string expectedPackId,
        out string problem
    )
    {
        problem = string.Empty;
        if (expectedIdentity == null)
        {
            problem = "Launch authorization requires an authoritative GameIdentity.";
            return false;
        }
        if (string.IsNullOrWhiteSpace(expectedPackId))
        {
            problem = "Launch authorization requires an exact runtime-pack ID.";
            return false;
        }

        var path = MarkerPath(dataDir);
        if (!File.Exists(path))
        {
            problem = "Launch authorization marker is missing.";
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("schemaVersion", out var schema)
                || schema.ValueKind != JsonValueKind.Number
                || !schema.TryGetInt32(out var schemaVersion)
                || schemaVersion != AuthorizationSchemaVersion)
            {
                problem = "Launch authorization marker has an unknown or missing schema.";
                return false;
            }

            var branch = RequiredString(root, BranchProperty);
            var gameIdentityId = RequiredString(root, GameIdentityIdProperty);
            var runtimePackId = RequiredString(root, "runtimePackId");
            var installGeneration = RequiredString(root, "installGeneration");
            var pckSha256 = RequiredString(root, PckSha256Property);
            var sourceAssemblySha256 = RequiredString(root, SourceAssemblySha256Property);
            var filesReady = RequiredTrue(root, FilesReadyProperty);
            var playable = RequiredTrue(root, "playable");
            var runtimeCompatible = RequiredTrue(root, "runtimeCompatible");
            var patchCompatible = RequiredTrue(root, "patchCompatible");

            var exact = filesReady
                && playable
                && runtimeCompatible
                && patchCompatible
                && SteamGameBranch.StorageIdentity(branch) == expectedIdentity.Branch
                && string.Equals(gameIdentityId, expectedIdentity.Id, System.StringComparison.Ordinal)
                && string.Equals(runtimePackId, expectedPackId, System.StringComparison.Ordinal)
                && string.Equals(installGeneration, expectedIdentity.InstallGeneration, System.StringComparison.Ordinal)
                && string.Equals(pckSha256, expectedIdentity.PckSha256, System.StringComparison.Ordinal)
                && string.Equals(sourceAssemblySha256, expectedIdentity.SourceAssemblySha256, System.StringComparison.Ordinal);
            if (exact)
                return true;

            problem = "Launch authorization marker does not exactly match the current GameIdentity and runtime pack.";
            return false;
        }
        catch (System.Exception ex)
        {
            problem = $"Launch authorization marker is unreadable: {ex.GetBaseException().Message}";
            return false;
        }
    }

    internal static string GameIdentityId(string dataDir)
        => ReadString(dataDir, GameIdentityIdProperty);

    internal static string Branch(string dataDir)
        => ReadString(dataDir, BranchProperty);

    internal static string FilesReady(string dataDir)
        => ReadString(dataDir, FilesReadyProperty);

    internal static string ReadinessProblem(string dataDir)
        => ReadString(dataDir, ReadinessProblemProperty);

    internal static string RuntimePackUsabilityStatus(string dataDir)
        => ReadString(dataDir, RuntimePackUsabilityStatusProperty);

    internal static string PatchCompatibilityStatus(string dataDir)
        => ReadString(dataDir, PatchCompatibilityStatusProperty);

    private static bool IsMissing(string value)
        => string.IsNullOrWhiteSpace(value) || value.StartsWith("<", System.StringComparison.Ordinal);

    private static string ReadString(string dataDir, string property)
    {
        try
        {
            if (!File.Exists(MarkerPath(dataDir)))
                return "<none>";

            using var document = JsonDocument.Parse(File.ReadAllText(MarkerPath(dataDir)));
            if (!document.RootElement.TryGetProperty(property, out var value))
                return "<missing>";

            return value.ValueKind switch
            {
                JsonValueKind.String => value.GetString() ?? "<missing>",
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Number => value.ToString(),
                _ => "<unsupported>"
            };
        }
        catch
        {
            return "<read failed>";
        }
    }

    private static string RequiredString(JsonElement root, string property)
    {
        if (!root.TryGetProperty(property, out var value)
            || value.ValueKind != JsonValueKind.String
            || string.IsNullOrWhiteSpace(value.GetString()))
        {
            throw new InvalidDataException(
                $"Launch authorization property '{property}' is missing."
            );
        }
        return value.GetString().Trim();
    }

    private static bool RequiredTrue(JsonElement root, string property)
        => root.TryGetProperty(property, out var value)
            && value.ValueKind == JsonValueKind.True;
}
