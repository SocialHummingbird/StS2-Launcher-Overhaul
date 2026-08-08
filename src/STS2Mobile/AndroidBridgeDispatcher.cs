using System;
using System.Collections.Concurrent;
using System.Threading;

namespace STS2Mobile;

internal static class AndroidBridgeDispatcher
{
    private const int BridgeTimeoutMs = 180_000;
    private static readonly ConcurrentQueue<BridgeRequest> Requests = new();
    private static readonly object StateLock = new();
    private static int _mainThreadId;
    private static bool _registered;

    internal static void RegisterCurrentThread()
    {
        if (!OperatingSystem.IsAndroid())
            return;

        lock (StateLock)
        {
            _mainThreadId = Environment.CurrentManagedThreadId;
            _registered = true;
        }
    }

    internal static void UnregisterCurrentThread()
    {
        if (!OperatingSystem.IsAndroid())
            return;

        lock (StateLock)
        {
            if (_mainThreadId != Environment.CurrentManagedThreadId)
                return;

            _registered = false;
            _mainThreadId = 0;
        }
    }

    internal static void Pump()
    {
        if (!CanPumpOnCurrentThread())
            return;

        while (Requests.TryDequeue(out var request))
            request.Execute();
    }

    internal static T Run<T>(Func<T> action)
    {
        if (!ShouldDispatch())
            return action();

        var request = new BridgeRequest<T>(action);
        Requests.Enqueue(request);
        return request.Wait(BridgeTimeoutMs);
    }

    private static bool ShouldDispatch()
    {
        if (!OperatingSystem.IsAndroid())
            return false;

        lock (StateLock)
        {
            return _registered && _mainThreadId != Environment.CurrentManagedThreadId;
        }
    }

    private static bool CanPumpOnCurrentThread()
    {
        if (!OperatingSystem.IsAndroid())
            return true;

        lock (StateLock)
        {
            return _registered && _mainThreadId == Environment.CurrentManagedThreadId;
        }
    }

    private abstract class BridgeRequest
    {
        internal abstract void Execute();
    }

    private sealed class BridgeRequest<T> : BridgeRequest
    {
        private readonly Func<T> _action;
        private readonly ManualResetEventSlim _completed = new();
        private Exception _exception;
        private T _result;

        internal BridgeRequest(Func<T> action)
        {
            _action = action;
        }

        internal override void Execute()
        {
            try
            {
                _result = _action();
            }
            catch (Exception ex)
            {
                _exception = ex;
            }
            finally
            {
                _completed.Set();
            }
        }

        internal T Wait(int timeoutMs)
        {
            if (!_completed.Wait(timeoutMs))
                throw new TimeoutException("Android bridge call timed out waiting for the Godot main thread.");

            if (_exception != null)
                throw _exception;

            return _result;
        }
    }
}
