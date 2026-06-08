using System;
using System.Threading;
using SteamKit2;
using SteamKit2.Internal;

namespace STS2Mobile.Steam;


// General-purpose on-demand Steam connection. Connects when a handler is accessed,
// auto-disconnects after idle timeout, reconnects with exponential backoff on failure.
// Reuses the same SteamClient instance across reconnects for handler/service persistence.
//
// State machine:
//   Idle → Connecting       : Handler property accessed
//   Connecting → Connected  : Auth succeeds
//   Connecting → Backoff    : Connect/auth fails
//   Connected → Connected   : Handler accessed (resets idle timer)
//   Connected → Idle        : Idle timeout, no pending work
//   Connected → Draining    : Flush requested
//   Connected → Backoff     : WebSocket drops
//   Draining → Idle         : Pending RPCs complete
//   Backoff → Connecting    : Backoff expires, work pending
//   Backoff → Idle          : Backoff expires, no work pending
internal sealed partial class SteamConnection : IDisposable
{
    private enum ConnectionState
    {
        Idle,
        Connecting,
        Connected,
        Draining,
        Backoff,
    }

    private const int MaxBackoffMs = 32_000;
    private const int ConnectTimeoutMs = 30_000;

    private readonly string _accountName;
    private readonly string _refreshToken;
    private readonly int _defaultIdleTimeoutMs;

    private readonly SteamClient _client;
    private readonly CallbackManager _callbackManager;
    private readonly SteamUser _steamUser;
    private readonly SteamApps _steamApps;
    private readonly SteamContent _steamContent;
    private readonly SteamUnifiedMessages _unifiedMessages;

    private readonly SteamCallbackPump _callbackPump;

    private readonly object _stateLock = new();
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private readonly ManualResetEventSlim _connectedGate = new(false);
    private Timer _idleTimer;
    private int _backoffMs;
    private Exception _connectError;
    private volatile int _idleSuspendCount;
    private volatile bool _callbackPumpStopped;
    private volatile bool _disposing;
    private volatile bool _disposed;
    private readonly ManualResetEventSlim _teardownComplete = new(false);
    private int _teardownStarted;
    private ulong _appAccessToken;

    private ConnectionState State { get; set; } = ConnectionState.Idle;
    private bool IsConnected => State == ConnectionState.Connected;
}
