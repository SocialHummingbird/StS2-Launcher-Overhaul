using System;
using System.Reflection;
using HarmonyLib;

namespace STS2Mobile.Patches;

internal static partial class LanMultiplayerPatcher
{
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
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static
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
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static
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
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static
                    )
                )
            );

            PatchHostService(harmony, hostServiceType, patcherType);
            PatchPlayerNameStrategy(harmony, sts2Asm, patcherType);

            PatchHelper.Log("LAN multiplayer patches applied (6)");
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"LAN patch failed: {ex}");
        }
    }
}
