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

        private FileReadResult Read { get; }
        private long Bytes { get; }
        private DateTime ModifiedUtc { get; }
        private bool HasText => Read.HasContent();
        private string Text => Read.ContentText();

        internal static DiagnosticFileSnapshot From(DiagnosticFile file)
        {
            var read = file.Read();
            if (!read.HasContent())
                return new DiagnosticFileSnapshot(read, bytes: 0, modifiedUtc: default);

            var info = file.Info();
            return new DiagnosticFileSnapshot(read, info.Length, info.LastWriteTimeUtc);
        }

        internal void AppendContents(StringBuilder sb)
        {
            sb.AppendLine(Text);
        }

        internal void AppendContentsSection(StringBuilder sb)
        {
            if (AppendReadStatusIfNoText(sb))
            {
                sb.AppendLine();
                return;
            }

            AppendMetadata(sb, DiagnosticFileMetadataStyle.Inline);
            sb.AppendLine("  contents:");
            AppendContents(sb);
            sb.AppendLine();
        }

        internal void AppendSummary(
            StringBuilder sb,
            DiagnosticFile file,
            long inlineContentLimit
        )
        {
            sb.AppendLine(file.SummaryLine());
            if (AppendReadStatusIfNoText(sb))
                return;

            AppendMetadata(sb, DiagnosticFileMetadataStyle.MultiLine);
            AppendInlineContentsIfSmall(sb, inlineContentLimit);
        }

        internal void AppendInlineContentsIfSmall(StringBuilder sb, long inlineContentLimit)
        {
            if (Bytes <= inlineContentLimit)
                sb.AppendLine($"  contents={SingleLine(Text)}");
        }

        internal void AppendMetadata(StringBuilder sb, DiagnosticFileMetadataStyle style)
        {
            if (style == DiagnosticFileMetadataStyle.Inline)
            {
                sb.AppendLine(
                    $"  exists=True bytes={Bytes} modifiedUtc={ModifiedUtc:O}"
                );
                return;
            }

            sb.AppendLine("  exists=True");
            sb.AppendLine($"  bytes={Bytes}");
            sb.AppendLine($"  modifiedUtc={ModifiedUtc:O}");
        }

        internal bool AppendReadStatusIfNoText(StringBuilder sb)
        {
            if (HasText)
                return false;

            Read.AppendFileStatus(sb);
            return true;
        }
    }

    private static void AppendFileSummary(
        StringBuilder sb,
        DiagnosticFile file,
        long inlineContentLimit
    )
        => AppendInspectedFile(
            file,
            snapshot => snapshot.AppendSummary(sb, file, inlineContentLimit),
            ex => sb.AppendLine(file.InspectFailedMessage(ex))
        );

    private static void AppendFileContentsSection(
        StringBuilder sb,
        DiagnosticFile file
    )
    {
        file.AppendHeader(sb);

        AppendInspectedFile(
            file,
            snapshot => snapshot.AppendContentsSection(sb),
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

    private static string SingleLine(string text)
        => text.Replace('\n', ' ').Replace('\r', ' ');

}
