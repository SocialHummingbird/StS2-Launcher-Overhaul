#nullable enable

using System;
using System.IO;
using System.Text.Json;

namespace STS2Mobile.Steam;

internal enum SaveNamespace
{
    Vanilla,
    Modded,
}

internal readonly record struct SaveContext
{
    private const int CurrentMarkerVersion = 1;

    private SaveContext(
        ulong steamId64,
        SaveNamespace saveNamespace,
        string runtimeIdentity,
        string modSetFingerprint
    )
    {
        SteamId64 = steamId64;
        Namespace = saveNamespace;
        RuntimeIdentity = runtimeIdentity;
        ModSetFingerprint = modSetFingerprint;
    }

    internal ulong SteamId64 { get; }
    internal SaveNamespace Namespace { get; }
    internal string RuntimeIdentity { get; }
    internal string ModSetFingerprint { get; }

    internal string MarkerPath
        => $".sts2-launcher/contexts/{NamespaceName}.json";

    internal string NamespaceName
        => Namespace == SaveNamespace.Modded ? "modded" : "vanilla";

    internal string StorageKey
        => AutomaticSyncHash.Compute(SerializeMarker());

    internal static SaveContext Create(
        ulong steamId64,
        SaveNamespace saveNamespace,
        string runtimeIdentity,
        string? modSetFingerprint
    )
    {
        if (steamId64 == 0)
            throw new ArgumentOutOfRangeException(
                nameof(steamId64),
                "SteamID64 must be authenticated before save transfer"
            );

        var normalized = NormalizeLocalSelection(
            saveNamespace,
            runtimeIdentity,
            modSetFingerprint
        );

        return new SaveContext(
            steamId64,
            normalized.Namespace,
            normalized.RuntimeIdentity,
            normalized.ModSetFingerprint
        );
    }

    internal static (
        SaveNamespace Namespace,
        string RuntimeIdentity,
        string ModSetFingerprint
    ) NormalizeLocalSelection(
        SaveNamespace saveNamespace,
        string runtimeIdentity,
        string? modSetFingerprint
    )
    {
        if (!Enum.IsDefined(saveNamespace))
            throw new ArgumentOutOfRangeException(nameof(saveNamespace));

        if (string.IsNullOrWhiteSpace(runtimeIdentity))
        {
            throw new ArgumentException(
                "Runtime compatibility identity is required",
                nameof(runtimeIdentity)
            );
        }
        var normalizedRuntime = SteamGameBranch.StorageIdentity(runtimeIdentity);
        var normalizedFingerprint = NormalizeFingerprint(modSetFingerprint);
        if (saveNamespace == SaveNamespace.Vanilla)
        {
            if (normalizedFingerprint.Length != 0)
            {
                throw new ArgumentException(
                    "Vanilla saves cannot carry a mod-set fingerprint",
                    nameof(modSetFingerprint)
                );
            }
        }
        else if (normalizedFingerprint.Length == 0)
        {
            throw new ArgumentException(
                "Modded saves require a mod-set fingerprint",
                nameof(modSetFingerprint)
            );
        }

        return (
            saveNamespace,
            normalizedRuntime,
            normalizedFingerprint
        );
    }

    internal string SerializeMarker()
        => JsonSerializer.Serialize(
            new SaveContextMarker
            {
                Version = CurrentMarkerVersion,
                SteamId64 = SteamId64,
                SaveNamespace = NamespaceName,
                RuntimeIdentity = RuntimeIdentity,
                ModSetFingerprint = ModSetFingerprint,
            }
        );

    internal static SaveContext ParseMarker(string content)
    {
        SaveContextMarker? marker;
        try
        {
            marker = JsonSerializer.Deserialize<SaveContextMarker>(content);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException(
                "Steam Cloud save-context marker is not valid JSON",
                ex
            );
        }

        if (marker is null || marker.Version != CurrentMarkerVersion)
        {
            throw new InvalidDataException(
                "Steam Cloud save-context marker has an unsupported version"
            );
        }

        var saveNamespace = marker.SaveNamespace switch
        {
            "vanilla" => SaveNamespace.Vanilla,
            "modded" => SaveNamespace.Modded,
            _ => throw new InvalidDataException(
                "Steam Cloud save-context marker has an invalid namespace"
            ),
        };

        try
        {
            return Create(
                marker.SteamId64,
                saveNamespace,
                marker.RuntimeIdentity,
                marker.ModSetFingerprint
            );
        }
        catch (ArgumentException ex)
        {
            throw new InvalidDataException(
                "Steam Cloud save-context marker is incomplete",
                ex
            );
        }
    }

    internal void RequireExactMatch(SaveContext actual)
    {
        if (SteamId64 != actual.SteamId64)
        {
            throw new InvalidOperationException(
                "Steam Cloud save-context mismatch for Steam account"
            );
        }

        if (Namespace != actual.Namespace)
            throw ContextMismatch("save namespace", Namespace, actual.Namespace);

        if (!string.Equals(
                RuntimeIdentity,
                actual.RuntimeIdentity,
                StringComparison.Ordinal
            ))
        {
            throw ContextMismatch(
                "runtime compatibility/branch",
                RuntimeIdentity,
                actual.RuntimeIdentity
            );
        }

        if (!string.Equals(
                ModSetFingerprint,
                actual.ModSetFingerprint,
                StringComparison.Ordinal
            ))
        {
            throw ContextMismatch(
                "mod set",
                DisplayFingerprint(ModSetFingerprint),
                DisplayFingerprint(actual.ModSetFingerprint)
            );
        }
    }

    private static InvalidOperationException ContextMismatch(
        string field,
        object expected,
        object actual
    )
        => new(
            $"Steam Cloud save-context mismatch for {field}: "
                + $"cloud={expected}; selected={actual}"
        );

    private static string NormalizeFingerprint(string? fingerprint)
        => string.IsNullOrWhiteSpace(fingerprint)
            ? string.Empty
            : fingerprint.Trim().ToLowerInvariant();

    private static string DisplayFingerprint(string fingerprint)
        => fingerprint.Length == 0 ? "none" : fingerprint;

    private sealed class SaveContextMarker
    {
        public int Version { get; set; }
        public ulong SteamId64 { get; set; }
        public string SaveNamespace { get; set; } = "";
        public string RuntimeIdentity { get; set; } = "";
        public string ModSetFingerprint { get; set; } = "";
    }
}
