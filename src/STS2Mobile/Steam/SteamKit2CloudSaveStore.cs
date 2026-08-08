using System;
using System.Threading;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Saves;

namespace STS2Mobile.Steam;

// ICloudSaveStore backed by SteamKit2 CCloud unified messages.
internal partial class SteamKit2CloudSaveStore :
    ICloudSaveStore,
    ISaveStore,
    ICancellableSaveStore,
    IRawSaveStore,
    ICancellableCloudMetadataStore,
    ITransferSaveStore,
    IDisposable
{
    private static SteamKit2CloudSaveStore _instance;

    private readonly string _accountName;
    private readonly string _refreshToken;
    private readonly SteamConnection _connection;
    private readonly CloudFileCache _cache;

    private SteamKit2CloudSaveStore(string accountName, string refreshToken)
    {
        _accountName = accountName ?? string.Empty;
        _refreshToken = refreshToken ?? string.Empty;
        _connection = new SteamConnection(accountName, refreshToken);
        _cache = new CloudFileCache(_connection);

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

    void ICancellableCloudMetadataStore.PrepareFileMetadata(
        CancellationToken cancellationToken
    )
        => _cache.EnsureLoadedOrThrow(cancellationToken);

    Task<ulong> ITransferSaveStore.GetAuthenticatedSteamId64Async(
        CancellationToken cancellationToken
    )
        => _connection.GetAuthenticatedSteamId64Async(cancellationToken);

    void IDisposable.Dispose()
        => Dispose();

    private void Dispose()
    {
        _connection.Dispose();
        _http.Dispose();
        if (_instance == this)
            _instance = null;
    }
}
