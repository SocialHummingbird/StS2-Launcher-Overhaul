using System;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;

namespace STS2Mobile.Patches;

// Android can reach NDevConsole._Ready before the desktop-only debug console
// dependencies are fully available. If its NullReferenceException escapes
// Godot's C# bridge, Godot tears down the process and aborts. Keep the console
// behavior when it initializes successfully, but suppress the known Android
// initialization failure so the main menu can continue loading.
internal static class DevConsolePatches
{
    private const BindingFlags AllFlags =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    private const string ReadyMethod = "_Ready";
    private const string TypeName = "MegaCrit.Sts2.Core.Nodes.Debug.NDevConsole";

    private static bool _loggedFirstSuppression;

    internal static void Apply(Harmony harmony)
    {
        var target = FindTarget();
        if (target == null)
            return;

        harmony.Patch(
            target,
            finalizer: new HarmonyMethod(
                typeof(DevConsolePatches).GetMethod(nameof(ReadyFinalizer), AllFlags)
            )
        );
        PatchHelper.Log("Patched NDevConsole._Ready finalizer");
    }

    private static MethodInfo FindTarget()
    {
        var type = typeof(NGame).Assembly.GetType(TypeName);
        if (type == null)
        {
            PatchHelper.Log("DevConsolePatches: NDevConsole type not found; skipping");
            return null;
        }

        var method = type.GetMethod(ReadyMethod, AllFlags);
        if (method == null)
            PatchHelper.Log("DevConsolePatches: NDevConsole._Ready method not found; skipping");

        return method;
    }

    private static Exception ReadyFinalizer(Exception __exception)
    {
        if (__exception == null)
            return null;

        if (!_loggedFirstSuppression)
        {
            _loggedFirstSuppression = true;
            PatchHelper.Log(
                $"DevConsolePatches: suppressed {__exception.GetType().Name} from NDevConsole._Ready "
                    + $"({__exception.Message}); further occurrences silenced"
            );
        }

        return null;
    }
}
