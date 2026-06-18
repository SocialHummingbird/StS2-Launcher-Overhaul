using System;
using MegaCrit.Sts2.Core.Saves;

namespace STS2Mobile.Steam;

// ICloudSaveStore backed by SteamKit2 CCloud unified messages.
internal partial class SteamKit2CloudSaveStore : ICloudSaveStore, ISaveStore, IDisposable
{
    private static SteamKit2CloudSaveStore _instance;

    private readonly string _accountName;
    private readonly string _refreshToken;
    private readonly SteamConnection _connection;
    private readonly CloudFileCache _cache;
    private readonly CloudWriteQueue _writeQueue;

    private SteamKit2CloudSaveStore(string accountName, string refreshToken)
    {
        _accountName = accountName ?? string.Empty;
        _refreshToken = refreshToken ?? string.Empty;
        _connection = new SteamConnection(accountName, refreshToken);
        _cache = new CloudFileCache(_connection);
        _writeQueue = new CloudWriteQueue();

        _instance = this;
    }

    internal static ICloudSaveStore GetOrCreate(string accountName, string refreshToken)
    {
        if (_instance != null && _instance.MatchesCredentials(accountName, refreshToken))
            return _instance;

        if (_instance != null)
        {
            PatchHelper.Log("[Cloud] Replacing Steam cloud store for refreshed credentials");
            _instance.Dispose();
        }

        return new SteamKit2CloudSaveStore(accountName, refreshToken);
    }

    private bool MatchesCredentials(string accountName, string refreshToken)
        => string.Equals(_accountName, accountName ?? string.Empty, StringComparison.Ordinal)
            && string.Equals(_refreshToken, refreshToken ?? string.Empty, StringComparison.Ordinal);

    internal static bool FlushActive(int timeoutMs)
        => _instance?.Flush(timeoutMs) ?? true;

    internal static void DisposeActive(string reason)
    {
        var instance = _instance;
        if (instance == null)
            return;

        PatchHelper.Log(reason);
        instance.Dispose();
    }

    public virtual bool HasUserEnabledCloudSync()
        => true;

    private bool Flush(int timeoutMs = 5000)
    {
        var queueFlushed = TryFlush(
            () => _writeQueue.Flush(timeoutMs),
            QueueFlushFailed
        );
        return TryFlush(
            () =>
            {
                _connection.Flush();
                return true;
            },
            ConnectionFlushFailed
        ) && queueFlushed;
    }

    private static bool TryFlush(Func<bool> flush, Func<Exception, string> failureMessage)
    {
        try
        {
            return flush();
        }
        catch (Exception ex)
        {
            PatchHelper.Log(failureMessage(ex));
            return false;
        }
    }

    void IDisposable.Dispose()
        => Dispose();

    private void Dispose()
    {
        _writeQueue.Dispose();
        _connection.Dispose();
        _http.Dispose();
        if (_instance == this)
            _instance = null;
    }
}
