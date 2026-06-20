using Godot;

namespace STS2Mobile.Launcher.Sections;

internal static partial class CompactButtonDetailLabels
{
    internal static void Apply(
        Button button,
        string text,
        float scale,
        bool enabled,
        CompactButtonDetailLabelSpec spec
    )
    {
        if (!enabled || !TrySplitText(text, out var title, out var detail))
        {
            Hide(button, spec);
            button.Text = text;
            return;
        }

        var labels = Ensure(button, scale, spec);
        button.Text = "";
        labels.Body.Visible = true;
        labels.Title.Text = title;
        labels.Detail.Text = detail;
    }

    private static void Hide(Button button, CompactButtonDetailLabelSpec spec)
    {
        var body = button.GetNodeOrNull<VBoxContainer>(new NodePath(spec.BodyName));
        if (body != null)
            body.Visible = false;
    }
}
