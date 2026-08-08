using System.Threading.Tasks;
using Godot;

namespace STS2Mobile.Launcher;

internal enum LauncherThreadedLoadOutcome
{
    Loaded,
    Failed,
    DeadlineExceeded,
    Cancelled,
}

internal readonly struct LauncherThreadedLoadResult
{
    private LauncherThreadedLoadResult(
        LauncherThreadedLoadOutcome outcome,
        Resource resource,
        string failure
    )
    {
        Outcome = outcome;
        Resource = resource;
        Failure = failure;
    }

    internal LauncherThreadedLoadOutcome Outcome { get; }
    internal Resource Resource { get; }
    internal string Failure { get; }

    internal static LauncherThreadedLoadResult FromOperation(
        LauncherThreadedResourceOperationResult<Resource> result
    )
        => new(
            result.Outcome switch
            {
                LauncherThreadedResourceOperationOutcome.Loaded
                    => LauncherThreadedLoadOutcome.Loaded,
                LauncherThreadedResourceOperationOutcome.DeadlineExceeded
                    => LauncherThreadedLoadOutcome.DeadlineExceeded,
                LauncherThreadedResourceOperationOutcome.Cancelled
                    => LauncherThreadedLoadOutcome.Cancelled,
                _ => LauncherThreadedLoadOutcome.Failed,
            },
            result.Resource,
            result.Failure
        );
}

internal static class LauncherThreadedResourceLoader
{
    internal static async Task<LauncherThreadedLoadResult> LoadAsync(
        SceneTree tree,
        string path,
        string typeHint,
        LauncherMonotonicDeadline deadline,
        LauncherOperationLifecycle lifecycle = null
    )
    {
        var result = await LauncherThreadedResourceLoadOperation.RunAsync(
            new GodotThreadedResourceApi(),
            LauncherAsyncYield.CreateWaiter(tree, lifecycle),
            path,
            typeHint,
            deadline
        );
        return LauncherThreadedLoadResult.FromOperation(result);
    }

    private sealed class GodotThreadedResourceApi :
        ILauncherThreadedResourceApi<Resource>
    {
        public LauncherThreadedRequestState Request(string path, string typeHint)
        {
            var error = ResourceLoader.LoadThreadedRequest(
                path,
                typeHint ?? string.Empty,
                useSubThreads: false,
                ResourceLoader.CacheMode.Reuse
            );
            return error switch
            {
                Error.Ok => LauncherThreadedRequestState.Accepted,
                Error.Busy => LauncherThreadedRequestState.AlreadyInProgress,
                _ => LauncherThreadedRequestState.Rejected,
            };
        }

        public LauncherThreadedLoadPollState Poll(string path)
            => ResourceLoader.LoadThreadedGetStatus(path) switch
            {
                ResourceLoader.ThreadLoadStatus.InProgress
                    => LauncherThreadedLoadPollState.InProgress,
                ResourceLoader.ThreadLoadStatus.Loaded
                    => LauncherThreadedLoadPollState.Loaded,
                ResourceLoader.ThreadLoadStatus.Failed
                    => LauncherThreadedLoadPollState.Failed,
                _ => LauncherThreadedLoadPollState.InvalidResource,
            };

        public Resource Retrieve(string path)
            => ResourceLoader.LoadThreadedGet(path);
    }
}
