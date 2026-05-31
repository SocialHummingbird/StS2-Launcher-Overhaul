using System;
using STS2Mobile;

namespace STS2Mobile.Launcher;

internal sealed class LauncherLogcatSnapshot
{
    private LauncherLogcatSnapshot(string text, string fallbackText)
    {
        Text = text;
        FallbackText = fallbackText;
    }

    internal string Text { get; }
    internal string FallbackText { get; }
    internal bool Available => !string.IsNullOrWhiteSpace(Text);
    internal string Content => Available ? Text : FallbackText;

    internal static LauncherLogcatSnapshot Capture(int lineCount)
    {
        try
        {
            var text = AndroidGodotAppBridge.GetLogcatTail(lineCount);
            return string.IsNullOrWhiteSpace(text)
                ? new LauncherLogcatSnapshot(null, LauncherDiagnosticAppendixText.Unavailable)
                : new LauncherLogcatSnapshot(text, null);
        }
        catch (Exception ex)
        {
            return new LauncherLogcatSnapshot(
                null,
                LauncherDiagnosticAppendixText.LogcatCollectionFailed(ex)
            );
        }
    }
}
