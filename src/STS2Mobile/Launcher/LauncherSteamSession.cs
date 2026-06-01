using STS2Mobile.Steam;
using System;
using System.Threading.Tasks;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherSteamSession : IDisposable
{
    private readonly string _dataDir;
    private readonly SteamCredentialStore _credentialStore;
    private TaskCompletionSource<string> _codeTcs;
    private SteamConnection _connection;
    private SteamAuth _auth;
    private volatile bool _awaitingCode;

    internal LauncherSteamSession(string dataDir, SteamCredentialStore credentialStore)
    {
        _dataDir = dataDir;
        _credentialStore = credentialStore;
    }

    internal SteamConnection Connection => _connection;
    internal bool AwaitingCode => _awaitingCode;

    internal void Retry(bool downloadActive)
    {
        if (!downloadActive)
        {
            _connection?.Dispose();
            _connection = null;
        }

        ResetAuth();
    }

    internal void Dispose(bool preserveLaunchConnection)
    {
        ResetAuth();
        if (!preserveLaunchConnection)
            _connection?.Dispose();
    }

    void IDisposable.Dispose() => Dispose(preserveLaunchConnection: false);

    private void ResetAuth()
    {
        _auth?.Dispose();
        _auth = null;
    }
}
