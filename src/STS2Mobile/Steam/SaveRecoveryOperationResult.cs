#nullable enable

namespace STS2Mobile.Steam;

internal readonly record struct SaveRecoveryOperationResult(
    string Message,
    SaveRecoveryStatus Status
);
