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
        => LogcatSnapshot.Capture(lineCount).Content;

    private static bool TryCaptureLogcatText(
        int lineCount,
        out string text,
        out string fallbackText
    )
    {
        var logcat = LogcatSnapshot.Capture(lineCount);
        fallbackText = logcat.Content;
        return logcat.TryGetText(out text);
    }

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
        if (!TryCaptureLogcatText(
                ErrorSummaryCaptureLines,
                out var text,
                out var fallbackText
            ))
        {
            sb.AppendLine(fallbackText);
            return;
        }

        var lines = text.Replace("\r\n", "\n").Split('\n');
        foreach (var line in SelectInterestingDiagnosticLines(
            lines,
            ErrorSummaryInterestingLines
        ))
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

    private sealed class LogcatSnapshot
    {
        private readonly string _fallbackText;
        private readonly string _text;

        private LogcatSnapshot(string text, string fallbackText)
        {
            _text = text;
            _fallbackText = fallbackText;
        }

        private string Content => HasText ? _text : _fallbackText;

        private bool HasText => !string.IsNullOrWhiteSpace(_text);

        private bool TryGetText(out string text)
        {
            text = _text;
            return HasText;
        }

        private static LogcatSnapshot Capture(int lineCount)
        {
            try
            {
                var text = AndroidGodotAppBridge.GetLogcatTail(lineCount);
                return string.IsNullOrWhiteSpace(text)
                    ? new LogcatSnapshot(null, Unavailable)
                    : new LogcatSnapshot(text, null);
            }
            catch (Exception ex)
            {
                return new LogcatSnapshot(null, LogcatCollectionFailed(ex));
            }
        }
    }
}
