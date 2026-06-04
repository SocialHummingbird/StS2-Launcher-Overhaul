using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using STS2Mobile;

namespace STS2Mobile.Launcher;

internal static partial class LauncherDiagnostics
{
    private static readonly string[] InterestingDiagnosticKeywords =
    {
        "error",
        "exception",
        "failed",
        "fatal",
        "crash",
        "watchdog",
        "stalled",
        "platformutil",
        "main menu",
        "startup",
        "godot",
        "mono",
        "sts2mobile",
    };
    private const int RawLogcatTailLines = 1200;

    private readonly struct LogcatCapture
    {
        private enum CaptureState
        {
            Captured,
            Unavailable,
        }

        private LogcatCapture(CaptureState state, string text)
        {
            State = state;
            Text = text;
        }

        private CaptureState State { get; }
        private string Text { get; }
        private bool HasText => State == CaptureState.Captured;

        internal static LogcatCapture Captured(string text)
            => new(CaptureState.Captured, text);

        internal static LogcatCapture Unavailable(string fallbackText)
            => new(CaptureState.Unavailable, fallbackText);

        internal void AppendContent(StringBuilder sb)
            => sb.AppendLine(Text);

        internal string Content()
            => Text;

        internal bool HasContent()
            => HasText;

        internal IEnumerable<string> InterestingLines(int maxLines)
            => SelectInterestingDiagnosticLines(ContentLines(), maxLines);

        private string[] ContentLines()
            => Text.Replace("\r\n", "\n").Split('\n');
    }

    private static void AppendLogcatTail(
        StringBuilder sb,
        string heading,
        int lineCount,
        bool leadingBlank = false
    )
    {
        if (leadingBlank)
            sb.AppendLine();

        sb.AppendLine(heading);
        sb.AppendLine(CaptureLogcatContent(lineCount));
    }

    private static string CaptureLogcatContent(int lineCount)
        => CaptureLogcat(lineCount).Content();

    private static void AppendRawLogcatTail(StringBuilder sb)
        => AppendLogcatTail(
            sb,
            RawLogcatTail,
            RawLogcatTailLines,
            leadingBlank: true
        );

    private static void AppendLogcatErrorSummary(StringBuilder sb)
    {
        sb.AppendLine();
        sb.AppendLine(LogcatErrorLines);
        var logcat = CaptureLogcat(ErrorSummaryCaptureLines);
        if (!logcat.HasContent())
        {
            logcat.AppendContent(sb);
            return;
        }

        foreach (var line in logcat.InterestingLines(ErrorSummaryInterestingLines))
            sb.AppendLine(line);
    }

    private static string[] SelectInterestingDiagnosticLines(string[] lines, int maxLines)
    {
        var selected = Tail(lines.Where(IsInterestingDiagnosticLine), maxLines);

        if (selected.Length > 0)
            return selected;

        return Tail(
            lines.Where(line => !string.IsNullOrWhiteSpace(line)),
            Math.Min(maxLines, ErrorSummaryFallbackLines)
        );
    }

    private static bool IsInterestingDiagnosticLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return false;

        var lower = line.ToLowerInvariant();
        return InterestingDiagnosticKeywords.Any(lower.Contains);
    }

    private static string[] Tail(IEnumerable<string> lines, int maxLines) =>
        lines.TakeLast(maxLines).ToArray();

    private static LogcatCapture CaptureLogcat(int lineCount)
    {
        try
        {
            var text = AndroidGodotAppBridge.GetLogcatTail(lineCount);
            return string.IsNullOrWhiteSpace(text)
                ? LogcatCapture.Unavailable(Unavailable)
                : LogcatCapture.Captured(text);
        }
        catch (Exception ex)
        {
            return LogcatCapture.Unavailable(LogcatCollectionFailed(ex));
        }
    }

}
