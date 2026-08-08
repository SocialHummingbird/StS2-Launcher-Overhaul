using System;
using Godot;

namespace STS2Mobile.Patches;

internal static partial class LanMultiplayerPatcher
{
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
}
