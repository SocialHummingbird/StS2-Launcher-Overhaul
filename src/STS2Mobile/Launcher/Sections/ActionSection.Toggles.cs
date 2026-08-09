using Godot;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class ActionSection
{
    private void ApplyToggle(Button button, bool value, string text)
    {
        SetCompactActionButtonText(button, text);
        var style = value ? _toggleOnStyle : _toggleOffStyle;
        button.AddThemeStyleboxOverride("normal", style);
        button.AddThemeStyleboxOverride("hover", style);
        button.AddThemeStyleboxOverride("pressed", style);
        button.AddThemeStyleboxOverride("disabled", style);
    }
}
