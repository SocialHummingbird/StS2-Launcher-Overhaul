#nullable enable

namespace STS2Mobile.Steam;

internal readonly record struct SaveRecoveryStatus(
    bool SyncHeld,
    bool CanUndo,
    bool ValidationRequired,
    bool CanApprove,
    ulong? SteamId64,
    SaveNamespace SaveNamespace,
    string RuntimeIdentity,
    string ModSetFingerprint,
    string Message
)
{
    internal static SaveRecoveryStatus None => new(
        false,
        false,
        false,
        false,
        null,
        SaveNamespace.Vanilla,
        "",
        "",
        "No local recovery is active."
    );
}
