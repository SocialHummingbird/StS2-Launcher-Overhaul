#nullable enable

namespace STS2Mobile.Launcher;

internal readonly record struct SaveRecoveryCandidatePresentation(
    string Id,
    string Title,
    string Detail,
    bool CanRestore
);
