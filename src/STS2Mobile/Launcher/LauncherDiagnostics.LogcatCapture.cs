using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace STS2Mobile.Launcher;

internal static partial class LauncherDiagnostics
{
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

    private static string[] Tail(IEnumerable<string> lines, int maxLines) =>
        lines.TakeLast(maxLines).ToArray();
}
