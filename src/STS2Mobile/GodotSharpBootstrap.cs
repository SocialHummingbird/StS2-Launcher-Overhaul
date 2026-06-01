using System;
using System.Runtime.InteropServices;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;

namespace STS2Mobile;

internal static class GodotSharpBootstrap
{
    internal static void Initialize(
        IntPtr godotDllHandle,
        IntPtr outManagedCallbacks,
        IntPtr unmanagedCallbacks,
        int unmanagedCallbacksSize
    )
    {
        DllImportResolver dllImportResolver = new GodotDllImportResolver(godotDllHandle).OnResolveDllImport;
        var coreApiAssembly = typeof(GodotObject).Assembly;
        NativeLibrary.SetDllImportResolver(coreApiAssembly, dllImportResolver);

        NativeFuncs.Initialize(unmanagedCallbacks, unmanagedCallbacksSize);
        ManagedCallbacks.Create(outManagedCallbacks);
    }
}
