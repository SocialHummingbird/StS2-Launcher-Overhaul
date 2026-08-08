using System;
using Godot;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class ActionSection
{
    private const string CompactActionButtonBodyName = "CompactActionButtonBody";
    private const string CompactActionButtonTitleName = "CompactActionButtonTitle";
    private const string CompactActionButtonDetailName = "CompactActionButtonDetail";

    private static readonly CompactButtonDetailLabelSpec CompactActionButtonLabels =
        CompactButtonDetailLabelSpec.Default(
            CompactActionButtonBodyName,
            CompactActionButtonTitleName,
            CompactActionButtonDetailName
        );

    private Button AddCompactSupportToolButton(
        Container parent,
        string text,
        float scale,
        Action pressed,
        string detail = null
    )
    {
        var button = AddHiddenButton(
            parent,
            string.IsNullOrEmpty(detail) ? text : CompactSupportToolText(text, detail),
            scale,
            LauncherSectionMetrics.CompactSupportToolFontSize,
            LauncherSectionMetrics.CompactSupportToolHeight,
            pressed
        );
        SetCompactActionButtonText(button, button.Text);
        return button;
    }

    private static string CompactSupportToolText(string text, string detail)
        => $"{text}\n{detail}";

    private void SetCompactActionButtonText(Button button, string text)
        => CompactButtonDetailLabels.Apply(
            button,
            text,
            _scale,
            _compact,
            CompactActionButtonLabels
        );
}
