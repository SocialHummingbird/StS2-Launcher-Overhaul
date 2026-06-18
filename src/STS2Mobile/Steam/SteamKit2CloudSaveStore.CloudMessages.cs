namespace STS2Mobile.Steam;

internal partial class SteamKit2CloudSaveStore
{
    private TResult SendCloudBlocking<TRequest, TResult>(string method, TRequest request)
        where TRequest : ProtoBuf.IExtensible, new()
        where TResult : ProtoBuf.IExtensible, new()
        => _connection
            .SendCloud<TRequest, TResult>(method, request)
            .GetAwaiter()
            .GetResult();
}
