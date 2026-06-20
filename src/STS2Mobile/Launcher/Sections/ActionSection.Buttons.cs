using System;
using Godot;
using STS2Mobile.Launcher.Components;

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

    private Button AddPrimaryHiddenButton(Container parent, string text, float scale, Action pressed)
        => AddHiddenButton(
            parent,
            text,
            scale,
            LauncherSectionMetrics.PrimaryButtonFontSize,
            LauncherSectionMetrics.PrimaryButtonHeight,
            pressed
        );

    private Button AddSecondaryHiddenButton(Container parent, string text, float scale, Action pressed)
        => AddHiddenButton(
            parent,
            text,
            scale,
            LauncherSectionMetrics.SecondaryButtonFontSize,
            LauncherSectionMetrics.SecondaryButtonHeight,
            pressed
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

    private Button AddHiddenButton(
        Container parent,
        string text,
        float scale,
        int fontSize,
        int height,
        Action pressed
    )
    {
        var button = new StyledButton(text, scale, fontSize: fontSize, height: height);
        button.Visible = false;
        button.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        if (pressed != null)
            button.Pressed += pressed;
        parent.AddChild(button);
        return button;
    }

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

    private (VBoxContainer Body, StyledLabel Title, StyledLabel Detail) EnsureCompactActionButtonLabels(Button button)
    {
        var body = button.GetNodeOrNull<VBoxContainer>(new NodePath(CompactActionButtonBodyName));
        if (body != null)
        {
            return (
                body,
                body.GetNode<StyledLabel>(new NodePath(CompactActionButtonTitleName)),
                body.GetNode<StyledLabel>(new NodePath(CompactActionButtonDetailName))
            );
        }

        body = new VBoxContainer
        {
            Name = CompactActionButtonBodyName,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        body.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        body.OffsetLeft = LauncherViewLayoutMetrics.ScaleInt(CompactActionButtonHorizontalMargin, _scale);
        body.OffsetRight = -LauncherViewLayoutMetrics.ScaleInt(CompactActionButtonHorizontalMargin, _scale);
        body.OffsetTop = LauncherViewLayoutMetrics.ScaleInt(CompactActionButtonVerticalMargin, _scale);
        body.OffsetBottom = -LauncherViewLayoutMetrics.ScaleInt(CompactActionButtonVerticalMargin, _scale);
        body.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            0
        );

        var title = new StyledLabel(
            "",
            _scale,
            fontSize: CompactActionButtonTitleFontSize,
            align: HorizontalAlignment.Center
        )
        {
            Name = CompactActionButtonTitleName,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            VerticalAlignment = VerticalAlignment.Center,
            ClipText = true,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
        };
        title.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.TextPrimary
        );
        body.AddChild(title);

        var detail = new StyledLabel(
            "",
            _scale,
            fontSize: CompactActionButtonDetailFontSize,
            align: HorizontalAlignment.Center
        )
        {
            Name = CompactActionButtonDetailName,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            VerticalAlignment = VerticalAlignment.Center,
            ClipText = true,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
        };
        detail.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.TextSecondary
        );
        body.AddChild(detail);

        button.AddChild(body);
        return (body, title, detail);
    }

    private static Button AddPushPullButton(
        Container row,
        string text,
        float scale,
        Action pressed
    )
    {
        var button = new StyledButton(
            text,
            scale,
            LauncherSectionMetrics.SecondaryButtonFontSize,
            LauncherSectionMetrics.SecondaryButtonHeight
        );
        button.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        if (pressed != null)
            button.Pressed += pressed;
        row.AddChild(button);
        return button;
    }
}
