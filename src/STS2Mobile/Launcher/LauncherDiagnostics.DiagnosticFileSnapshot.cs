using System;
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

        private void AppendContents(StringBuilder sb)
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

        private void AppendInlineContentsIfSmall(StringBuilder sb, long inlineContentLimit)
        {
            if (Bytes <= inlineContentLimit)
                sb.AppendLine($"  contents={SingleLine(Text)}");
        }

        private void AppendMetadata(StringBuilder sb, DiagnosticFileMetadataStyle style)
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

        private bool AppendReadStatusIfNoText(StringBuilder sb)
        {
            if (HasText)
                return false;

            Read.AppendFileStatus(sb);
            return true;
        }

        private static string SingleLine(string text)
            => text.Replace('\n', ' ').Replace('\r', ' ');
    }
}
