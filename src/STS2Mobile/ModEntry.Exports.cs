using System;
using System.Runtime.InteropServices;
using HarmonyLib;

namespace STS2Mobile;

public static partial class ModEntry
{
    // Bootstraps GodotSharp by setting up DLL import resolver, native interop,
    // and managed callbacks. Called from gd_mono.cpp before Apply().
    [UnmanagedCallersOnly]
    public static int InitializeGodotSharp(
        IntPtr godotDllHandle,
        IntPtr outManagedCallbacks,
        IntPtr unmanagedCallbacks,
        int unmanagedCallbacksSize
    )
    {
        try
        {
            GodotSharpBootstrap.Initialize(
                godotDllHandle,
                outManagedCallbacks,
                unmanagedCallbacks,
                unmanagedCallbacksSize
            );
            return 1;
        }
        catch
        {
            return 0;
        }
    }

    [UnmanagedCallersOnly]
    public static void Apply()
    {
        ApplyInternal();
    }

    [UnmanagedCallersOnly]
    public static int ApplyFromGodot()
    {
        try
        {
            BootstrapTrace.Log("ApplyFromGodot entered");
            ApplyInternal();
            BootstrapTrace.Log("ApplyFromGodot completed");
            return ProbeSuccess;
        }
        catch (Exception ex)
        {
            BootstrapTrace.Log($"Unhandled bootstrap failure: {ex}");
            return ProbeFailure;
        }
    }

    [UnmanagedCallersOnly]
    public static int BootstrapProbe()
    {
        return BootstrapProbeCode;
    }

    [UnmanagedCallersOnly]
    public static int HarmonyConstructorProbe()
    {
        _ = new Harmony(HarmonyId);
        return HarmonyConstructorProbeCode;
    }

    [UnmanagedCallersOnly]
    public static int ShowLauncherOnly()
    {
        try
        {
            ScheduleStandaloneLauncher();
            return ProbeSuccessWithValue;
        }
        catch
        {
            return ProbeSuccess;
        }
    }
}
