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
        private LogcatCapture(string? text, string? fallbackText)
        {
            Text = text;
            FallbackText = fallbackText;
        }

        internal string? Text { get; }
        internal string? FallbackText { get; }
        internal bool HasText => Text != null;

        internal static LogcatCapture Captured(string text)
            => new(text, fallbackText: null);

        internal static LogcatCapture Unavailable(string fallbackText)
            => new(text: null, fallbackText);
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
    {
        var logcat = CaptureLogcat(lineCount);
        return logcat.HasText ? logcat.Text! : logcat.FallbackText ?? string.Empty;
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
        var logcat = CaptureLogcat(ErrorSummaryCaptureLines);
        if (!logcat.HasText)
        {
            sb.AppendLine(logcat.FallbackText);
            return;
        }

        var lines = logcat.Text!.Replace("\r\n", "\n").Split('\n');
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
