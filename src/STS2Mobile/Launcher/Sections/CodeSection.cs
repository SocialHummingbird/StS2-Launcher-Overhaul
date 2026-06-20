using System;
using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher.Sections;

internal sealed class CodeSection : VBoxContainer
{
    private const string DefaultPrompt = "Enter Steam Guard code";
    private const string IncorrectPrompt = "Steam rejected that code. Enter the latest Steam Guard code:";
    private const string CompactIncorrectPrompt = "Code rejected";
    private const string CompactDefaultHelp = "Use current Steam Guard code\nOne-shot submit; code is not stored";
    private const string CompactIncorrectHelp = "Use newest Steam Guard code\nOld codes can expire; spaces removed";
    private const string CompactCodeSubmitBodyName = "CompactCodeSubmitBody";
    private const string CompactCodeSubmitTitleName = "CompactCodeSubmitTitle";
    private const string CompactCodeSubmitDetailName = "CompactCodeSubmitDetail";
    private const int CompactCodePromptHeight = 30;
    private const int CompactCodePromptFontSize = LauncherSectionMetrics.CompactVersionSummaryFontSize;
    private const int CompactCodeHelpHeight = 48;
    private const int CompactCodeHelpFontSize = LauncherSectionMetrics.CompactVersionSummaryFontSize;
    private const int CompactCodeSubmitTitleFontSize = LauncherSectionMetrics.CompactDetailButtonFontSize;
    private const int CompactCodeSubmitDetailFontSize = LauncherSectionMetrics.CompactDetailLabelFontSize;
    private const int CompactCodeSubmitHorizontalMargin = 6;
    private const int CompactCodeSubmitVerticalMargin = 4;
    private const int CompactCodeActionRowSeparation = 6;
    private readonly bool _compact;
    private bool _compactStackedActionRows;
    private bool _normalizingCodeText;

    internal event Action<string> CodeSubmitted;

    private readonly LineEdit _codeField;
    private readonly Label _codeLabel;
    private readonly Label _codeHelpLabel;
    private readonly GridContainer _compactCodeActionRow;

