using System;
using MegaCrit.Sts2.Core.Saves;

namespace STS2Mobile.Steam;

// ICloudSaveStore backed by SteamKit2 CCloud unified messages.
internal sealed partial class SteamKit2CloudSaveStore : ICloudSaveStore, ISaveStore, IDisposable
{
    private static SteamKit2CloudSaveStore Instance { get; set; }

    private readonly SteamConnection _connection;
    private readonly CloudFileCache _cache;
    private readonly CloudWriteQueue _writeQueue;
    private readonly SaveBatchBuffer _saveBatch = new();

    private SteamKit2CloudSaveStore(string accountName, string refreshToken)
    {
        _connection = new SteamConnection(accountName, refreshToken);
        _cache = new CloudFileCache(_connection);
        _writeQueue = new CloudWriteQueue();

        Instance = this;
    }

    internal static ICloudSaveStore GetOrCreate(string accountName, string refreshToken)
        => Instance ?? new SteamKit2CloudSaveStore(accountName, refreshToken);

    internal static bool FlushActive(int timeoutMs)
        => Instance?.Flush(timeoutMs) ?? true;

    private bool Flush(int timeoutMs = 5000)
    {
        var queueFlushed = TryFlushWriteQueue(timeoutMs);
        return TryFlushConnection() && queueFlushed;
    }

    private bool TryFlushWriteQueue(int timeoutMs)
    {
        try
        {
            return _writeQueue.Flush(timeoutMs);
        }
        catch (Exception ex)
        {
            PatchHelper.Log(StoreMessage.QueueFlushFailed(ex));
            return false;
        }
    }

    private bool TryFlushConnection()
    {
        try
        {
            _connection.Flush();
            return true;
        }
        catch (Exception ex)
        {
            PatchHelper.Log(StoreMessage.ConnectionFlushFailed(ex));
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
        if (Instance == this)
            Instance = null;
    }
}
