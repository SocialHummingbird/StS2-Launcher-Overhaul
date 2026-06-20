using Godot;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    private static void SetCompactSafeFlowToggleText(
        Button toggle,
        float scale,
        string title,
        string detail
    )
    {
        toggle.Text = "";
        var labels = EnsureCompactSafeFlowToggleLabels(toggle, scale);
        labels.Body.Visible = true;
        labels.Title.Text = title;
        labels.Detail.Text = detail;
    }
}
