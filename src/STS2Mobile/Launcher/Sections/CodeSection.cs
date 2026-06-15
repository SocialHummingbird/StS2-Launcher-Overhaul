using System;
using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher.Sections;

internal sealed class CodeSection : VBoxContainer
{
    private const string DefaultPrompt = "Enter Steam Guard code";
    private const string IncorrectPrompt = "Steam rejected that code. Enter the latest Steam Guard code:";

    internal event Action<string> CodeSubmitted;

    private readonly LineEdit _codeField;
    private readonly Label _codeLabel;
    private readonly Label _codeHelpLabel;

    internal CodeSection(float scale, bool compact = false)
    {
        LauncherSectionSetup.ConfigureHiddenSection(
            this,
            scale,
            "Steam Guard",
            "Complete Steam's second factor challenge without storing your Steam password.",
            LauncherComponentTheme.CyanAccent,
            compact
        );

        _codeLabel = new StyledLabel(
            DefaultPrompt,
            scale,
            fontSize: LauncherSectionMetrics.PromptFontSize
        );
        AddChild(_codeLabel);

        _codeHelpLabel = new StyledLabel(
            compact
                ? "Use the current Steam app or email code. Codes are never stored."
                : "Use the current code from your Steam app or Steam email. StS2 Mobile submits it once and never stores Steam Guard codes.",
            scale,
            fontSize: LauncherSectionMetrics.ProgressFontSize,
            align: HorizontalAlignment.Left
        );
        _codeHelpLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        AddChild(_codeHelpLabel);

        _codeField = new StyledLineEdit(
            "Steam Guard code",
            scale,
            keyboardType: DisplayServer.VirtualKeyboardType.Number
        );
        _codeField.MaxLength = LauncherSectionMetrics.CodeMaxLength;
        AddChild(_codeField);

        var submitButton = new StyledButton("VERIFY CODE", scale);
        LauncherButtonStyles.ApplyPrimaryAction(submitButton, scale);
        _codeField.TextSubmitted += _ => OnSubmit();
        submitButton.Pressed += OnSubmit;
        AddChild(submitButton);
    }

    internal void Show(bool wasIncorrect)
    {
        Visible = true;
        _codeField.Text = "";
        _codeField.PlaceholderText = "Steam Guard code";
        _codeLabel.Text = wasIncorrect ? IncorrectPrompt : DefaultPrompt;
        _codeField.GrabFocus();
    }

    private void OnSubmit()
    {
        var code = _codeField.Text.Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(code))
            return;

        Visible = false;
        CodeSubmitted?.Invoke(code);
    }
}
