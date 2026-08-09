using System;
using Godot;
using STS2Mobile.Launcher;

namespace STS2Mobile;

public static partial class ModEntry
{
    private static void ScheduleStandaloneLauncher()
        => ScheduleStandaloneLauncher(null);

    private static void ScheduleStandaloneLauncher(string reason)
    {
        if (!string.IsNullOrWhiteSpace(reason))
            _startupFallbackReason = reason;

        PatchHelper.Log("Scheduling standalone launcher...");
        Callable.From(CreateStandaloneLauncher).CallDeferred();
    }

    private static void CreateStandaloneLauncher()
    {
        if (Engine.GetMainLoop() is not SceneTree tree)
        {
            Callable.From(CreateStandaloneLauncher).CallDeferred();
            return;
        }

        var launcher = new LauncherUI();
        AddStartupFallbackShield(tree);
        tree.Root.AddChild(launcher);
        var launcherInitialized = launcher.Initialize();
        if (launcherInitialized)
            _ = launcher.NotifyBootTransitionWhenVisibleAsync();
        RaiseStartupFallbackLauncher(launcher);
        AddStartupFallbackBanner(tree);
        PatchHelper.Log("Standalone launcher displayed");
    }

    private static void AddStartupFallbackShield(SceneTree tree)
    {
        if (string.IsNullOrWhiteSpace(_startupFallbackReason))
            return;

        var shield = new ColorRect
        {
            Name = "STS2MobileStartupFallbackShield",
            Color = new Color(0.02f, 0.02f, 0.025f, 0.96f),
            ZIndex = StartupFallbackShieldZIndex,
            MouseFilter = Control.MouseFilterEnum.Stop,
        };
        shield.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        tree.Root.AddChild(shield);
        PatchHelper.Log("Startup fallback shield displayed");
    }

    private static void RaiseStartupFallbackLauncher(LauncherUI launcher)
    {
        if (string.IsNullOrWhiteSpace(_startupFallbackReason))
            return;

        launcher.ZIndex = StartupFallbackLauncherZIndex;
        PatchHelper.Log("Startup fallback launcher raised above shield");
    }

    private static void AddStartupFallbackBanner(SceneTree tree)
    {
        if (string.IsNullOrWhiteSpace(_startupFallbackReason))
            return;

        PatchHelper.Log(
            "Startup fallback raw banner suppressed; launcher diagnostics retain the startup failure detail."
        );
    }
}
