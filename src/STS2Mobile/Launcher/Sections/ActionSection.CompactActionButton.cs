using System;
using Godot;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class ActionSection
{
    private const string CompactActionButtonBodyName = "CompactActionButtonBody";
    private const string CompactActionButtonTitleName = "CompactActionButtonTitle";
    private const string CompactActionButtonDetailName = "CompactActionButtonDetail";
    private const int CompactActionButtonTitleFontSize = LauncherSectionMetrics.CompactDetailButtonFontSize;
    private const int CompactActionButtonDetailFontSize = LauncherSectionMetrics.CompactDetailLabelFontSize;
    private const int CompactActionButtonHorizontalMargin = 6;
    private const int CompactActionButtonVerticalMargin = 4;

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
    {
        if (!_compact || !TrySplitCompactActionButtonText(text, out var title, out var detail))
        {
            HideCompactActionButtonLabels(button);
            button.Text = text;
            return;
        }

        var labels = EnsureCompactActionButtonLabels(button);
        button.Text = "";
        labels.Body.Visible = true;
        labels.Title.Text = title;
        labels.Detail.Text = detail;
    }
}