    internal CodeSection(float scale, bool compact = false, bool compactStackedActionRows = false)
    {
        _compact = compact;
        _compactStackedActionRows = compact && compactStackedActionRows;
        LauncherSectionSetup.ConfigureHiddenSection(
            this,
            scale,
            "Steam Guard",
            "Complete Steam's second factor challenge without storing your Steam password.",
            LauncherComponentTheme.CyanAccent,
            compact,
            "Current code"
        );

        _codeLabel = new StyledLabel(
            DefaultPrompt,
            scale,
            fontSize: compact
                ? CompactCodePromptFontSize
                : LauncherSectionMetrics.PromptFontSize
        );
        if (compact)
        {
            _codeLabel.AutowrapMode = TextServer.AutowrapMode.Off;
            _codeLabel.ClipText = true;
            _codeLabel.VerticalAlignment = VerticalAlignment.Center;
            _codeLabel.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
            _codeLabel.CustomMinimumSize = new Vector2(
                0,
                LauncherViewLayoutMetrics.ScaleInt(CompactCodePromptHeight, scale)
            );
        }
        AddChild(_codeLabel);

        _codeHelpLabel = new StyledLabel(
            CodeHelpText(compact, wasIncorrect: false),
            scale,
            fontSize: compact
                ? CompactCodeHelpFontSize
                : LauncherSectionMetrics.ProgressFontSize,
            align: compact
                ? HorizontalAlignment.Center
                : HorizontalAlignment.Left
        );
        _codeHelpLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        if (compact)
        {
            _codeHelpLabel.ClipText = true;
            _codeHelpLabel.VerticalAlignment = VerticalAlignment.Center;
            _codeHelpLabel.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
            _codeHelpLabel.CustomMinimumSize = new Vector2(
                0,
                LauncherViewLayoutMetrics.ScaleInt(CompactCodeHelpHeight, scale)
            );
        }
        AddChild(_codeHelpLabel);

        Container codeActionParent = this;
        Container compactCodeActionRow = null;
        if (compact)
        {
            _compactCodeActionRow = BuildCompactCodeActionRow(scale, _compactStackedActionRows);
            compactCodeActionRow = _compactCodeActionRow;
            AddChild(compactCodeActionRow);
            codeActionParent = compactCodeActionRow;
        }

        _codeField = new StyledLineEdit(
            compact ? "ABC123" : "Steam Guard code",
            scale,
            keyboardType: DisplayServer.VirtualKeyboardType.Default
        );
        _codeField.MaxLength = LauncherSectionMetrics.CodeMaxLength;
        _codeField.Alignment = HorizontalAlignment.Center;
        var inputHeight = compact
            ? LauncherSectionMetrics.CodeInputHeight
            : LauncherSectionMetrics.PrimaryButtonHeight;
        var inputFontSize = compact
            ? LauncherSectionMetrics.CodeInputFontSize
            : LauncherSectionMetrics.PrimaryButtonFontSize;
        _codeField.CustomMinimumSize = new Vector2(
            0,
            LauncherComponentTheme.ScaleInt(scale, inputHeight)
        );
        _codeField.AddThemeFontSizeOverride(
            LauncherComponentTheme.FontSize,
            LauncherComponentTheme.ScaleInt(scale, inputFontSize)
        );
        _codeField.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        codeActionParent.AddChild(_codeField);

        var submitButton = new StyledButton(
            compact ? CompactCodeSubmitText() : "Verify Code",
            scale,
            fontSize: compact
                ? LauncherSectionMetrics.CodeSubmitFontSize
                : LauncherSectionMetrics.PrimaryButtonFontSize,
            height: inputHeight
        );
        submitButton.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        LauncherButtonStyles.ApplyPrimaryAction(submitButton, scale);
        SetCompactCodeSubmitButtonText(submitButton, submitButton.Text, scale, compact);
        _codeField.TextChanged += NormalizeCodeText;
        _codeField.TextSubmitted += _ => OnSubmit();
        submitButton.Pressed += OnSubmit;
        codeActionParent.AddChild(submitButton);
        if (compactCodeActionRow != null)
            MoveChild(_codeHelpLabel, compactCodeActionRow.GetIndex() + 1);
    }

    internal void UpdateViewportProfile(LauncherLayoutProfile profile)
    {
        if (!_compact || !GodotObject.IsInstanceValid(_compactCodeActionRow))
            return;

        _compactStackedActionRows = profile.Compact && profile.CompactStackedActionRows;
        ApplyCompactCodeActionRowLayout(_compactCodeActionRow, profile.Scale, _compactStackedActionRows);
    }

    private static GridContainer BuildCompactCodeActionRow(float scale, bool compactStackedActionRows)
    {
        var row = new GridContainer();
        row.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        ApplyCompactCodeActionRowLayout(row, scale, compactStackedActionRows);
        return row;
    }

