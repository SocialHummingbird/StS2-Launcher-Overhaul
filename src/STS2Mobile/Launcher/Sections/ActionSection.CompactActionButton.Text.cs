using Godot;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class ActionSection
{
    private static bool TrySplitCompactActionButtonText(
        string text,
        out string title,
        out string detail
    )
    {
        title = text ?? "";
        detail = "";
        var separator = title.IndexOf('\n');
        if (separator < 0)
            return false;

        detail = title[(separator + 1)..].Trim();
        title = title[..separator].Trim();
        return title.Length > 0 && detail.Length > 0;
    }

    private static void HideCompactActionButtonLabels(Button button)
    {
        var body = button.GetNodeOrNull<VBoxContainer>(new NodePath(CompactActionButtonBodyName));
        if (body != null)
            body.Visible = false;
    }
}
