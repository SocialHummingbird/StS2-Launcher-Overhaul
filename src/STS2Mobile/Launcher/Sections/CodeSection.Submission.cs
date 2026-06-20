using System;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class CodeSection
{
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
