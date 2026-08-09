using System;
using System.Threading.Tasks;

namespace STS2Mobile.Launcher;

internal enum LauncherThreadedRequestState
{
    Accepted,
    AlreadyInProgress,
    Rejected,
}

internal interface ILauncherThreadedResourceApi<TResource>
    where TResource : class
{
    LauncherThreadedRequestState Request(string path, string typeHint);
    LauncherThreadedLoadPollState Poll(string path);
    TResource Retrieve(string path);
}

internal enum LauncherThreadedResourceOperationOutcome
{
    Loaded,
    Failed,
    DeadlineExceeded,
    Cancelled,
}

internal readonly struct LauncherThreadedResourceOperationResult<TResource>
    where TResource : class
{
    private LauncherThreadedResourceOperationResult(
        LauncherThreadedResourceOperationOutcome outcome,
        TResource resource,
        string failure
    )
    {
        Outcome = outcome;
        Resource = resource;
        Failure = failure;
    }

    internal LauncherThreadedResourceOperationOutcome Outcome { get; }
    internal TResource Resource { get; }
    internal string Failure { get; }

    internal static LauncherThreadedResourceOperationResult<TResource> Loaded(
        TResource resource
    )
        => new(LauncherThreadedResourceOperationOutcome.Loaded, resource, string.Empty);

    internal static LauncherThreadedResourceOperationResult<TResource> Failed(
        string failure
    )
        => new(LauncherThreadedResourceOperationOutcome.Failed, null, failure);

    internal static LauncherThreadedResourceOperationResult<TResource> DeadlineExceeded()
        => new(
            LauncherThreadedResourceOperationOutcome.DeadlineExceeded,
            null,
            "overall launcher deadline reached"
        );

    internal static LauncherThreadedResourceOperationResult<TResource> Cancelled()
        => new(
            LauncherThreadedResourceOperationOutcome.Cancelled,
            null,
            "launcher scene was destroyed"
        );
}

internal static class LauncherThreadedResourceLoadOperation
{
    internal static async Task<LauncherThreadedResourceOperationResult<TResource>>
        RunAsync<TResource>(
            ILauncherThreadedResourceApi<TResource> resources,
            LauncherAsyncSignalWaiter waiter,
            string path,
            string typeHint,
            LauncherMonotonicDeadline deadline
        )
        where TResource : class
    {
        try
        {
            if (deadline.IsExpired)
                return LauncherThreadedResourceOperationResult<TResource>.DeadlineExceeded();

            var requestState = resources.Request(path, typeHint ?? string.Empty);
            var requestDecision = LauncherThreadedLoadPolicy.EvaluateRequest(
                requestState == LauncherThreadedRequestState.Accepted,
                requestState == LauncherThreadedRequestState.AlreadyInProgress,
                deadline.IsExpired
            );
            if (requestDecision == LauncherThreadedLoadDecision.DeadlineExceeded)
                return LauncherThreadedResourceOperationResult<TResource>.DeadlineExceeded();
            if (requestDecision == LauncherThreadedLoadDecision.Fail)
            {
                return LauncherThreadedResourceOperationResult<TResource>.Failed(
                    $"threaded load request was {requestState}"
                );
            }

            while (true)
            {
                var status = resources.Poll(path);
                var decision = LauncherThreadedLoadPolicy.EvaluatePoll(
                    status,
                    deadline.IsExpired
                );
                switch (decision)
                {
                    case LauncherThreadedLoadDecision.Complete:
                        {
                            var resource = resources.Retrieve(path);
                            return resource != null
                                ? LauncherThreadedResourceOperationResult<TResource>.Loaded(resource)
                                : LauncherThreadedResourceOperationResult<TResource>.Failed(
                                    "threaded load completed without a resource"
                                );
                        }
                    case LauncherThreadedLoadDecision.Fail:
                        return LauncherThreadedResourceOperationResult<TResource>.Failed(
                            $"threaded load finished with status {status}"
                        );
                    case LauncherThreadedLoadDecision.DeadlineExceeded:
                        return LauncherThreadedResourceOperationResult<TResource>.DeadlineExceeded();
                }

                var frame = await waiter.WaitForProcessFrameAsync(deadline);
                if (frame == LauncherAsyncWaitOutcome.Destroyed)
                    return LauncherThreadedResourceOperationResult<TResource>.Cancelled();
                if (frame == LauncherAsyncWaitOutcome.DeadlineExceeded)
                    return LauncherThreadedResourceOperationResult<TResource>.DeadlineExceeded();
            }
        }
        catch (Exception ex)
        {
            return LauncherThreadedResourceOperationResult<TResource>.Failed(
                ex.GetBaseException().Message
            );
        }
    }
}
