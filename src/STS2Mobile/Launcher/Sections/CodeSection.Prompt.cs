namespace STS2Mobile.Launcher.Sections;

internal sealed partial class CodeSection
{
    private const string DefaultPrompt = "Enter Steam Guard code";
    private const string IncorrectPrompt = "Steam rejected that code. Enter the latest Steam Guard code:";
    private const string CompactIncorrectPrompt = "Code rejected";
    private const string CompactDefaultHelp = "Use current Steam Guard code\nOne-shot submit; code is not stored";
    private const string CompactIncorrectHelp = "Use newest Steam Guard code\nOld codes can expire; spaces removed";
    private const int CompactCodePromptHeight = 30;
    private const int CompactCodePromptFontSize = LauncherSectionMetrics.CompactVersionSummaryFontSize;
    private const int CompactCodeHelpHeight = 48;
    private const int CompactCodeHelpFontSize = LauncherSectionMetrics.CompactVersionSummaryFontSize;

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
}
