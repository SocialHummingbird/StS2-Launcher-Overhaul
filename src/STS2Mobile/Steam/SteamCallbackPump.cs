using System;
using System.Threading;
using SteamKit2;

namespace STS2Mobile.Steam;

internal sealed class SteamCallbackPump
{
    private readonly CallbackManager _callbackManager;
    private readonly Action<string> _log;
    private readonly Action _onCallbackError;
    private readonly string _threadName;
    private Thread _thread;
    private volatile bool _running;

    internal SteamCallbackPump(
        CallbackManager callbackManager,
        string threadName,
        Action<string> log,
        Action onCallbackError = null
    )
    {
        _callbackManager = callbackManager;
        _threadName = threadName;
        _log = log;
        _onCallbackError = onCallbackError;
    }

    internal void Start()
    {
        if (_thread != null && _thread.IsAlive)
            return;

        _running = true;
        _thread = new Thread(ProcessLoop)
        {
            IsBackground = true,
            Name = _threadName,
        };
        _thread.Start();
    }

    internal void Stop(int joinTimeoutMs, bool clearThread = false)
    {
        _running = false;
        _thread?.Join(joinTimeoutMs);
        if (clearThread)
            _thread = null;
    }

    private void ProcessLoop()
    {
        while (_running)
        {
            try
            {
                _callbackManager.RunWaitCallbacks(TimeSpan.FromSeconds(1));
            }
            catch (ObjectDisposedException) when (!_running)
            {
                break;
            }
            catch (Exception ex)
            {
                _log($"Steam callback error: {ex.GetType().Name}: {ex.Message}");
                _onCallbackError?.Invoke();
                Thread.Sleep(500);
            }
        }
    }
}
