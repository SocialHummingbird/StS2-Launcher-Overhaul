namespace STS2Mobile.Launcher;

internal sealed class PatchCompatibilityValidationResult
{
    internal PatchCompatibilityValidationResult(
        GameRuntimeSlot runtimeSlot,
        RuntimePackCandidate candidate,
        string problem
    )
    {
        RuntimeSlot = runtimeSlot;
        Candidate = candidate;
        Problem = problem ?? string.Empty;
    }

    internal GameRuntimeSlot RuntimeSlot { get; }
    internal RuntimePackCandidate Candidate { get; }
    internal string Problem { get; }
}
