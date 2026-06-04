using System.Collections.Generic;
using System.Text;

namespace STS2Mobile.Launcher;

internal static partial class LauncherDiagnostics
{
    private readonly struct InterestingDiagnosticTail
    {
        internal InterestingDiagnosticTail(DiagnosticFile file, int maxLines)
        {
            File = file;
            MaxLines = maxLines;
        }

        private DiagnosticFile File { get; }
        private int MaxLines { get; }

        internal void AppendHeader(StringBuilder sb)
            => File.AppendHeader(sb);

        internal IEnumerable<string> InterestingLines(FileReadResult read)
            => SelectInterestingDiagnosticLines(read.ContentLines(), MaxLines);

        internal FileReadResult Read()
            => File.Read();
    }
}
