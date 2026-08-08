using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Godot;

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
        private readonly Dictionary<string, (string hostname, int port, DateTime lastSeen)> _hosts =
            new();

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
                _udpClient.Client.SetSocketOption(
                    SocketOptionLevel.Socket,
                    SocketOptionName.ReuseAddress,
                    true
                );
                _udpClient.Client.SetSocketOption(
                    SocketOptionLevel.Socket,
                    SocketOptionName.Broadcast,
                    true
                );
                _udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, BeaconPort));
            }
            catch (SocketException ex)
            {
                PatchHelper.Log($"Discovery bind failed on port {BeaconPort}: {ex.Message}");
                _udpClient?.Close();
                _udpClient = null;
                return;
            }

            _listenThread = new Thread(ListenLoop) { IsBackground = true, Name = "LanDiscovery" };
            _listenThread.Start();

            _pollTimer = new Godot.Timer();
            _pollTimer.WaitTime = 1.0;
            _pollTimer.Autostart = true;
            ((Node)screen).AddChild(_pollTimer);
            _pollTimer.Connect("timeout", Callable.From(PollHosts));

            PatchHelper.Log("LAN discovery started");
        }
    }
}
