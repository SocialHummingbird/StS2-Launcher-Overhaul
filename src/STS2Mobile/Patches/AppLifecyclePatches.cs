using System;
using System.Reflection;
using Godot;
using HarmonyLib;

namespace STS2Mobile.Patches;

// Handles app backgrounding and foregrounding. Mutes audio, pauses the scene
// tree, and opens the pause menu on resume.
internal static partial class AppLifecyclePatches
{
    private const int PauseMenuValue = 4;

    internal static void Apply(Harmony harmony)
    {
        var bgHandlerType = typeof(MegaCrit.Sts2.Core.Nodes.NGame).Assembly.GetType(
            "MegaCrit.Sts2.Core.Nodes.NBackgroundModeHandler"
        );
        if (bgHandlerType != null)
        {
            PatchHelper.Patch(
                harmony,
                bgHandlerType,
                "EnterBackgroundMode",
                postfix: PatchHelper.Method(
                    typeof(AppLifecyclePatches),
                    nameof(EnterBackgroundPostfix)
                )
            );

            PatchHelper.Patch(
                harmony,
                bgHandlerType,
                "ExitBackgroundMode",
                prefix: PatchHelper.Method(
                    typeof(AppLifecyclePatches),
                    nameof(ExitBackgroundPrefix)
                )
            );
        }

        // Let NGame.Quit perform its final local saves, then return to the launcher.
        PatchHelper.Patch(
            harmony,
            typeof(MegaCrit.Sts2.Core.Nodes.NGame),
            "Quit",
            postfix: PatchHelper.Method(typeof(AppLifecyclePatches), nameof(QuitPostfix))
        );
    }

    private static void EnterBackgroundPostfix(object __instance)
    {
        try
        {
            MuteFmodAudio();

            int masterBus = AudioServer.GetBusIndex("Master");
            AudioServer.SetBusMute(masterBus, true);

            var node = (Node)__instance;
            node.GetTree().Paused = true;

            PatchHelper.Log("App backgrounded: audio muted, SceneTree paused");
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"EnterBackgroundPostfix failed: {ex.Message}");
        }
    }

    // Opens the pause menu on resume so the player can re-orient before gameplay continues.
    private static bool ExitBackgroundPrefix(object __instance)
    {
        try
        {
            var node = (Node)__instance;
            var tree = node.GetTree();

            if (!tree.Paused)
                return true;

            // Show pause menu while tree is still paused so it renders on the first visible frame
            TryOpenPauseMenu();

            tree.Paused = false;

            // Restore FMOD and Godot audio to user's saved volume levels
            int masterBus = AudioServer.GetBusIndex("Master");
            AudioServer.SetBusMute(masterBus, false);
            RestoreFmodAudio();

            PatchHelper.Log("App resumed: SceneTree unpaused, audio restored");

            var isBackgroundedField = AccessTools.Field(__instance.GetType(), "_isBackgrounded");
            var savedFpsField = AccessTools.Field(__instance.GetType(), "_savedMaxFps");

            if ((bool)isBackgroundedField.GetValue(__instance))
            {
                isBackgroundedField.SetValue(__instance, false);
                Engine.MaxFps = (int)savedFpsField.GetValue(__instance);
            }

            return false;
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"ExitBackgroundPrefix failed: {ex.Message}");
            return true;
        }
    }

    // NGame.Quit has completed its synchronous final saves before this runs.
    private static void QuitPostfix()
    {
        try
        {
            PatchHelper.Log("NGame.Quit completed final local saves; restarting launcher");
            AndroidGodotAppBridge.RestartApp();
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"Launcher restart after NGame.Quit failed: {ex.Message}");
        }
    }

    private static void TryOpenPauseMenu()
    {
        try
        {
            var submenuStack = FindSubmenuStack();
            if (submenuStack == null || HasCurrentCapstoneScreen())
                return;

            var sts2Asm = typeof(MegaCrit.Sts2.Core.Nodes.NGame).Assembly;
            var enumType = sts2Asm.GetType(
                "MegaCrit.Sts2.Core.Nodes.Screens.CapstoneSubmenuType"
            );
            var pauseMenuVal = Enum.ToObject(enumType, PauseMenuValue);
            var showScreen = submenuStack
                .GetType()
                .GetMethod("ShowScreen", BindingFlags.Public | BindingFlags.Instance);
            showScreen?.Invoke(submenuStack, new object[] { pauseMenuVal });
            PatchHelper.Log("Opened pause menu on resume");
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"Failed to open pause menu: {ex.Message}");
        }
    }

    private static object FindSubmenuStack()
    {
        var nGameInstance = MegaCrit.Sts2.Core.Nodes.NGame.Instance;
        if (nGameInstance == null)
            return null;

        var currentRunNode = typeof(MegaCrit.Sts2.Core.Nodes.NGame)
            .GetProperty("CurrentRunNode", BindingFlags.Public | BindingFlags.Instance)
            ?.GetValue(nGameInstance);
        if (currentRunNode == null)
            return null;

        var globalUi = currentRunNode
            .GetType()
            .GetProperty("GlobalUi", BindingFlags.Public | BindingFlags.Instance)
            ?.GetValue(currentRunNode);
        if (globalUi == null)
            return null;

        return globalUi
            .GetType()
            .GetProperty("SubmenuStack", BindingFlags.Public | BindingFlags.Instance)
            ?.GetValue(globalUi);
    }

    private static bool HasCurrentCapstoneScreen()
    {
        var sts2Asm = typeof(MegaCrit.Sts2.Core.Nodes.NGame).Assembly;
        var capContainerType = sts2Asm.GetType(
            "MegaCrit.Sts2.Core.Nodes.Screens.Capstones.NCapstoneContainer"
        );
        var capInstance = capContainerType
            .GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)
            ?.GetValue(null);
        var currentScreen = capContainerType
            ?.GetProperty("CurrentCapstoneScreen", BindingFlags.Public | BindingFlags.Instance)
            ?.GetValue(capInstance);

        return currentScreen != null;
    }

}
