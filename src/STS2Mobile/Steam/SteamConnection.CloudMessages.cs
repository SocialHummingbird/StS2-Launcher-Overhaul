using System;
using System.Threading.Tasks;
using SteamKit2;

namespace STS2Mobile.Steam;

internal sealed partial class SteamConnection
{
    private const int CloudRpcTimeoutMs = 45_000;

    // Sends a CCloud RPC. Connects on demand and retries on
    // transient connection failure.
    internal async Task<TResult> SendCloud<TRequest, TResult>(string method, TRequest request)
        where TRequest : ProtoBuf.IExtensible, new()
        where TResult : ProtoBuf.IExtensible, new()
    {
        EnsureConnected();

        await _sendLock.WaitAsync().ConfigureAwait(false);
        try
        {
            ThrowIfDisposing();
            var job = _unifiedMessages.SendMessage<TRequest, TResult>(
                CloudRpcEndpoint(method),
                request
            );
            var response = await WaitForCloudJobAsync(method, job.ToTask()).ConfigureAwait(false);
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

    private static async Task<TResponse> WaitForCloudJobAsync<TResponse>(
        string method,
        Task<TResponse> task
    )
    {
        var deadline = Environment.TickCount64 + CloudRpcTimeoutMs;

        while (!task.IsCompleted)
        {
            if (Environment.TickCount64 >= deadline)
                throw CloudRpcTimedOut(method);

            if (OperatingSystem.IsAndroid())
                AndroidBridgeDispatcher.Pump();

            var remaining = Math.Max(1, deadline - Environment.TickCount64);
            await Task.WhenAny(task, Task.Delay((int)Math.Min(50, remaining))).ConfigureAwait(false);
        }

        try
        {
            return await task.ConfigureAwait(false);
        }
        catch (TaskCanceledException ex)
        {
            throw CloudRpcCanceled(method, ex);
        }
    }

    private static InvalidOperationException CloudRpcFailed(string method, EResult result)
        => new($"Cloud.{method} failed: {result}");

    private static TimeoutException CloudRpcTimedOut(string method)
        => new($"Cloud.{method} timed out after {CloudRpcTimeoutMs}ms");

    private static TimeoutException CloudRpcCanceled(string method, TaskCanceledException ex)
        => new($"Cloud.{method} was canceled before completion", ex);
}
