using System.Text;

namespace STS2Mobile.Launcher;

internal static partial class LauncherDiagnostics
{
    private readonly struct DiagnosticAttachment
    {
        internal DiagnosticAttachment(DiagnosticFile file, int maxChars)
        {
            File = file;
            MaxChars = maxChars;
        }

        private DiagnosticFile File { get; }
        private int MaxChars { get; }

        internal void AppendHeader(StringBuilder sb)
            => File.AppendHeader(sb);

        internal FileReadResult Read()
            => File.Read();

        internal string TruncatedContent(FileReadResult read)
            => TruncateForDisplay(read.ContentText(), MaxChars);
    }
}
