using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class ActionSection
{
    internal void SetRendererMode(string mode)
        => ApplyRendererMode(mode, notify: false);

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
}
