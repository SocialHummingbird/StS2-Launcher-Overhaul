using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace STS2Mobile.Patches;

internal static partial class LanMultiplayerPatcher
{
    private sealed partial class LanDiscovery
    {
        private void PollHosts()
        {
            if (!_running)
                return;

            Dictionary<string, (string hostname, int port, DateTime lastSeen)> snapshot;
            lock (_lock)
            {
                var staleKeys = _hosts
                    .Where(kv => (DateTime.UtcNow - kv.Value.lastSeen).TotalSeconds > 6.0)
                    .Select(kv => kv.Key)
                    .ToList();
                foreach (var k in staleKeys)
                    _hosts.Remove(k);

                snapshot = new Dictionary<string, (string, int, DateTime)>(_hosts);
            }

            _contextDirty = false;

            var toRemove = new List<string>();
            foreach (var kv in _hostButtons)
            {
                if (!snapshot.ContainsKey(kv.Key))
                {
                    if (GodotObject.IsInstanceValid(kv.Value))
                        kv.Value.QueueFree();
                    toRemove.Add(kv.Key);
                    _contextDirty = true;
                }
            }
            foreach (var k in toRemove)
                _hostButtons.Remove(k);

            foreach (var kv in snapshot)
            {
                if (!_hostButtons.ContainsKey(kv.Key))
                {
                    AddHostButton(kv.Key, kv.Value.hostname, kv.Value.port);
                    _contextDirty = true;
                }
            }

            try
            {
                var loadingIndicator = (Control)_loadingIndicatorField.GetValue(_screen);
                loadingIndicator.Visible = false;

                var noFriendsLabel = (Control)_noFriendsLabelField.GetValue(_screen);
                noFriendsLabel.Visible = _buttonContainer.GetChildCount() == 0;
            }
            catch (Exception ex)
            {
                if (!_visibilityUpdateFailureLogged)
                {
                    _visibilityUpdateFailureLogged = true;
                    PatchHelper.Log($"LAN discovery visibility update failed: {ex.Message}");
                }
            }

            if (_contextDirty)
                UpdateScreenContext();
        }
    }
}