    private static void ApplyCompactCodeActionRowLayout(
        GridContainer row,
        float scale,
        bool compactStackedActionRows
    )
    {
        row.Columns = compactStackedActionRows ? 1 : 2;
        row.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(CompactCodeActionRowSeparation, scale)
        );
    }

    private static string CompactCodeSubmitText()
        => "Verify Code\nSubmit once";

    private static void SetCompactCodeSubmitButtonText(
        Button button,
        string text,
        float scale,
        bool compact
    )
    {
        if (!compact || !TrySplitCompactCodeSubmitText(text, out var title, out var detail))
        {
            HideCompactCodeSubmitButtonLabels(button);
            button.Text = text;
            return;
        }

        var labels = EnsureCompactCodeSubmitButtonLabels(button, scale);
        button.Text = "";
        labels.Body.Visible = true;
        labels.Title.Text = title;
        labels.Detail.Text = detail;
    }

    private static bool TrySplitCompactCodeSubmitText(
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

    private static void HideCompactCodeSubmitButtonLabels(Button button)
    {
        var body = button.GetNodeOrNull<VBoxContainer>(new NodePath(CompactCodeSubmitBodyName));
        if (body != null)
            body.Visible = false;
    }

    private static (VBoxContainer Body, StyledLabel Title, StyledLabel Detail) EnsureCompactCodeSubmitButtonLabels(
        Button button,
        float scale
    )
    {
        var body = button.GetNodeOrNull<VBoxContainer>(new NodePath(CompactCodeSubmitBodyName));
        if (body != null)
        {
            return (
                body,
                body.GetNode<StyledLabel>(new NodePath(CompactCodeSubmitTitleName)),
                body.GetNode<StyledLabel>(new NodePath(CompactCodeSubmitDetailName))
            );
        }

        body = new VBoxContainer
        {
            Name = CompactCodeSubmitBodyName,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        body.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        body.OffsetLeft = LauncherViewLayoutMetrics.ScaleInt(CompactCodeSubmitHorizontalMargin, scale);
        body.OffsetRight = -LauncherViewLayoutMetrics.ScaleInt(CompactCodeSubmitHorizontalMargin, scale);
        body.OffsetTop = LauncherViewLayoutMetrics.ScaleInt(CompactCodeSubmitVerticalMargin, scale);
        body.OffsetBottom = -LauncherViewLayoutMetrics.ScaleInt(CompactCodeSubmitVerticalMargin, scale);
        body.AddThemeConstantOverride(LauncherViewLayoutMetrics.ThemeSeparation, 0);

        var title = new StyledLabel(
            "",
            scale,
            fontSize: CompactCodeSubmitTitleFontSize,
            align: HorizontalAlignment.Center
        )
        {
            Name = CompactCodeSubmitTitleName,
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
            scale,
            fontSize: CompactCodeSubmitDetailFontSize,
            align: HorizontalAlignment.Center
        )
        {
            Name = CompactCodeSubmitDetailName,
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

    internal void Show(bool wasIncorrect)
    {
        Visible = true;
        _codeField.Text = "";
        _codeField.PlaceholderText = "Steam Guard code";
        _codeLabel.Text = CodePromptText(_compact, wasIncorrect);
        _codeHelpLabel.Text = CodeHelpText(_compact, wasIncorrect);
        _codeField.GrabFocus();
    }

    private static string CodePromptText(bool compact, bool wasIncorrect)
    {
        if (!wasIncorrect)
            return DefaultPrompt;

        return compact ? CompactIncorrectPrompt : IncorrectPrompt;
    }

    private static string CodeHelpText(bool compact, bool wasIncorrect)
    {
        if (compact)
            return wasIncorrect ? CompactIncorrectHelp : CompactDefaultHelp;

        return wasIncorrect
            ? "Use the newest code from your Steam app or Steam email. StS2 Mobile submits it once and never stores Steam Guard codes."
            : "Use the current code from your Steam app or Steam email. StS2 Mobile submits it once and never stores Steam Guard codes.";
    }

    private void NormalizeCodeText(string text)
    {
        if (_normalizingCodeText)
            return;

        var normalized = NormalizeCode(text);
        if (string.Equals(text, normalized, StringComparison.Ordinal))
            return;

        _normalizingCodeText = true;
        _codeField.Text = normalized;
        _codeField.CaretColumn = normalized.Length;
        _normalizingCodeText = false;
    }

    private void OnSubmit()
    {
        var code = NormalizeCode(_codeField.Text);
        if (string.IsNullOrEmpty(code))
            return;

        Visible = false;
        CodeSubmitted?.Invoke(code);
    }

    private static string NormalizeCode(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "";

        var normalized = new char[Math.Min(text.Length, LauncherSectionMetrics.CodeMaxLength)];
        var length = 0;
        foreach (var c in text)
        {
            if (length >= normalized.Length)
                break;

            if (char.IsLetterOrDigit(c))
                normalized[length++] = char.ToUpperInvariant(c);
        }

        return new string(normalized, 0, length);
    }
}
