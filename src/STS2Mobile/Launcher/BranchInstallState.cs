using System;
using System.Collections.Generic;
using System.Linq;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal enum BranchInstallStatus
{
    Updating,
    Ready,
}

internal sealed record BranchInstallDepot
{
    internal BranchInstallDepot(ulong depotId, ulong manifestId, string manifestSource)
    {
        if (depotId == 0)
            throw new ArgumentOutOfRangeException(nameof(depotId));
        if (manifestId == 0)
            throw new ArgumentOutOfRangeException(nameof(manifestId));
        if (string.IsNullOrWhiteSpace(manifestSource))
            throw new ArgumentException("A depot manifest source is required.", nameof(manifestSource));

        DepotId = depotId;
        ManifestId = manifestId;
        ManifestSource = manifestSource.Trim().ToLowerInvariant();
    }

    internal ulong DepotId { get; }
    internal ulong ManifestId { get; }
    internal string ManifestSource { get; }
}

internal sealed record BranchInstallRuntimePack
{
    internal BranchInstallRuntimePack(
        string packId,
        string patchSetVersion,
        string validationSurfaceVersion,
        string androidAssemblySha256
    )
    {
        PackId = RequireText(packId, nameof(packId));
        PatchSetVersion = RequireText(patchSetVersion, nameof(patchSetVersion));
        ValidationSurfaceVersion = RequireText(validationSurfaceVersion, nameof(validationSurfaceVersion));
        if (!GameIdentity.IsSha256(androidAssemblySha256))
            throw new ArgumentException("A complete Android assembly SHA-256 is required.", nameof(androidAssemblySha256));
        AndroidAssemblySha256 = androidAssemblySha256.ToLowerInvariant();
    }

    internal string PackId { get; }
    internal string PatchSetVersion { get; }
    internal string ValidationSurfaceVersion { get; }
    internal string AndroidAssemblySha256 { get; }

    private static string RequireText(string value, string parameterName)
        => string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("A non-empty value is required.", parameterName)
            : value.Trim();
}

internal sealed class BranchInstallState
{
    internal const int SchemaVersion = 1;

    private BranchInstallState(
        string branch,
        BranchInstallStatus status,
        Guid transactionId,
        DateTimeOffset transitionUtc,
        IReadOnlyList<BranchInstallDepot> depots,
        string phase,
        string lastError,
        string pckPreparationVersion,
        GameIdentity gameIdentity,
        BranchInstallRuntimePack runtimePack
    )
    {
        Branch = SteamGameBranch.StorageIdentity(branch);
        Status = status;
        TransactionId = transactionId;
        TransitionUtc = transitionUtc.ToUniversalTime();
        Depots = NormalizeDepots(depots);
        Phase = phase ?? string.Empty;
        LastError = lastError ?? string.Empty;
        PckPreparationVersion = pckPreparationVersion ?? string.Empty;
        GameIdentity = gameIdentity;
        RuntimePack = runtimePack;
    }

    internal string Branch { get; }
    internal BranchInstallStatus Status { get; }
    internal Guid TransactionId { get; }
    internal DateTimeOffset TransitionUtc { get; }
    internal IReadOnlyList<BranchInstallDepot> Depots { get; }
    internal string Phase { get; }
    internal string LastError { get; }
    internal string PckPreparationVersion { get; }
    internal GameIdentity GameIdentity { get; }
    internal BranchInstallRuntimePack RuntimePack { get; }
    internal bool IsReady => Status == BranchInstallStatus.Ready;

    internal static BranchInstallState Updating(
        string branch,
        Guid transactionId,
        string phase,
        DateTimeOffset startedUtc,
        IReadOnlyList<BranchInstallDepot> targetDepots,
        string lastError = ""
    )
    {
        if (transactionId == Guid.Empty)
            throw new ArgumentException("An update transaction ID is required.", nameof(transactionId));
        if (string.IsNullOrWhiteSpace(phase))
            throw new ArgumentException("An update phase is required.", nameof(phase));

        return new BranchInstallState(
            branch,
            BranchInstallStatus.Updating,
            transactionId,
            startedUtc,
            targetDepots,
            phase.Trim(),
            lastError?.Trim() ?? string.Empty,
            string.Empty,
            null,
            null
        );
    }

    internal static BranchInstallState Ready(
        string branch,
        Guid transactionId,
        DateTimeOffset completedUtc,
        IReadOnlyList<BranchInstallDepot> depots,
        string pckPreparationVersion,
        GameIdentity gameIdentity,
        BranchInstallRuntimePack runtimePack = null
    )
    {
        var normalizedBranch = SteamGameBranch.StorageIdentity(branch);
        if (transactionId == Guid.Empty)
            throw new ArgumentException("An update transaction ID is required.", nameof(transactionId));
        if (gameIdentity == null)
            throw new ArgumentNullException(nameof(gameIdentity));
        if (!string.Equals(gameIdentity.Branch, normalizedBranch, StringComparison.Ordinal))
            throw new ArgumentException("The ready identity belongs to a different normalized branch.", nameof(gameIdentity));
        if (string.IsNullOrWhiteSpace(pckPreparationVersion))
            throw new ArgumentException("A PCK preparation version is required.", nameof(pckPreparationVersion));

        return new BranchInstallState(
            normalizedBranch,
            BranchInstallStatus.Ready,
            transactionId,
            completedUtc,
            depots,
            string.Empty,
            string.Empty,
            pckPreparationVersion.Trim(),
            gameIdentity,
            runtimePack
        );
    }

    internal bool MatchesReadyIdentity(GameIdentity currentIdentity)
        => IsReady && currentIdentity != null && GameIdentity == currentIdentity;

    internal bool HasSameReadyPayload(BranchInstallState other)
        => other != null
            && IsReady
            && other.IsReady
            && TransactionId == other.TransactionId
            && GameIdentity == other.GameIdentity
            && RuntimePack == other.RuntimePack
            && string.Equals(PckPreparationVersion, other.PckPreparationVersion, StringComparison.Ordinal)
            && Depots.SequenceEqual(other.Depots);

    private static IReadOnlyList<BranchInstallDepot> NormalizeDepots(
        IReadOnlyList<BranchInstallDepot> depots
    )
    {
        var source = depots ?? Array.Empty<BranchInstallDepot>();
        if (source.Any(depot => depot == null))
            throw new ArgumentException("Depot rows cannot be null.", nameof(depots));
        var normalized = source
            .OrderBy(depot => depot.DepotId)
            .ThenBy(depot => depot.ManifestId)
            .ToArray();
        if (normalized.GroupBy(depot => depot.DepotId).Any(group => group.Count() > 1))
            throw new ArgumentException("A branch state cannot contain duplicate depot IDs.", nameof(depots));
        return normalized;
    }
}

internal enum BranchInstallStateFailureKind
{
    Missing,
    LegacyRequiresRecovery,
    Corrupt,
    UnknownSchema,
    BranchMismatch,
    IdentityMismatch,
    InvalidTransition,
    Busy,
}

internal sealed class BranchInstallStateException : System.IO.IOException
{
    internal BranchInstallStateException(
        BranchInstallStateFailureKind kind,
        string message,
        string path = null,
        Exception innerException = null
    ) : base(message, innerException)
    {
        Kind = kind;
        Path = path ?? string.Empty;
    }

    internal BranchInstallStateFailureKind Kind { get; }
    internal string Path { get; }
}
