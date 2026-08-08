using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace STS2Mobile;

internal static partial class StartupPatchOrchestrator
{
    private readonly record struct PatchStep(string Name, Action<Harmony> Apply);

    private readonly record struct PatchGroup(
        string Name,
        bool Critical,
        IReadOnlyList<PatchStep> Steps
    );

    internal sealed record PatchAttempt(
        string Name,
        bool Success,
        TimeSpan Elapsed,
        string? Failure
    );

    internal sealed record PatchGroupResult(
        string Name,
        bool Critical,
        TimeSpan Elapsed,
        IReadOnlyList<PatchAttempt> Attempts
    )
    {
        internal int Applied => Attempts.Count(x => x.Success);
        internal int Total => Attempts.Count;
        internal int Failed => Total - Applied;

        internal IReadOnlyList<string> FailureMessages => Attempts
            .Where(x => !x.Success && !string.IsNullOrWhiteSpace(x.Failure))
            .Select(x => $"{x.Name}: {x.Failure}")
            .ToArray();
    }

    internal sealed class StartupPatchResult
    {
        private readonly IReadOnlyList<PatchGroupResult> _groupResults;

        internal StartupPatchResult(
            IReadOnlyList<PatchGroupResult> groupResults,
            bool criticalFailed,
            TimeSpan duration
        )
        {
            _groupResults = groupResults;
            CriticalFailed = criticalFailed;
            Duration = duration;
        }

        internal bool CriticalFailed { get; }

        internal TimeSpan Duration { get; }

        internal int TotalPatchCount => _groupResults.Sum(x => x.Total);
        internal int AppliedPatchCount => _groupResults.Sum(x => x.Applied);
        internal int FailedPatchCount => _groupResults.Sum(x => x.Failed);

        internal IEnumerable<string> FailureMessages()
        {
            foreach (var group in _groupResults)
            {
                foreach (var failure in group.FailureMessages)
                {
                    yield return $"[{group.Name}] {failure}";
                }
            }
        }

        internal bool HasFailures => FailedPatchCount > 0;
    }
}
