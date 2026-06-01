using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using Godot;
using HarmonyLib;

namespace STS2Mobile.Patches;

// Enables LAN multiplayer by replacing the Steam friends list with UDP broadcast
// discovery. Hosts advertise via a beacon on port 33770, clients discover them
// automatically or connect manually by IP address.
internal static class LanMultiplayerPatcher
{
    private const int BeaconPort = 33770;
    private const int BeaconChecksPerSendInterval = 20;
    private const int BeaconSendLoopSleepMs = 100;
    private const string BeaconPrefix = "STS2LAN";
    private const int ContainerSeparation = 10;
    private const int EntryFontSize = 28;
    private const int GamePort = 33771;
    private const ulong HostPlayerId = 1uL;
    private const string LastIpConfigPath = "user://lan_last_ip.cfg";
    private const string LastIpKey = "last_ip";
    private const string LastIpSection = "lan";
    private const int MaxPort = 65535;
    private const int MinPort = 1;
    private const ulong Player2Id = 1000uL;
    private const ulong Player3Id = 1001uL;
    private const ulong Player4Id = 1002uL;

    private static FieldInfo _buttonContainerField;
    private static FieldInfo _loadingOverlayField;
    private static FieldInfo _noFriendsLabelField;
    private static FieldInfo _loadingIndicatorField;
    private static MethodInfo _joinFriendButtonCreate;
    private static ConstructorInfo _eNetClientConnInitCtor;
    private static MethodInfo _joinGameAsyncMethod;
    private static MethodInfo _taskHelperRunSafely;
    private static MethodInfo _setTextAutoSize;
    private static PropertyInfo _activeScreenContextInstance;
    private static MethodInfo _activeScreenContextUpdate;

    private static LineEdit _ipLineEdit;
    private static LanDiscovery _discovery;
    private static LanBeacon _hostBeacon;
    private static bool _joinInProgress;
    private static bool _screenContextUpdateFailureLogged;

