using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher.Sections;
internal sealed partial class ActionSection
{
    internal void SetRendererMode(string mode) => ApplyRendererMode(mode, notify: false);
    private void ApplyRendererMode(string mode, bool notify)
    {
        _rendererMode = LauncherRendererMode.Normalize(mode);
        _rendererAutoButton.ButtonPressed = _rendererMode == LauncherRendererMode.Auto;
        _rendererVulkanButton.ButtonPressed = _rendererMode == LauncherRendererMode.Vulkan;
        _rendererOpenGlButton.ButtonPressed = _rendererMode == LauncherRendererMode.OpenGl;
        ApplyRendererButtonStyle(_rendererAutoButton, _rendererMode == LauncherRendererMode.Auto);
        ApplyRendererButtonStyle(_rendererVulkanButton, _rendererMode == LauncherRendererMode.Vulkan);
        ApplyRendererButtonStyle(_rendererOpenGlButton, _rendererMode == LauncherRendererMode.OpenGl);
        if (notify)
            RendererModeChanged?.Invoke(_rendererMode);
    }

    private void ApplyRendererButtonStyle(Button button, bool selected)
    {
        if (selected)
            LauncherButtonStyles.ApplySafeAction(button, _scale);
        else
            LauncherButtonStyles.ApplySupportAction(button, _scale);
    }

    private (VBoxContainer Group, Button AutoButton, Button VulkanButton, Button OpenGlButton) BuildRendererControls(float scale, bool compact)
    {
        var group = new VBoxContainer
        {
            Name = "GraphicsSection",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        group.AddThemeConstantOverride("separation", LauncherViewLayoutMetrics.ScaleInt(compact ? 6 : 8, scale));
        var label = new StyledLabel("Graphics", scale, compact ? 13 : 14, HorizontalAlignment.Left);
        label.Name = "GraphicsSectionLabel";
        label.AddThemeColorOverride("font_color", LauncherComponentTheme.TextSecondary);
        group.AddChild(label);
        var row = new HBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        row.AddThemeConstantOverride("separation", LauncherViewLayoutMetrics.ScaleInt(6, scale));
        group.AddChild(row);
        var buttonGroup = new ButtonGroup
        {
            AllowUnpress = false,
        };
        var auto = AddRendererButton(row, buttonGroup, "Auto", LauncherRendererMode.Auto, scale, compact);
        var vulkan = AddRendererButton(row, buttonGroup, "Vulkan", LauncherRendererMode.Vulkan, scale, compact);
        var openGl = AddRendererButton(row, buttonGroup, "OpenGL", LauncherRendererMode.OpenGl, scale, compact);
        auto.Name = "RendererAuto";
        vulkan.Name = "RendererVulkan";
        openGl.Name = "RendererOpenGL";
        return (group, auto, vulkan, openGl);
    }

    private Button AddRendererButton(Container parent, ButtonGroup buttonGroup, string text, string mode, float scale, bool compact)
    {
        var button = new StyledButton(text, scale, compact ? 13 : 14, compact ? LauncherSectionMetrics.CompactDrawerToggleHeight : LauncherSectionMetrics.SecondaryButtonHeight)
        {
            ToggleMode = true,
            ButtonGroup = buttonGroup,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            AccessibilityName = $"Renderer {text}",
            AccessibilityDescription = mode == LauncherRendererMode.Auto ? "Recommended for normal game starts." : $"Use {text} for graphics troubleshooting.",
        };
        LauncherButtonStyles.ApplySupportAction(button, scale);
        button.Pressed += () => ApplyRendererMode(mode, notify: true);
        parent.AddChild(button);
        return button;
    }
}
