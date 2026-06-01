using Godot;

namespace STS2Mobile.Launcher.Components;

internal static class LauncherStyleBoxes
{
    internal static StyleBoxFlat MakeFilled(Color bg, int cornerRadius)
    {
        var style = new StyleBoxFlat();
        style.BgColor = bg;
        style.SetCornerRadiusAll(cornerRadius);
        return style;
    }

    internal static StyleBoxFlat MakeOutline(Color borderColor, int cornerRadius, int borderWidth)
    {
        var style = new StyleBoxFlat();
        style.BgColor = Colors.Transparent;
        style.BorderColor = borderColor;
        style.SetBorderWidthAll(borderWidth);
        style.SetCornerRadiusAll(cornerRadius);
        return style;
    }
}
