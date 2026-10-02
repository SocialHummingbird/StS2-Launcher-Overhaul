using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Godot;
using System.Net.NetworkInformation;
using System.Text;
using System.Linq;

namespace STS2Mobile.Patches;
internal static partial class LanMultiplayerPatcher
{
    private sealed partial class LanDiscovery
    {
        private volatile bool _running;
        private readonly object _stateLock = new();
        private Thread _listenThread;
        private UdpClient _udpClient;
        private readonly object _lock = new();
        private readonly Dictionary<string, (string hostname, int port, DateTime lastSeen)> _hosts = new();
        private Godot.Timer _pollTimer;
        private object _screen;
        private Control _buttonContainer;
        private readonly Dictionary<string, Node> _hostButtons = new();
        private HashSet<string> _localIps;
        private bool _contextDirty;
        private bool _visibilityUpdateFailureLogged;
        internal void Start(object screen, Control buttonContainer)
        {
            lock (_stateLock)
            {
                if (_running)
                    return;
            }

            Stop();
            _screen = screen;
            _buttonContainer = buttonContainer;
            _running = true;
            _localIps = GetLocalIps();
            try
            {
                _udpClient = new UdpClient();
                _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);
                _udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, BeaconPort));
            }
            catch (SocketException ex)
            {
                PatchHelper.Log($"Discovery bind failed on port {BeaconPort}: {ex.Message}");
                _udpClient?.Close();
                _udpClient = null;
                return;
            }

            _listenThread = new Thread(ListenLoop)
            {
                IsBackground = true,
                Name = "LanDiscovery"
            };
            _listenThread.Start();
            _pollTimer = new Godot.Timer();
            _pollTimer.WaitTime = 1.0;
            _pollTimer.Autostart = true;
            ((Node)screen).AddChild(_pollTimer);
            _pollTimer.Connect("timeout", Callable.From(PollHosts));
            PatchHelper.Log("LAN discovery started");
        }

        private void AddHostButton(string ip, string hostname, int port)
        {
            try
            {
                var fakeId = (ulong)(uint)ip.GetHashCode();
                var button = (Node)_joinFriendButtonCreate.Invoke(null, new object[] { fakeId });
                _buttonContainer.AddChild(button);
                try
                {
                    var textNode = button.GetNode("%Text");
                    textNode.Set("text", $"[center]{hostname}\n{ip}:{port}[/center]");
                }
                catch (Exception ex)
                {
                    PatchHelper.Log($"Text override failed for {ip}: {ex.Message}");
                }

                var capturedIp = ip;
                var capturedPort = port;
                button.Connect("Released", Callable.From<Control>(_ =>
                {
                    SaveLastIp(capturedIp);
                    JoinViaIp(_screen, capturedIp, capturedPort);
                }));
                _hostButtons[ip] = button;
                PatchHelper.Log($"Discovered LAN host: {hostname} @ {ip}:{port}");
            }
            catch (Exception ex)
            {
                PatchHelper.Log($"AddHostButton error: {ex.Message}");
            }
        }

        private void ListenLoop()
        {
            var ep = new IPEndPoint(IPAddress.Any, 0);
            while (true)
            {
                if (!_running || _udpClient == null)
                    break;
                try
                {
                    var data = _udpClient.Receive(ref ep);
                    var msg = Encoding.UTF8.GetString(data);
                    var parts = msg.Split('|');
                    if (parts.Length >= 3 && parts[0] == BeaconPrefix)
                    {
                        var ip = ep.Address.ToString();
                        if (_localIps.Contains(ip))
                            continue;
                        var hostname = parts[1];
                        if (int.TryParse(parts[2], out int port))
                        {
                            lock (_lock)
                            {
                                _hosts[ip] = (hostname, port, DateTime.UtcNow);
                            }
                        }
                    }
                }
                catch (SocketException)when (!_running)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    if (_running)
                        PatchHelper.Log($"Discovery recv error: {ex.Message}");
                }
            }
        }

        private static HashSet<string> GetLocalIps()
        {
            var ips = new HashSet<string>();
            try
            {
                foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != OperationalStatus.Up)
                        continue;
                    foreach (var addr in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                            ips.Add(addr.Address.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                PatchHelper.Log($"LAN local IP enumeration failed: {ex.Message}");
            }

            return ips;
        }

        private void PollHosts()
        {
            if (!_running)
                return;
            Dictionary<string, (string hostname, int port, DateTime lastSeen)> snapshot;
            lock (_lock)
            {
                var staleKeys = _hosts.Where(kv => (DateTime.UtcNow - kv.Value.lastSeen).TotalSeconds > 6.0).Select(kv => kv.Key).ToList();
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

    private static void StopHostBeacon()
    {
        _hostBeacon?.Stop();
        _hostBeacon = null;
    }

    private static void OpenDiscovery(object screen)
    {
        var loadingOverlay = (Control)_loadingOverlayField.GetValue(screen);
        loadingOverlay.Visible = false;
        var buttonContainer = (Control)_buttonContainerField.GetValue(screen);
        foreach (var child in buttonContainer.GetChildren())
            child.QueueFree();
        var loadingIndicator = (Control)_loadingIndicatorField.GetValue(screen);
        loadingIndicator.Visible = true;
        var noFriendsLabel = (Control)_noFriendsLabelField.GetValue(screen);
        noFriendsLabel.Visible = false;
        CloseDiscovery();
        _discovery = new LanDiscovery();
        _discovery.Start(screen, buttonContainer);
    }

    private static void CloseDiscovery()
    {
        _discovery?.Stop();
        _discovery = null;
    }
}
