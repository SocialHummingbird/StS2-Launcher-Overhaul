namespace STS2Mobile.Launcher;

internal enum LauncherThreadedLoadPollState
{
    InvalidResource,
    InProgress,
    Failed,
    Loaded,
}

internal enum LauncherThreadedLoadDecision
{
    ContinuePolling,
    Complete,
    Fail,
    DeadlineExceeded,
}

internal static class LauncherThreadedLoadPolicy
{
    internal static LauncherThreadedLoadDecision EvaluateRequest(
        bool requestAccepted,
        bool requestAlreadyInProgress,
        bool deadlineExpired
    )
    {
        if (deadlineExpired)
            return LauncherThreadedLoadDecision.DeadlineExceeded;

        return requestAccepted || requestAlreadyInProgress
            ? LauncherThreadedLoadDecision.ContinuePolling
            : LauncherThreadedLoadDecision.Fail;
    }

    internal static LauncherThreadedLoadDecision EvaluatePoll(
        LauncherThreadedLoadPollState state,
        bool deadlineExpired
    )
    {
        if (deadlineExpired)
            return LauncherThreadedLoadDecision.DeadlineExceeded;

        return state switch
        {
            LauncherThreadedLoadPollState.InProgress
                => LauncherThreadedLoadDecision.ContinuePolling,
            LauncherThreadedLoadPollState.Loaded
                => LauncherThreadedLoadDecision.Complete,
            _ => LauncherThreadedLoadDecision.Fail,
        };
    }
}
