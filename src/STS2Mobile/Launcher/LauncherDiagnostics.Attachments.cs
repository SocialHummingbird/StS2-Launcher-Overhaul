using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace STS2Mobile.Launcher;

internal static partial class LauncherDiagnostics
{
    private const int LargeAttachmentMaxChars = 256 * 1024;
    private const int SmallAttachmentMaxChars = 64 * 1024;

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

    private static void AppendSummaryErrorDiagnostics(StringBuilder sb, string dataDir)
    {
        AppendAttachments(
            sb,
            SummarySmallFiles(dataDir),
            SummaryFileStatusPrefix,
            SummaryFileStatusPrefix
        );

        foreach (var tail in SummaryInterestingTails(dataDir))
            AppendInterestingFileTail(sb, tail);

        AppendLogcatErrorSummary(sb);
    }

    private static void AppendRawErrorDiagnostics(StringBuilder sb, string dataDir)
    {
        AppendAttachments(sb, RawErrorLogFiles(dataDir));
        AppendRawLogcatTail(sb);
    }

    private static IEnumerable<DiagnosticAttachment> SummarySmallFiles(string dataDir)
    {
        yield return new DiagnosticAttachment(StartupMarker(dataDir), 2048);
        yield return new DiagnosticAttachment(AndroidUncaughtException(dataDir), 4096);
    }

    private static IEnumerable<InterestingDiagnosticTail> SummaryInterestingTails(string dataDir)
    {
        yield return new InterestingDiagnosticTail(BootstrapTrace(), 80);
        yield return new InterestingDiagnosticTail(StartupSceneSnapshot(dataDir), 80);
    }

    private static IEnumerable<DiagnosticAttachment> RawErrorLogFiles(string dataDir)
    {
        yield return new DiagnosticAttachment(
            StartupMarker(dataDir),
            SmallAttachmentMaxChars
        );
        yield return new DiagnosticAttachment(
            AndroidUncaughtException(dataDir),
            SmallAttachmentMaxChars
        );
        yield return new DiagnosticAttachment(BootstrapTrace(), LargeAttachmentMaxChars);
        yield return new DiagnosticAttachment(
            StartupSceneSnapshot(dataDir),
            LargeAttachmentMaxChars
        );
    }

    private static void AppendAttachments(
        StringBuilder sb,
        IEnumerable<DiagnosticAttachment> files,
        string missingPrefix = "",
        string failedPrefix = ""
    )
    {
        foreach (var file in files)
        {
            sb.AppendLine();
            file.AppendHeader(sb);
            AppendTruncatedFile(
                sb,
                file,
                missingPrefix,
                failedPrefix
            );
        }
    }

    private static void AppendInterestingFileTail(
        StringBuilder sb,
        InterestingDiagnosticTail file
    )
    {
        sb.AppendLine();
        file.AppendHeader(sb);
        var read = file.Read();
        if (!read.HasContent())
        {
            read.AppendStatus(sb);
            return;
        }

        foreach (var line in file.InterestingLines(read))
            sb.AppendLine(line);
    }

    private static void AppendTruncatedFile(
        StringBuilder sb,
        DiagnosticAttachment file,
        string missingPrefix = "",
        string failedPrefix = ""
    )
    {
        var read = file.Read();
        if (read.HasContent())
        {
            sb.AppendLine(file.TruncatedContent(read));
            return;
        }

        read.AppendStatus(sb, missingPrefix, failedPrefix);
    }

    private static FileReadResult ReadFileText(string path)
    {
        try
        {
            if (!System.IO.File.Exists(path))
                return FileReadResult.Missing();

            return FileReadResult.Read(System.IO.File.ReadAllText(path));
        }
        catch (Exception ex)
        {
            return FileReadResult.Failed(ex.Message);
        }
    }

    private static string TruncateForDisplay(string text, int maxChars)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxChars)
            return text ?? string.Empty;

        return text.Substring(0, maxChars) + "\n<truncated>";
    }
}
