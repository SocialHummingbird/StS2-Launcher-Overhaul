namespace STS2Mobile.Launcher;

internal enum AndroidMainMenuPreparationOutcome
{
    NotRequired,
    StableRenderedFrames,
    TimedOut,
    Aborted,
    Failed,
}

internal readonly struct AndroidMainMenuPreparationResult
{
    private AndroidMainMenuPreparationResult(
        AndroidMainMenuPreparationOutcome outcome,
        string detail
    )
    {
        Outcome = outcome;
        Detail = detail;
    }

    internal AndroidMainMenuPreparationOutcome Outcome { get; }
    internal string Detail { get; }

    internal bool CanExposeMainMenu
        => Outcome is AndroidMainMenuPreparationOutcome.NotRequired
            or AndroidMainMenuPreparationOutcome.StableRenderedFrames;

    internal static AndroidMainMenuPreparationResult NotRequired()
        => new(AndroidMainMenuPreparationOutcome.NotRequired, "not required");

    internal static AndroidMainMenuPreparationResult Stable()
        => new(
            AndroidMainMenuPreparationOutcome.StableRenderedFrames,
            "consecutive FramePostDraw stability demonstrated"
        );

    internal static AndroidMainMenuPreparationResult TimedOut()
        => new(
            AndroidMainMenuPreparationOutcome.TimedOut,
            "rendered-frame stability deadline reached"
        );

    internal static AndroidMainMenuPreparationResult Aborted()
        => new(
            AndroidMainMenuPreparationOutcome.Aborted,
            "game node left the scene tree during preparation"
        );

    internal static AndroidMainMenuPreparationResult Failed(string detail)
        => new(AndroidMainMenuPreparationOutcome.Failed, detail);
}
