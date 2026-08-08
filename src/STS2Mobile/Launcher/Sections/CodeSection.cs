using System;
using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class CodeSection : VBoxContainer
{
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

        _codeLabel = CreateCodePromptLabel(scale, compact);
        AddChild(_codeLabel);

        _codeHelpLabel = CreateCodeHelpLabel(scale, compact);
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

        _codeField = CreateCodeField(scale, compact);
        codeActionParent.AddChild(_codeField);

        var submitButton = CreateCodeSubmitButton(scale, compact);
        _codeField.TextChanged += NormalizeCodeText;
        _codeField.TextSubmitted += _ => OnSubmit();
        submitButton.Pressed += OnSubmit;
        codeActionParent.AddChild(submitButton);
        if (compactCodeActionRow != null)
            MoveChild(_codeHelpLabel, compactCodeActionRow.GetIndex() + 1);
    }
}
