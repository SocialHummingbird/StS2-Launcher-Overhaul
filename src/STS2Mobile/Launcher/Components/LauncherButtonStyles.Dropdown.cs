using Godot;

namespace STS2Mobile.Launcher.Components;

internal static partial class LauncherButtonStyles
{
    private const string PopupHover = "hover";
    private const string PopupHorizontalSeparation = "h_separation";
    private const string PopupVerticalSeparation = "v_separation";
    private const string PopupItemStartPadding = "item_start_padding";
    private const string PopupItemEndPadding = "item_end_padding";
    private const int DropdownPopupHorizontalPadding = 14;
    private const int DropdownPopupHorizontalSeparation = 8;
    private const int DropdownPopupVerticalSeparation = 8;
    private const int CompactDropdownPopupHorizontalPadding = 20;
    private const int CompactDropdownPopupHorizontalSeparation = 12;
    private const int CompactDropdownPopupVerticalSeparation = 16;

    internal static void ApplyDropdownAction(
        OptionButton button,
        float scale,
        int fontSize,
        bool compact = false
    )
    {
        ApplySupportAction(button, scale);
        var scaledFontSize = LauncherComponentTheme.ScaleInt(scale, fontSize);
        button.AddThemeFontSizeOverride(LauncherComponentTheme.FontSize, scaledFontSize);

        var popup = button.GetPopup();
        popup.AddThemeFontSizeOverride(LauncherComponentTheme.FontSize, scaledFontSize);
        popup.AddThemeConstantOverride(
            PopupVerticalSeparation,
            LauncherComponentTheme.ScaleInt(
                scale,
                compact
                    ? CompactDropdownPopupVerticalSeparation
                    : DropdownPopupVerticalSeparation
            )
        );
        popup.AddThemeConstantOverride(
            PopupHorizontalSeparation,
            LauncherComponentTheme.ScaleInt(
                scale,
                compact
                    ? CompactDropdownPopupHorizontalSeparation
                    : DropdownPopupHorizontalSeparation
            )
        );
        popup.AddThemeConstantOverride(
            PopupItemStartPadding,
            LauncherComponentTheme.ScaleInt(
                scale,
                compact
                    ? CompactDropdownPopupHorizontalPadding
                    : DropdownPopupHorizontalPadding
            )
        );
        popup.AddThemeConstantOverride(
            PopupItemEndPadding,
            LauncherComponentTheme.ScaleInt(
                scale,
                compact
                    ? CompactDropdownPopupHorizontalPadding
                    : DropdownPopupHorizontalPadding
            )
        );
        popup.AddThemeStyleboxOverride(
            LauncherComponentTheme.Panel,
            LauncherStyleBoxes.MakeFilled(
                LauncherComponentTheme.PanelBackground,
                LauncherComponentTheme.ScaleInt(scale, LauncherComponentTheme.ButtonRadius)
            )
        );
        popup.AddThemeStyleboxOverride(
            PopupHover,
            LauncherStyleBoxes.MakeFilled(
                LauncherComponentTheme.ButtonHover,
                LauncherComponentTheme.ScaleInt(scale, LauncherComponentTheme.ButtonRadius)
            )
        );
    }
}
