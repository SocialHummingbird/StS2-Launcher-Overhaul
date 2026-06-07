using System;
using System.Threading;
using System.Threading.Tasks;
using SteamKit2;
using SteamKit2.Authentication;

namespace STS2Mobile.Steam;

// Handles one-time interactive Steam login (password + 2FA). Creates a temporary
// SteamClient for the auth flow, returns credentials, then disposes. Does NOT
// call SteamUser.LogOn - callers use the returned refresh token with SteamConnection.
internal sealed partial class SteamAuth : IDisposable, IAuthenticator
{
    internal readonly struct LoginCredentials
    {
        internal LoginCredentials(string accountName, string refreshToken, string guardData)
        {
            AccountName = accountName;
            RefreshToken = refreshToken;
            GuardData = guardData;
        }

        internal string AccountName { get; }
        private string RefreshToken { get; }
        private string GuardData { get; }

        internal SteamConnection CreateConnection()
            => new(AccountName, RefreshToken);

        internal bool SaveTo(SteamCredentialStore credentialStore)
            => credentialStore.Save(AccountName, RefreshToken, GuardData);
    }

    private readonly SteamClient _client;
    private readonly CallbackManager _callbackManager;
    private readonly SteamUser _steamUser;
    private readonly SteamCallbackPump _callbackPump;
    private volatile bool _connectStarted;
    private volatile bool _credentialAuthStarted;
    private volatile bool _needsReconnectForAuth;
    private volatile bool _waitingForAuthCode;
    private volatile bool _androidAuthConnectionLossLogged;
    private readonly Func<bool, Task<string>> _codeProvider;

    private readonly ManualResetEventSlim _connectedGate = new(false);
    private bool _disposed;

    internal event Action<string> LogMessage;

    // The bool indicates whether the previous code was incorrect.
    internal SteamAuth(Func<bool, Task<string>> codeProvider)
    {
        _codeProvider = codeProvider ?? throw new ArgumentNullException(nameof(codeProvider));
        _client = new SteamClient(SteamConnectionConfigurationFactory.Create());
        _callbackManager = new CallbackManager(_client);
        _callbackPump = new SteamCallbackPump(_callbackManager, "SteamAuthCallbacks", Log);
        _steamUser = _client.GetHandler<SteamUser>();
        RegisterConnectionCallbacks();
    }

    private void Log(string msg)
    {
        PatchHelper.Log($"[Auth] {msg}");
        LogMessage?.Invoke(msg);
    }

    private static bool RequiresPersistentAuthConnection
        => !OperatingSystem.IsAndroid();
}
