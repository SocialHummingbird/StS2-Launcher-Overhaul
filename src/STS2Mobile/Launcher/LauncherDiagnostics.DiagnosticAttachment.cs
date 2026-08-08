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

        internal void AppendTruncatedContent(
            StringBuilder sb,
            string missingPrefix = "",
            string failedPrefix = ""
        )
        {
            var read = Read();
            if (read.HasContent())
            {
                sb.AppendLine(TruncatedContent(read));
                return;
            }

            read.AppendStatus(sb, missingPrefix, failedPrefix);
        }

        private string TruncatedContent(FileReadResult read)
            => TruncateForDisplay(read.ContentText(), MaxChars);
    }
}
