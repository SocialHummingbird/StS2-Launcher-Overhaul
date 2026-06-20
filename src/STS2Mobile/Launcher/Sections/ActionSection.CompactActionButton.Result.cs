using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher.Sections;

internal readonly struct ActionSectionCompactActionButtonLabels
{
    internal ActionSectionCompactActionButtonLabels(
        VBoxContainer body,
        StyledLabel title,
        StyledLabel detail
    )
    {
        Body = body;
        Title = title;
        Detail = detail;
    }

    internal VBoxContainer Body { get; }
    internal StyledLabel Title { get; }
    internal StyledLabel Detail { get; }
}
