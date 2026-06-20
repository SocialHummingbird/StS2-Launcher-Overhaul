using Godot;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    private void UpdateCompactStickyTaskHeader(Vector2 viewportSize)
    {
        if (!_profile.Compact
            || !GodotObject.IsInstanceValid(_compactStickyTaskHeader)
            || !GodotObject.IsInstanceValid(_compactCurrentTaskButton)
            || !GodotObject.IsInstanceValid(_compactWorkflowStrip))
        {
            return;
        }

        var profile = viewportSize.X > 0f && viewportSize.Y > 0f
            ? LauncherLayoutProfile.ForViewport(viewportSize)
            : _profile;
        ApplyCompactStickyTaskHeaderLayout(
            _compactStickyTaskHeader,
            _compactCurrentTaskButton,
            _compactWorkflowStrip,
            profile
        );
    }

    private void UpdateCompactSectionResponsiveRows(Vector2 viewportSize)
    {
        if (!_profile.Compact || !GodotObject.IsInstanceValid(Code))
            return;

        var profile = viewportSize.X > 0f && viewportSize.Y > 0f
            ? LauncherLayoutProfile.ForViewport(viewportSize)
            : _profile;
        Code.UpdateViewportProfile(profile);
    }

    private void UpdateCompactStatusHeadline(Vector2 viewportSize)
    {
        if (!_profile.Compact
            || !GodotObject.IsInstanceValid(_compactStatusHeadline)
            || !GodotObject.IsInstanceValid(_compactStatusPhasePanel)
            || !GodotObject.IsInstanceValid(_statusActionLabel))
        {
            return;
        }

        var profile = viewportSize.X > 0f && viewportSize.Y > 0f
            ? LauncherLayoutProfile.ForViewport(viewportSize)
            : _profile;
        ApplyCompactStatusHeadlineLayout(
            _compactStatusHeadline,
            _compactStatusPhasePanel,
            _statusActionLabel,
            profile
        );
    }
}
