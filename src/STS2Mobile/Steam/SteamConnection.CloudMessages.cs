using System;
using System.Threading.Tasks;
using SteamKit2;

namespace STS2Mobile.Steam;

internal sealed partial class SteamConnection
{
    private readonly struct CloudRpcMethod
    {
        private CloudRpcMethod(string name)
        {
            Name = name;
        }

        private string Name { get; }

        internal static CloudRpcMethod Create(string name)
            => new(name);

        internal string Endpoint()
            => $"Cloud.{Name}#1";

        internal InvalidOperationException CreateFailedException(EResult result)
            => new($"Cloud.{Name} failed: {result}");
    }

    // Sends a CCloud RPC. Connects on demand and retries on
    // transient connection failure.
    internal async Task<TResult> SendCloud<TRequest, TResult>(string method, TRequest request)
        where TRequest : ProtoBuf.IExtensible, new()
        where TResult : ProtoBuf.IExtensible, new()
    {
        EnsureConnected();
        var rpc = CloudRpcMethod.Create(method);

        await _sendLock.WaitAsync().ConfigureAwait(false);
        try
        {
            var job = _unifiedMessages.SendMessage<TRequest, TResult>(rpc.Endpoint(), request);
            var response = await job.ToTask().ConfigureAwait(false);
            if (response.Result != EResult.OK)
                throw rpc.CreateFailedException(response.Result);
            return response.Body;
        }
        finally
        {
            _sendLock.Release();
        }
    }
}
