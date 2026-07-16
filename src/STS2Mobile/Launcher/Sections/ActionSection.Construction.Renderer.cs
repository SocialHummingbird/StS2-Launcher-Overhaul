using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class ActionSection
{
    private (
        VBoxContainer Group,
        Button AutoButton,
        Button VulkanButton,
        Button OpenGlButton
    ) BuildRendererControls(float scale, bool compact)
    {
        var group = new VBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        group.AddThemeConstantOverride(
            "separation",
            LauncherViewLayoutMetrics.ScaleInt(compact ? 6 : 8, scale)
        );

        var label = new StyledLabel(
            "Graphics renderer",
            scale,
            compact ? 13 : 14,
            HorizontalAlignment.Left
        );
        label.AddThemeColorOverride("font_color", LauncherComponentTheme.TextSecondary);
        group.AddChild(label);

        var row = new HBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        row.AddThemeConstantOverride(
            "separation",
            LauncherViewLayoutMetrics.ScaleInt(6, scale)
        );
        group.AddChild(row);

        var buttonGroup = new ButtonGroup
        {
            AllowUnpress = false,
        };
        var auto = AddRendererButton(row, buttonGroup, "Auto", LauncherRendererMode.Auto, scale, compact);
        var vulkan = AddRendererButton(row, buttonGroup, "Vulkan", LauncherRendererMode.Vulkan, scale, compact);
        var openGl = AddRendererButton(row, buttonGroup, "OpenGL", LauncherRendererMode.OpenGl, scale, compact);

        return (group, auto, vulkan, openGl);
    }

    private Button AddRendererButton(
        Container parent,
        ButtonGroup buttonGroup,
        string text,
        string mode,
        float scale,
        bool compact
    )
    {
        var button = new StyledButton(
            text,
            scale,
            compact ? 13 : 14,
            compact
                ? LauncherSectionMetrics.CompactDrawerToggleHeight
                : LauncherSectionMetrics.SecondaryButtonHeight
        )
        {
            ToggleMode = true,
            ButtonGroup = buttonGroup,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            AccessibilityName = $"Renderer {text}",
            AccessibilityDescription = $"Use {text} for normal game starts.",
        };
        LauncherButtonStyles.ApplySupportAction(button, scale);
        button.Pressed += () => ApplyRendererMode(mode, notify: true);
        parent.AddChild(button);
        return button;
    }
}