    internal static void Apply(Harmony harmony)
    {
        try
        {
            var sts2Asm = typeof(MegaCrit.Sts2.Core.Nodes.NGame).Assembly;

            var joinScreenType = sts2Asm.GetType(
                "MegaCrit.Sts2.Core.Nodes.Screens.MainMenu.NJoinFriendScreen"
            );
            var joinButtonType = sts2Asm.GetType(
                "MegaCrit.Sts2.Core.Nodes.Screens.MainMenu.NJoinFriendButton"
            );
            var eNetConnType = sts2Asm.GetType(
                "MegaCrit.Sts2.Core.Multiplayer.Connection.ENetClientConnectionInitializer"
            );
            var taskHelperType = sts2Asm.GetType("MegaCrit.Sts2.Core.Helpers.TaskHelper");
            var megaLabelType = sts2Asm.GetType("MegaCrit.Sts2.addons.mega_text.MegaLabel");
            var hostServiceType = sts2Asm.GetType(
                "MegaCrit.Sts2.Core.Multiplayer.NetHostGameService"
            );
            var activeScreenCtxType = sts2Asm.GetType(
                "MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext.ActiveScreenContext"
            );

            if (joinScreenType == null || joinButtonType == null || eNetConnType == null)
            {
                PatchHelper.Log("LAN: Required types not found, skipping");
                return;
            }

            _buttonContainerField = AccessTools.Field(joinScreenType, "_buttonContainer");
            _loadingOverlayField = AccessTools.Field(joinScreenType, "_loadingOverlay");
            _noFriendsLabelField = AccessTools.Field(joinScreenType, "_noFriendsLabel");
            _loadingIndicatorField = AccessTools.Field(joinScreenType, "_loadingFriendsIndicator");

            _joinFriendButtonCreate = joinButtonType?.GetMethod(
                "Create",
                BindingFlags.Public | BindingFlags.Static
            );
            _eNetClientConnInitCtor = eNetConnType?.GetConstructor(
                new[] { typeof(ulong), typeof(string), typeof(ushort) }
            );
            _joinGameAsyncMethod = joinScreenType?.GetMethod(
                "JoinGameAsync",
                BindingFlags.Public | BindingFlags.Instance
            );
            _taskHelperRunSafely = taskHelperType?.GetMethod(
                "RunSafely",
                BindingFlags.Public | BindingFlags.Static
            );
            _setTextAutoSize = megaLabelType?.GetMethod(
                "SetTextAutoSize",
                BindingFlags.Public | BindingFlags.Instance
            );
            _activeScreenContextInstance = activeScreenCtxType?.GetProperty(
                "Instance",
                BindingFlags.Public | BindingFlags.Static
            );
            _activeScreenContextUpdate = activeScreenCtxType?.GetMethod(
                "Update",
                BindingFlags.Public | BindingFlags.Instance
            );

            if (
                _joinFriendButtonCreate == null
                || _eNetClientConnInitCtor == null
                || _joinGameAsyncMethod == null
            )
            {
                PatchHelper.Log("LAN: Critical reflection targets not found, skipping");
                return;
            }

            var patcherType = typeof(LanMultiplayerPatcher);

            // Add LAN UI elements to the join screen.
            var readyMethod = joinScreenType.GetMethod(
                "_Ready",
                BindingFlags.Public | BindingFlags.Instance
            );
            harmony.Patch(
                readyMethod,
                postfix: new HarmonyMethod(
                    patcherType.GetMethod(
                        nameof(JoinScreenReadyPostfix),
                        BindingFlags.Public | BindingFlags.Static
                    )
                )
            );

            // Replace the friend list with LAN discovery on screen open.
            var openedMethod = joinScreenType.GetMethod(
                "OnSubmenuOpened",
                BindingFlags.Public | BindingFlags.Instance
            );
            harmony.Patch(
                openedMethod,
                prefix: new HarmonyMethod(
                    patcherType.GetMethod(
                        nameof(OnSubmenuOpenedPrefix),
                        BindingFlags.Public | BindingFlags.Static
                    )
                )
            );

            // Stop LAN discovery when leaving the join screen.
            var closedMethod = joinScreenType.GetMethod(
                "OnSubmenuClosed",
                BindingFlags.Public | BindingFlags.Instance
            );
            harmony.Patch(
                closedMethod,
                postfix: new HarmonyMethod(
                    patcherType.GetMethod(
                        nameof(JoinScreenClosedPostfix),
                        BindingFlags.Public | BindingFlags.Static
                    )
                )
            );

            // Start the LAN beacon when hosting a game.
            if (hostServiceType != null)
            {
                var startHostMethod = hostServiceType.GetMethod(
                    "StartENetHost",
                    BindingFlags.Public | BindingFlags.Instance
                );
                if (startHostMethod != null)
                {
                    harmony.Patch(
                        startHostMethod,
                        postfix: new HarmonyMethod(
                            patcherType.GetMethod(
                                nameof(StartENetHostPostfix),
                                BindingFlags.Public | BindingFlags.Static
                            )
                        )
                    );
                }

                // Stop the LAN beacon on disconnect.
                var disconnectMethod = hostServiceType.GetMethod(
                    "Disconnect",
                    BindingFlags.Public | BindingFlags.Instance
                );
                if (disconnectMethod != null)
                {
                    harmony.Patch(
                        disconnectMethod,
                        postfix: new HarmonyMethod(
                            patcherType.GetMethod(
                                nameof(DisconnectPostfix),
                                BindingFlags.Public | BindingFlags.Static
                            )
                        )
                    );
                }
            }

            // Replace debug player names with numbered player names.
            var nullStrategyType = sts2Asm.GetType(
                "MegaCrit.Sts2.Core.Platform.Null.NullPlatformUtilStrategy"
            );
            if (nullStrategyType != null)
            {
                var getPlayerNameMethod = nullStrategyType.GetMethod(
                    "GetPlayerName",
                    BindingFlags.Public | BindingFlags.Instance
                );
                if (getPlayerNameMethod != null)
                {
                    harmony.Patch(
                        getPlayerNameMethod,
                        prefix: new HarmonyMethod(
                            patcherType.GetMethod(
                                nameof(GetPlayerNamePrefix),
                                BindingFlags.Public | BindingFlags.Static
                            )
                        )
                    );
                }
            }

            PatchHelper.Log("LAN multiplayer patches applied (6)");
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"LAN patch failed: {ex}");
        }
    }

    private static void JoinScreenReadyPostfix(object __instance)
    {
        try
        {
            var screen = (Node)__instance;

            var noFriendsLabel = _noFriendsLabelField?.GetValue(__instance);
            _ipLineEdit = ApplyJoinScreenUi(
                screen,
                noFriendsLabel,
                () => OnManualJoinPressed(__instance)
            );

            PatchHelper.Log("Join screen UI patched for LAN");
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"JoinScreenReadyPostfix error: {ex}");
        }
    }

    private static bool OnSubmenuOpenedPrefix(object __instance)
    {
        try
        {
            _joinInProgress = false;
            OpenDiscovery(__instance);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"OnSubmenuOpenedPrefix error: {ex}");
        }
        return false;
    }

    private static void JoinScreenClosedPostfix()
    {
        try
        {
            _joinInProgress = false;
            CloseDiscovery();
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"JoinScreenClosedPostfix error: {ex}");
        }
    }

    private static void StartENetHostPostfix(object __result)
    {
        try
        {
            if (__result != null)
                return;

            StopHostBeacon();
            _hostBeacon = new LanBeacon();
            _hostBeacon.Start();
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"StartENetHostPostfix error: {ex}");
        }
    }

    private static void DisconnectPostfix()
    {
        try
        {
            StopHostBeacon();
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"DisconnectPostfix error: {ex}");
        }
    }

    private static bool GetPlayerNamePrefix(ulong playerId, ref string __result)
    {
        try
        {
            __result = playerId switch
            {
                HostPlayerId => "Player1 (Host)",
                Player2Id => "Player2",
                Player3Id => "Player3",
                Player4Id => "Player4",
                _ => $"Player{playerId}",
            };
            return false;
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"LAN player name fallback to original failed path: {ex.Message}");
            return true; // fall through to original on error
        }
    }

    private static void StopHostBeacon()
    {
        _hostBeacon?.Stop();
        _hostBeacon = null;
    }

    private static LineEdit ApplyJoinScreenUi(
        Node screen,
        object noFriendsLabel,
        Action onManualJoinPressed)
    {
        var titleLabel = screen.GetNode("TitleLabel");
        _setTextAutoSize?.Invoke(titleLabel, new object[] { "JOIN LAN GAME" });

        if (noFriendsLabel != null)
            _setTextAutoSize?.Invoke(
                noFriendsLabel,
                new object[] { "Searching for LAN hosts..." }
            );

        var ipEdit = CreateIpEdit();
        var joinButton = CreateJoinButton();
        var ipContainer = CreateIpContainer(ipEdit, joinButton);

        screen.AddChild(ipContainer);
        ConnectJoinActions(ipEdit, joinButton, onManualJoinPressed);

        return ipEdit;
    }

    private static LineEdit CreateIpEdit()
    {
        var ipEdit = new LineEdit();
        ipEdit.PlaceholderText = "Enter host IP address";
        ipEdit.Text = LoadLastIp();
        ipEdit.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        ipEdit.AddThemeFontSizeOverride("font_size", EntryFontSize);
        return ipEdit;
    }

    private static Button CreateJoinButton()
    {
        var joinButton = new Button();
        joinButton.Text = "JOIN";
        joinButton.CustomMinimumSize = new Vector2(100, 0);
        joinButton.AddThemeFontSizeOverride("font_size", EntryFontSize);
        return joinButton;
    }

    private static HBoxContainer CreateIpContainer(LineEdit ipEdit, Button joinButton)
    {
        var ipContainer = new HBoxContainer();
        ipContainer.Name = "LanIpEntry";
        ipContainer.AddThemeConstantOverride("separation", ContainerSeparation);
        ipContainer.AnchorLeft = 0.15f;
        ipContainer.AnchorRight = 0.85f;
        ipContainer.AnchorTop = 1.0f;
        ipContainer.AnchorBottom = 1.0f;
        ipContainer.OffsetTop = -100;
        ipContainer.OffsetBottom = -20;
        ipContainer.AddChild(ipEdit);
        ipContainer.AddChild(joinButton);
        return ipContainer;
    }

    private static void ConnectJoinActions(
        LineEdit ipEdit,
        Button joinButton,
        Action onManualJoinPressed)
    {
        joinButton.Connect("pressed", Callable.From(onManualJoinPressed));
        ipEdit.Connect(
            "text_submitted",
            Callable.From<string>(_ => onManualJoinPressed())
        );
    }

    private static void OnManualJoinPressed(object screen)
    {
        if (_ipLineEdit == null || !GodotObject.IsInstanceValid(_ipLineEdit))
            return;

        var raw = _ipLineEdit.Text.Trim();
        if (string.IsNullOrEmpty(raw))
            return;

        var (ip, port) = ParseIpPort(raw);
        SaveLastIp(raw);
        JoinViaIp(screen, ip, port);
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

    private static void JoinViaIp(object screen, string ip, int port)
    {
        if (_joinInProgress)
            return;

        _joinInProgress = true;
        try
        {
            var connInit = _eNetClientConnInitCtor.Invoke(
                new object[] { Player2Id, ip, (ushort)port }
            );
            var task = _joinGameAsyncMethod.Invoke(screen, new object[] { connInit });
            _taskHelperRunSafely?.Invoke(null, new object[] { task });

            PatchHelper.Log($"Joining LAN game at {ip}:{port}");
        }
        catch (Exception ex)
        {
            _joinInProgress = false;
            PatchHelper.Log($"JoinViaIp error: {ex}");
        }
    }

    private static (string ip, int port) ParseIpPort(string input)
    {
        if (input.Contains(':'))
        {
            var parts = input.Split(':');
            if (
                parts.Length == 2
                && int.TryParse(parts[1], out int port)
                && port >= MinPort
                && port <= MaxPort
            )
                return (parts[0], port);
        }

        return (input, GamePort);
    }

    private static string LoadLastIp()
    {
        try
        {
            var config = new ConfigFile();
            if (config.Load(LastIpConfigPath) == Error.Ok)
                return (string)config.GetValue(LastIpSection, LastIpKey, "");
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"Failed to load LAN last IP config: {ex.Message}");
        }

        return "";
    }

    private static void SaveLastIp(string ip)
    {
        try
        {
            var config = new ConfigFile();
            config.SetValue(LastIpSection, LastIpKey, ip);
            config.Save(LastIpConfigPath);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"Failed to save LAN last IP config: {ex.Message}");
        }
    }

    private static void UpdateScreenContext()
    {
        try
        {
            var instance = _activeScreenContextInstance?.GetValue(null);
            _activeScreenContextUpdate?.Invoke(instance, null);
        }
        catch (Exception ex)
        {
            if (!_screenContextUpdateFailureLogged)
            {
                _screenContextUpdateFailureLogged = true;
                PatchHelper.Log($"LAN screen context update failed: {ex.Message}");
            }
        }
    }

    private sealed class LanBeacon
    {
        private readonly object _stateLock = new();
        private volatile bool _running;
        private Thread _thread;
        private UdpClient _udpClient;

        internal void Start()
        {
            lock (_stateLock)
            {
                if (_running)
                    return;

                _running = true;
                try
                {
                    _udpClient = new UdpClient();
                    _udpClient.EnableBroadcast = true;
                }
                catch (Exception ex)
                {
                    _running = false;
                    PatchHelper.Log($"LAN beacon failed to start: {ex.Message}");
                    return;
                }

                _thread = new Thread(SendLoop)
                {
                    IsBackground = true,
                    Name = "LanBeacon",
                };
                _thread.Start();
                PatchHelper.Log("LAN beacon started");
            }
        }

        private void SendLoop()
        {
            var endpoint = new IPEndPoint(IPAddress.Broadcast, BeaconPort);
            var message = $"{BeaconPrefix}|{GetDeviceHostname()}|{GamePort}";
            var packet = Encoding.UTF8.GetBytes(message);

            while (true)
            {
                if (!_running || _udpClient == null)
                    break;

                try
                {
                    _udpClient.Send(packet, packet.Length, endpoint);
                }
                catch when (!_running)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    PatchHelper.Log($"Beacon send error: {ex.Message}");
                }

                for (var i = 0; i < BeaconChecksPerSendInterval && _running; i++)
                    Thread.Sleep(BeaconSendLoopSleepMs);
            }
        }

        private static string GetDeviceHostname()
        {
            try
            {
                return Dns.GetHostName();
            }
            catch
            {
                return "Android";
            }
        }

        internal void Stop()
        {
            lock (_stateLock)
            {
                if (!_running)
                {
                    _udpClient = null;
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
                PatchHelper.Log($"LAN beacon close failed: {ex.Message}");
            }
            _udpClient = null;
            _thread?.Join(500);
            PatchHelper.Log("LAN beacon stopped");
        }
    }


    private sealed class LanDiscovery
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
                catch (SocketException) when (!_running)
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
                button.Connect(
                    "Released",
                    Callable.From<Control>(_ =>
                    {
                        SaveLastIp(capturedIp);
                        JoinViaIp(_screen, capturedIp, capturedPort);
                    })
                );

                _hostButtons[ip] = button;
                PatchHelper.Log($"Discovered LAN host: {hostname} @ {ip}:{port}");
            }
            catch (Exception ex)
            {
                PatchHelper.Log($"AddHostButton error: {ex.Message}");
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



