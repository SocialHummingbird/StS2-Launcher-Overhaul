using System;
using System.Threading;
using System.Threading.Tasks;
using SteamKit2;

namespace STS2Mobile.Steam;

internal sealed partial class SteamConnection
{
    private const int CloudRpcTimeoutMs = 45_000;

    internal async Task<TResult> SendCloudAsync<TRequest, TResult>(
        string method,
        TRequest request,
        CancellationToken cancellationToken
    )
        where TRequest : ProtoBuf.IExtensible, new()
        where TResult : ProtoBuf.IExtensible, new()
    {
        EnsureConnected(cancellationToken);
        await _sendLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ThrowIfDisposing();
            var job = _unifiedMessages.SendMessage<TRequest, TResult>(
                $"Cloud.{method}#1",
                request
            );
            job.Timeout = TimeSpan.FromMilliseconds(CloudRpcTimeoutMs);

            var response = await WaitForCloudJobAsync(
                method,
                job.ToTask(),
                cancellationToken
            ).ConfigureAwait(false);
            if (response.Result != EResult.OK)
                throw new InvalidOperationException(
                    $"Cloud.{method} failed: {response.Result}"
                );

            return response.Body;
        }
        finally
        {
            _sendLock.Release();
        }
    }

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
                    AbortCloudRequest(method);
                    throw new TimeoutException(
                        $"Cloud.{method} timed out after {CloudRpcTimeoutMs}ms"
                    );
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

            return await task.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            AbortCloudRequest(method);
            throw;
        }
        catch (TaskCanceledException ex)
        {
            AbortCloudRequest(method);
            throw new TimeoutException(
                $"Cloud.{method} timed out after {CloudRpcTimeoutMs}ms",
                ex
            );
        }
    }

    private void AbortCloudRequest(string method)
    {
        PatchHelper.Log($"[Cloud] Aborting Cloud.{method} by disconnecting its Steam session");
        try
        {
            DisconnectToIdle();
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Cloud] Disconnect after Cloud.{method} failed: {ex.Message}");
        }
    }
}
