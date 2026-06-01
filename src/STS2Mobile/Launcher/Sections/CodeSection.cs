using System;
using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher.Sections;

internal sealed class CodeSection : VBoxContainer
{
    private const string DefaultPrompt = "Enter Steam Guard code";
    private const string IncorrectPrompt = "Code was incorrect. Enter new code:";

    internal event Action<string> CodeSubmitted;

    private readonly LineEdit _codeField;
    private readonly Label _codeLabel;

    internal CodeSection(float scale)
    {
        LauncherSectionSetup.ConfigureHiddenSection(this, scale);

        _codeLabel = new StyledLabel(
            "Enter Steam Guard code",
            scale,
            fontSize: LauncherSectionMetrics.PromptFontSize
        );
        AddChild(_codeLabel);

        _codeField = new StyledLineEdit("Code", scale);
        _codeField.MaxLength = LauncherSectionMetrics.CodeMaxLength;
        AddChild(_codeField);

        var submitButton = new StyledButton("SUBMIT", scale);
        _codeField.TextSubmitted += _ => OnSubmit();
        submitButton.Pressed += OnSubmit;
        AddChild(submitButton);
    }

    internal void Show(bool wasIncorrect)
    {
        Visible = true;
        _codeField.Text = "";
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
