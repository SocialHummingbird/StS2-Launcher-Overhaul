using System;
using System.Threading;
using System.Threading.Tasks;
using SteamKit2;

namespace STS2Mobile.Steam;

internal sealed partial class SteamConnection
{
    private const int CloudRpcTimeoutMs = 45_000;

    // Sends a CCloud RPC. Connects on demand and retries on
    // transient connection failure.
    internal async Task<TResult> SendCloud<TRequest, TResult>(
        string method,
        TRequest request,
        CancellationToken cancellationToken = default
    )
        where TRequest : ProtoBuf.IExtensible, new()
        where TResult : ProtoBuf.IExtensible, new()
    {
        EnsureConnected(cancellationToken);

        await _sendLock.WaitAsync(cancellationToken)
            .ConfigureAwait(false);
        try
        {
            ThrowIfDisposing();
            var job = _unifiedMessages.SendMessage<TRequest, TResult>(
                CloudRpcEndpoint(method),
                request
            );
            job.Timeout = TimeSpan.FromMilliseconds(CloudRpcTimeoutMs);
            var response = await WaitForCloudJobAsync(
                method,
                job.ToTask(),
                cancellationToken
            ).ConfigureAwait(false);
            if (response.Result != EResult.OK)
                throw CloudRpcFailed(method, response.Result);
            return response.Body;
        }
        finally
        {
            _sendLock.Release();
        }
    }

    private static string CloudRpcEndpoint(string method)
        => $"Cloud.{method}#1";

    private async Task<TResponse> WaitForCloudJobAsync<TResponse>(
        string method,
        Task<TResponse> task,
        CancellationToken cancellationToken
    )
    {
        var deadline = Environment.TickCount64 + CloudRpcTimeoutMs;

        try
        {
            while (!task.IsCompleted)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (Environment.TickCount64 >= deadline)
                {
                    await AbortAndDrainCloudJobAsync(method, task)
                        .ConfigureAwait(false);
                    throw CloudRpcTimedOut(method);
                }

                if (OperatingSystem.IsAndroid())
                    AndroidBridgeDispatcher.Pump();

                var remaining = Math.Max(1, deadline - Environment.TickCount64);
                await Task.WhenAny(
                    task,
                    Task.Delay(
                        (int)Math.Min(50, remaining),
                        cancellationToken
                    )
                ).ConfigureAwait(false);
            }

            cancellationToken.ThrowIfCancellationRequested();
            return await task.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            await AbortAndDrainCloudJobAsync(method, task)
                .ConfigureAwait(false);
            throw new OperationCanceledException(cancellationToken);
        }
        catch (TaskCanceledException ex)
        {
            throw CloudRpcCanceled(method, ex);
        }
    }

    private async Task AbortAndDrainCloudJobAsync<TResponse>(
        string method,
        Task<TResponse> task
    )
    {
        if (!task.IsCompleted)
        {
            PatchHelper.Log(
                $"[Cloud] Aborting Cloud.{method} by disconnecting its Steam session"
            );
            try
            {
                _client.Disconnect();
            }
            catch (Exception ex)
            {
                PatchHelper.Log(
                    $"[Cloud] Steam disconnect while aborting Cloud.{method} failed: {ex.Message}; waiting for the RPC to drain"
                );
            }
        }

        while (!task.IsCompleted)
        {
            if (OperatingSystem.IsAndroid())
                AndroidBridgeDispatcher.Pump();
            await Task.Delay(10).ConfigureAwait(false);
        }

        try
        {
            await task.ConfigureAwait(false);
        }
        catch
        {
            // The caller receives the original timeout or cancellation reason.
        }
    }

    private static InvalidOperationException CloudRpcFailed(string method, EResult result)
        => new($"Cloud.{method} failed: {result}");

    private static TimeoutException CloudRpcTimedOut(string method)
        => new($"Cloud.{method} timed out after {CloudRpcTimeoutMs}ms");

    private static TimeoutException CloudRpcCanceled(string method, TaskCanceledException ex)
        => new($"Cloud.{method} was canceled before completion", ex);
}
