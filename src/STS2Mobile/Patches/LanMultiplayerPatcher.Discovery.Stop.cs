using System;
using Godot;

namespace STS2Mobile.Patches;

internal static partial class LanMultiplayerPatcher
{
    private sealed partial class LanDiscovery
    {
        internal void Stop()
        {
            lock (_stateLock)
            {
                if (!_running)
                {
                    _cleanupDiscoveryState();
                    return;
                }
                _running = false;
            }

            try
            {
                _udpClient?.Close();
            }
            catch (Exception ex)
            {
                PatchHelper.Log($"LAN discovery UDP close failed: {ex.Message}");
            }

            _udpClient = null;
            _listenThread?.Join(500);

            if (_pollTimer != null && GodotObject.IsInstanceValid(_pollTimer))
            {
                _pollTimer.Stop();
                _pollTimer.QueueFree();
                _pollTimer = null;
            }

            foreach (var btn in _hostButtons.Values)
            {
                if (GodotObject.IsInstanceValid(btn))
                    btn.QueueFree();
            }
            _hostButtons.Clear();
            _cleanupDiscoveryState();

            PatchHelper.Log("LAN discovery stopped");
        }

        private void _cleanupDiscoveryState()
        {
            _running = false;
            _udpClient = null;
            _listenThread = null;
            _screen = null;
            _buttonContainer = null;
            _localIps = null;
            _contextDirty = false;
        }
    }
}
