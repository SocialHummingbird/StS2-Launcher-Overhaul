using System;
using Godot;
using STS2Mobile.Launcher.Components;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static class LauncherStartupStatus
{
    private const string NodeName = "STS2MobileStartupStatus";
    private const string MessageNodeName = "STS2MobileStartupStatusMessage";
    private const int ZIndex = 4096;
    private const float ReferenceShortEdge = 720f;
    private const float AndroidMinimumScale = 1.06f;
    private const float AndroidMaximumScale = 1.38f;
    private const float AndroidWidthRatio = 0.94f;
    private const int AndroidPanelRadius = 8;
    private const int AndroidTitleFontSize = 12;
    private const int AndroidMessageFontSize = 18;
    private const int AndroidPanelHorizontalMargin = 14;
    private const int AndroidPanelVerticalMargin = 12;
    private const int AndroidPanelSeparation = 5;
    private const int AndroidPanelHeight = 98;

    internal static Label CreateLabel(Node parent)
    {
        try
        {
            var viewportSize = parent.GetViewport()?.GetVisibleRect().Size
                ?? new Vector2(1920, 1080);
            if (OperatingSystem.IsAndroid())
                return CreateAndroidStatusCard(parent, viewportSize);

            return CreateLegacyLabel(parent, viewportSize);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"Startup status label creation failed: {ex.Message}");
            return null;
        }
    }

    private static Label CreateLegacyLabel(Node parent, Vector2 viewportSize)
    {
        var margin = CalculateSafeMargin(viewportSize);
        var fontSize = CalculateFontSize(viewportSize);
        var label = new Label
        {
            Name = NodeName,
            ZIndex = ZIndex,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
        };
        label.SetAnchorsPreset(Control.LayoutPreset.TopWide);
        label.OffsetLeft = margin;
        label.OffsetTop = margin;
        label.OffsetRight = -margin;
        label.OffsetBottom = margin + fontSize * 3.2f;
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", new Color(0.55f, 0.85f, 1f));
        parent.AddChild(label);
        return label;
    }

    private static Label CreateAndroidStatusCard(Node parent, Vector2 viewportSize)
    {
        var scale = CalculateAndroidScale(viewportSize);
        var margin = CalculateSafeMargin(viewportSize);
        var shell = new MarginContainer
        {
            Name = NodeName,
            ZIndex = ZIndex,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        shell.SetAnchorsPreset(Control.LayoutPreset.TopWide);
        shell.OffsetLeft = margin;
        shell.OffsetTop = margin;
        shell.OffsetRight = -margin;
        shell.OffsetBottom = margin + LauncherComponentTheme.ScaleInt(scale, AndroidPanelHeight);

        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(CalculateAndroidPanelWidth(viewportSize, margin), 0),
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        panel.AddThemeStyleboxOverride(LauncherComponentTheme.Panel, BuildAndroidPanelStyle(scale));
        shell.AddChild(panel);

        var content = new VBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        content.AddThemeConstantOverride(
            LauncherComponentTheme.ThemeSeparation,
            LauncherComponentTheme.ScaleInt(scale, AndroidPanelSeparation)
        );
        panel.AddChild(content);

        content.AddChild(CreateAndroidTitleLabel(scale));
        var message = CreateAndroidMessageLabel(scale);
        content.AddChild(message);

        parent.AddChild(shell);
        return message;
    }

    internal static void Set(Label label, string message)
    {
        PatchHelper.Log($"[Startup] {message}");
        if (label == null)
            return;

        try
        {
            label.Text = message;
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"Startup status label update failed: {ex.Message}");
        }
    }

    internal static bool QueueFree(Label label)
    {
        var target = FindStatusRoot(label);
        if (target == null)
            return true;

        try
        {
            target.QueueFree();
            return true;
        }
        catch (ObjectDisposedException)
        {
            PatchHelper.Log("Startup status already disposed");
            return true;
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"Startup status cleanup failed: {ex.Message}");
            return false;
        }
    }

    private static Node FindStatusRoot(Label label)
    {
        if (label == null)
            return null;

        for (Node current = label; current != null; current = current.GetParent())
        {
            if (current.Name == NodeName)
                return current;
        }

        return label;
    }

    private static int CalculateFontSize(Vector2 viewportSize)
    {
        var shortEdge = Math.Min(viewportSize.X, viewportSize.Y);
        return (int)Math.Clamp(shortEdge / 42f, 18f, 28f);
    }

    private static float CalculateAndroidScale(Vector2 viewportSize)
    {
        var shortEdge = Math.Max(1f, Math.Min(viewportSize.X, viewportSize.Y));
        return Math.Clamp(shortEdge / ReferenceShortEdge, AndroidMinimumScale, AndroidMaximumScale);
    }

    private static float CalculateAndroidPanelWidth(Vector2 viewportSize, float margin)
    {
        var safeWidth = Math.Max(1f, viewportSize.X - margin * 2f);
        return safeWidth * AndroidWidthRatio;
    }

    private static float CalculateSafeMargin(Vector2 viewportSize)
    {
        var shortEdge = Math.Min(viewportSize.X, viewportSize.Y);
        return Math.Clamp(shortEdge * 0.035f, 16f, 48f);
    }

    private static Label CreateAndroidTitleLabel(float scale)
    {
        var label = new Label
        {
            Text = "STARTING GAME",
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        label.AddThemeFontSizeOverride(
            LauncherComponentTheme.FontSize,
            LauncherComponentTheme.ScaleInt(scale, AndroidTitleFontSize)
        );
        label.AddThemeColorOverride("font_color", LauncherComponentTheme.OrangeHot);
        return label;
    }

    private static Label CreateAndroidMessageLabel(float scale)
    {
        var label = new Label
        {
            Name = MessageNodeName,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
        };
        label.AddThemeFontSizeOverride(
            LauncherComponentTheme.FontSize,
            LauncherComponentTheme.ScaleInt(scale, AndroidMessageFontSize)
        );
        label.AddThemeColorOverride("font_color", LauncherComponentTheme.TextPrimary);
        return label;
    }

    private static StyleBoxFlat BuildAndroidPanelStyle(float scale)
    {
        var style = LauncherStyleBoxes.MakeFilled(
            new Color(
                LauncherComponentTheme.PanelBackground.R,
                LauncherComponentTheme.PanelBackground.G,
                LauncherComponentTheme.PanelBackground.B,
                0.92f
            ),
            LauncherComponentTheme.ScaleInt(scale, AndroidPanelRadius)
        );
        style.BorderColor = LauncherComponentTheme.CyanDim;
        style.SetBorderWidthAll(Math.Max(1, LauncherComponentTheme.ScaleInt(scale, 1)));
        style.ContentMarginLeft = LauncherComponentTheme.ScaleInt(scale, AndroidPanelHorizontalMargin);
        style.ContentMarginRight = LauncherComponentTheme.ScaleInt(scale, AndroidPanelHorizontalMargin);
        style.ContentMarginTop = LauncherComponentTheme.ScaleInt(scale, AndroidPanelVerticalMargin);
        style.ContentMarginBottom = LauncherComponentTheme.ScaleInt(scale, AndroidPanelVerticalMargin);
        return style;
    }
}
