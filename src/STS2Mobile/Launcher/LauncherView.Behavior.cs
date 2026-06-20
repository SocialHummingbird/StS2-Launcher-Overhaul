using Godot;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    internal void UpdateViewportSize(Vector2 viewportSize)
    {
        _panelBaseY = _panel.Position.Y + _keyboardOffset;
        _panel.UpdateSizeFromViewport(viewportSize, _profile.PanelHeightRatio);
        UpdateCompactStatusHeadline(viewportSize);
        UpdateCompactStickyTaskHeader(viewportSize);
        UpdateCompactSectionResponsiveRows(viewportSize);
        UpdateDiagnosticsLogViewport(viewportSize);
        UpdateKeyboardOffset();
        ReanchorCompactScrollTargetAfterViewportChange();
    }
}
