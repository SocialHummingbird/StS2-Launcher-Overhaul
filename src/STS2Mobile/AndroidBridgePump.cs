using System;
using Godot;
using STS2Mobile.Patches;

namespace STS2Mobile;

internal static class AndroidBridgePump
{
    private static bool _installed;

    internal static void EnsureInstalled(Node parent)
    {
        if (!OperatingSystem.IsAndroid() || _installed)
            return;

        var tree = parent.GetTree();
        if (tree == null)
        {
            AndroidBridgeDispatcher.RegisterCurrentThread();
            PatchHelper.Log("Android bridge frame pump deferred; scene tree unavailable");
            return;
        }

        AndroidBridgeDispatcher.RegisterCurrentThread();
        tree.ProcessFrame += OnProcessFrame;
        _installed = true;
        PatchHelper.Log("Android bridge frame pump installed");
    }

    private static void OnProcessFrame()
    {
        AndroidBridgeDispatcher.RegisterCurrentThread();
        AndroidBridgeDispatcher.Pump();
    }
}
