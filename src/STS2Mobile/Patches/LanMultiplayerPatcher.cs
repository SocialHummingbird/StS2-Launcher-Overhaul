using System.Reflection;
using Godot;
using System;
using HarmonyLib;

namespace STS2Mobile.Patches;
// Enables LAN multiplayer by replacing the Steam friends list with UDP broadcast
// discovery. Hosts advertise via a beacon on port 33770, clients discover them
// automatically or connect manually by IP address.
internal static partial class LanMultiplayerPatcher
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
            var joinScreenType = sts2Asm.GetType("MegaCrit.Sts2.Core.Nodes.Screens.MainMenu.NJoinFriendScreen");
            var joinButtonType = sts2Asm.GetType("MegaCrit.Sts2.Core.Nodes.Screens.MainMenu.NJoinFriendButton");
            var eNetConnType = sts2Asm.GetType("MegaCrit.Sts2.Core.Multiplayer.Connection.ENetClientConnectionInitializer");
            var taskHelperType = sts2Asm.GetType("MegaCrit.Sts2.Core.Helpers.TaskHelper");
            var megaLabelType = sts2Asm.GetType("MegaCrit.Sts2.addons.mega_text.MegaLabel");
            var hostServiceType = sts2Asm.GetType("MegaCrit.Sts2.Core.Multiplayer.NetHostGameService");
            var activeScreenCtxType = sts2Asm.GetType("MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext.ActiveScreenContext");
            if (joinScreenType == null || joinButtonType == null || eNetConnType == null)
            {
                PatchHelper.Log("LAN: Required types not found, skipping");
                return;
            }

            _buttonContainerField = AccessTools.Field(joinScreenType, "_buttonContainer");
            _loadingOverlayField = AccessTools.Field(joinScreenType, "_loadingOverlay");
            _noFriendsLabelField = AccessTools.Field(joinScreenType, "_noFriendsLabel");
            _loadingIndicatorField = AccessTools.Field(joinScreenType, "_loadingFriendsIndicator");
            _joinFriendButtonCreate = joinButtonType?.GetMethod("Create", BindingFlags.Public | BindingFlags.Static);
            _eNetClientConnInitCtor = eNetConnType?.GetConstructor(new[] { typeof(ulong), typeof(string), typeof(ushort) });
            _joinGameAsyncMethod = joinScreenType?.GetMethod("JoinGameAsync", BindingFlags.Public | BindingFlags.Instance);
            _taskHelperRunSafely = taskHelperType?.GetMethod("RunSafely", BindingFlags.Public | BindingFlags.Static);
            _setTextAutoSize = megaLabelType?.GetMethod("SetTextAutoSize", BindingFlags.Public | BindingFlags.Instance);
            _activeScreenContextInstance = activeScreenCtxType?.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            _activeScreenContextUpdate = activeScreenCtxType?.GetMethod("Update", BindingFlags.Public | BindingFlags.Instance);
            if (_joinFriendButtonCreate == null || _eNetClientConnInitCtor == null || _joinGameAsyncMethod == null)
            {
                PatchHelper.Log("LAN: Critical reflection targets not found, skipping");
                return;
            }

            var patcherType = typeof(LanMultiplayerPatcher);
            // Add LAN UI elements to the join screen.
            var readyMethod = joinScreenType.GetMethod("_Ready", BindingFlags.Public | BindingFlags.Instance);
            harmony.Patch(readyMethod, postfix: new HarmonyMethod(patcherType.GetMethod(nameof(JoinScreenReadyPostfix), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)));
            // Replace the friend list with LAN discovery on screen open.
            var openedMethod = joinScreenType.GetMethod("OnSubmenuOpened", BindingFlags.Public | BindingFlags.Instance);
            harmony.Patch(openedMethod, prefix: new HarmonyMethod(patcherType.GetMethod(nameof(OnSubmenuOpenedPrefix), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)));
            // Stop LAN discovery when leaving the join screen.
            var closedMethod = joinScreenType.GetMethod("OnSubmenuClosed", BindingFlags.Public | BindingFlags.Instance);
            harmony.Patch(closedMethod, postfix: new HarmonyMethod(patcherType.GetMethod(nameof(JoinScreenClosedPostfix), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)));
            PatchHostService(harmony, hostServiceType, patcherType);
            PatchPlayerNameStrategy(harmony, sts2Asm, patcherType);
            PatchHelper.Log("LAN multiplayer patches applied (6)");
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"LAN patch failed: {ex}");
        }
    }

    private static void PatchHostService(Harmony harmony, Type hostServiceType, Type patcherType)
    {
        if (hostServiceType == null)
            return;
        var startHostMethod = hostServiceType.GetMethod("StartENetHost", BindingFlags.Public | BindingFlags.Instance);
        if (startHostMethod != null)
        {
            harmony.Patch(startHostMethod, postfix: new HarmonyMethod(patcherType.GetMethod(nameof(StartENetHostPostfix), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)));
        }

        var disconnectMethod = hostServiceType.GetMethod("Disconnect", BindingFlags.Public | BindingFlags.Instance);
        if (disconnectMethod != null)
        {
            harmony.Patch(disconnectMethod, postfix: new HarmonyMethod(patcherType.GetMethod(nameof(DisconnectPostfix), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)));
        }
    }

    private static void PatchPlayerNameStrategy(Harmony harmony, Assembly sts2Asm, Type patcherType)
    {
        var nullStrategyType = sts2Asm.GetType("MegaCrit.Sts2.Core.Platform.Null.NullPlatformUtilStrategy");
        if (nullStrategyType == null)
            return;
        var getPlayerNameMethod = nullStrategyType.GetMethod("GetPlayerName", BindingFlags.Public | BindingFlags.Instance);
        if (getPlayerNameMethod == null)
            return;
        harmony.Patch(getPlayerNameMethod, prefix: new HarmonyMethod(patcherType.GetMethod(nameof(GetPlayerNamePrefix), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)));
    }

    private static void JoinScreenReadyPostfix(object __instance)
    {
        try
        {
            var screen = (Node)__instance;
            var noFriendsLabel = _noFriendsLabelField?.GetValue(__instance);
            _ipLineEdit = ApplyJoinScreenUi(screen, noFriendsLabel, () => OnManualJoinPressed(__instance));
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
}
