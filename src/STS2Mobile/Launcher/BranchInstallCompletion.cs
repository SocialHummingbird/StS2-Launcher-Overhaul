using System;

namespace STS2Mobile.Launcher;

internal sealed record BranchInstallCompletion
{
    internal BranchInstallCompletion(
        string branch,
        Guid transactionId,
        GameIdentity gameIdentity
    )
    {
        if (gameIdentity == null)
            throw new ArgumentNullException(nameof(gameIdentity));
        if (!string.Equals(gameIdentity.Branch, Steam.SteamGameBranch.StorageIdentity(branch), StringComparison.Ordinal))
            throw new ArgumentException("Completion identity belongs to a different branch.", nameof(gameIdentity));

        Branch = gameIdentity.Branch;
        TransactionId = transactionId != Guid.Empty
            ? transactionId
            : throw new ArgumentException("Completion transaction ID is required.", nameof(transactionId));
        GameIdentity = gameIdentity;
    }

    internal string Branch { get; }
    internal Guid TransactionId { get; }
    internal GameIdentity GameIdentity { get; }
}
