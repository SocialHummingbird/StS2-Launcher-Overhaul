using System;

namespace STS2Mobile.Launcher;

internal sealed class RuntimePackPromotionResult
{
    private RuntimePackPromotionResult(
        RuntimePackManifest manifest,
        string finalDirectory,
        bool promoted,
        string problem
    )
    {
        Manifest = manifest;
        FinalDirectory = finalDirectory ?? string.Empty;
        Promoted = promoted;
        Problem = problem ?? string.Empty;
    }

    internal RuntimePackManifest Manifest { get; }
    internal string FinalDirectory { get; }
    internal bool Promoted { get; }
    internal string Problem { get; }
    internal bool Succeeded => Manifest?.Usable == true && string.IsNullOrWhiteSpace(Problem);

    internal static RuntimePackPromotionResult Success(
        RuntimePackManifest manifest,
        string finalDirectory,
        bool promoted
    ) => new(
        manifest ?? throw new ArgumentNullException(nameof(manifest)),
        finalDirectory,
        promoted,
        string.Empty
    );

    internal static RuntimePackPromotionResult Rejected(string finalDirectory, string problem)
        => new(
            null,
            finalDirectory,
            promoted: false,
            string.IsNullOrWhiteSpace(problem) ? "Runtime-pack promotion failed." : problem
        );
}

internal sealed class RuntimePackPromotionHooks
{
    internal Action BeforePromotion { get; init; }
    internal Action AfterExistingPackBackedUp { get; init; }
    internal Action AfterCandidatePromoted { get; init; }
    internal Action BeforePromotedValidation { get; init; }
}

internal sealed class RuntimePackLaunchPreparationResult
{
    private RuntimePackLaunchPreparationResult(
        GameRuntimeSlot runtimeSlot,
        bool promoted,
        string problem
    )
    {
        RuntimeSlot = runtimeSlot;
        Promoted = promoted;
        Problem = problem ?? string.Empty;
    }

    internal GameRuntimeSlot RuntimeSlot { get; }
    internal bool Promoted { get; }
    internal string Problem { get; }
    internal bool Succeeded => RuntimeSlot?.Playable == true && string.IsNullOrWhiteSpace(Problem);

    internal static RuntimePackLaunchPreparationResult Success(
        GameRuntimeSlot runtimeSlot,
        bool promoted
    ) => new(
        runtimeSlot ?? throw new ArgumentNullException(nameof(runtimeSlot)),
        promoted,
        string.Empty
    );

    internal static RuntimePackLaunchPreparationResult Rejected(
        GameRuntimeSlot runtimeSlot,
        string problem
    ) => new(
        runtimeSlot,
        promoted: false,
        string.IsNullOrWhiteSpace(problem) ? "Runtime-pack launch preparation failed." : problem
    );
}

internal sealed class RuntimePackLaunchPreparationHooks
{
    internal RuntimePackPromotionHooks Promotion { get; init; }
    internal Action BeforeAuthorizationMarker { get; init; }
}

internal readonly record struct RuntimeAssemblyCachePreparationResult(
    bool Succeeded,
    string Problem
)
{
    internal static RuntimeAssemblyCachePreparationResult Success()
        => new(true, string.Empty);

    internal static RuntimeAssemblyCachePreparationResult Rejected(string problem)
        => new(
            false,
            string.IsNullOrWhiteSpace(problem)
                ? "Active Android assembly-cache preparation failed."
                : problem
        );
}

internal interface IRuntimeAssemblyCachePreparer
{
    RuntimeAssemblyCachePreparationResult Prepare(
        string dataDir,
        GameIdentity gameIdentity,
        RuntimePackManifest manifest
    );
}
