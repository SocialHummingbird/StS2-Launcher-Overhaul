namespace STS2Mobile.Launcher;

internal sealed class LauncherLaunchHandoffResult
{
    private LauncherLaunchHandoffResult(
        bool requested,
        string requestedPhase,
        string failurePhase,
        string failureProblem,
        bool writePatchLog
    )
    {
        Requested = requested;
        RequestedPhase = requestedPhase ?? string.Empty;
        FailurePhase = failurePhase;
        FailureProblem = failureProblem;
        WritePatchLog = writePatchLog;
    }

    internal bool Requested { get; }
    internal string RequestedPhase { get; }
    internal string FailurePhase { get; }
    internal string FailureProblem { get; }
    internal bool WritePatchLog { get; }

    internal static LauncherLaunchHandoffResult Success(string requestedPhase)
        => new(
            requested: true,
            requestedPhase,
            failurePhase: "",
            failureProblem: "",
            writePatchLog: false
        );

    internal static LauncherLaunchHandoffResult Failed(
        string failurePhase,
        string failureProblem,
        bool writePatchLog
    )
        => new(
            requested: false,
            requestedPhase: "",
            failurePhase,
            failureProblem,
            writePatchLog
        );
}
