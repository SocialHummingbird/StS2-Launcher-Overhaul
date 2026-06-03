using System;
using System.Threading.Tasks;
using SteamKit2;

namespace STS2Mobile.Steam;

internal sealed partial class SteamConnection
{
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
            var job = _unifiedMessages.SendMessage<TRequest, TResult>(
                CloudRpcEndpoint(method),
                request
            );
            var response = await job.ToTask().ConfigureAwait(false);
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

    private static InvalidOperationException CloudRpcFailed(string method, EResult result)
        => new($"Cloud.{method} failed: {result}");
}
