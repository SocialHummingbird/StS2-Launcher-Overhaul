using System;
using System.IO;
using System.Text;

namespace STS2Mobile.Launcher;

internal static partial class LauncherDiagnostics
{
    private enum DiagnosticFileMetadataStyle
    {
        MultiLine,
        Inline,
    }

    private readonly struct DiagnosticFileSnapshot
    {
        private DiagnosticFileSnapshot(
            FileReadResult read,
            long bytes,
            DateTime modifiedUtc
        )
        {
            Read = read;
            Bytes = bytes;
            ModifiedUtc = modifiedUtc;
        }

        internal FileReadResult Read { get; }
        internal long Bytes { get; }
        internal DateTime ModifiedUtc { get; }
        internal bool HasText => Read.HasText;
        internal string Text => Read.Content;

        internal static DiagnosticFileSnapshot From(DiagnosticFile file)
        {
            var read = ReadFileText(file.Path);
            if (!read.HasText)
                return new DiagnosticFileSnapshot(read, bytes: 0, modifiedUtc: default);

            var info = new FileInfo(file.Path);
            return new DiagnosticFileSnapshot(read, info.Length, info.LastWriteTimeUtc);
        }
    }

    private static void AppendFileSummary(
        StringBuilder sb,
        DiagnosticFile file,
        long inlineContentLimit
    )
        => AppendInspectedFile(
            file,
            snapshot =>
            {
                sb.AppendLine($"{file.Label}: {file.Path}");
                if (!snapshot.HasText)
                {
                    AppendFileReadStatus(sb, snapshot.Read);
                    return;
                }

                AppendFileMetadata(sb, snapshot, DiagnosticFileMetadataStyle.MultiLine);
                if (snapshot.Bytes <= inlineContentLimit)
                    sb.AppendLine($"  contents={SingleLine(snapshot.Text)}");
            },
            ex => sb.AppendLine($"{file.Label}: failed to inspect {file.Path}: {ex.Message}")
        );

    private static void AppendFileContentsSection(
        StringBuilder sb,
        DiagnosticFile file
    )
    {
        sb.AppendLine(Header(file.Label, file.Path));

        AppendInspectedFile(
            file,
            snapshot =>
            {
                if (!snapshot.HasText)
                {
                    AppendFileReadStatus(sb, snapshot.Read);
                    sb.AppendLine();
                    return;
                }

                AppendFileMetadata(sb, snapshot, DiagnosticFileMetadataStyle.Inline);
                sb.AppendLine("  contents:");
                sb.AppendLine(snapshot.Text);
                sb.AppendLine();
            },
            ex =>
            {
                sb.AppendLine($"  failed={ex.Message}");
                sb.AppendLine();
            }
        );
    }

    private static void AppendInspectedFile(
        DiagnosticFile file,
        Action<DiagnosticFileSnapshot> appendSnapshot,
        Action<Exception> appendFailure
    )
    {
        try
        {
            appendSnapshot(DiagnosticFileSnapshot.From(file));
        }
        catch (Exception ex)
        {
            appendFailure(ex);
        }
    }

    private static void AppendFileMetadata(
        StringBuilder sb,
        DiagnosticFileSnapshot snapshot,
        DiagnosticFileMetadataStyle style
    )
    {
        if (style == DiagnosticFileMetadataStyle.Inline)
        {
            sb.AppendLine(
                $"  exists=True bytes={snapshot.Bytes} modifiedUtc={snapshot.ModifiedUtc:O}"
            );
            return;
        }

        sb.AppendLine("  exists=True");
        sb.AppendLine($"  bytes={snapshot.Bytes}");
        sb.AppendLine($"  modifiedUtc={snapshot.ModifiedUtc:O}");
    }

    private static string SingleLine(string text)
        => text.Replace('\n', ' ').Replace('\r', ' ');

    private static void AppendFileReadStatus(StringBuilder sb, FileReadResult read)
        => sb.AppendLine(
            read.IsMissing
                ? "  exists=False"
                : $"  failed={read.Error}"
        );
}
