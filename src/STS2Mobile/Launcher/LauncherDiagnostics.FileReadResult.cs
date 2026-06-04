using System.Text;

namespace STS2Mobile.Launcher;

internal static partial class LauncherDiagnostics
{
    private readonly struct FileReadResult
    {
        private enum ReadState
        {
            Content,
            Missing,
            Failed,
        }

        private FileReadResult(ReadState state, string text)
        {
            State = state;
            Text = text;
        }

        private ReadState State { get; }
        private string Text { get; }
        private bool HasText => State == ReadState.Content;
        private bool IsMissing => State == ReadState.Missing;

        internal static FileReadResult Read(string text)
            => new(ReadState.Content, text);

        internal static FileReadResult Missing()
            => new(ReadState.Missing, string.Empty);

        internal static FileReadResult Failed(string error)
            => new(ReadState.Failed, error);

        internal void AppendFileStatus(StringBuilder sb)
            => sb.AppendLine(IsMissing ? "  exists=False" : $"  failed={Text}");

        internal void AppendStatus(
            StringBuilder sb,
            string missingPrefix = "",
            string failedPrefix = ""
        )
            => sb.AppendLine(Status(missingPrefix, failedPrefix));

        internal string ContentText()
            => HasText ? Text : string.Empty;

        internal bool HasContent()
            => HasText;

        internal string[] ContentLines()
            => ContentText().Replace("\r\n", "\n").Split('\n');

        private string Status(string missingPrefix = "", string failedPrefix = "")
            => IsMissing
                ? $"{missingPrefix}<missing>"
                : $"{failedPrefix}<failed to read: {Text}>";
    }
}
