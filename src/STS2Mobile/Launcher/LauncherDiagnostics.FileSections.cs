using System;
using System.Collections.Generic;
using System.Text;

namespace STS2Mobile.Launcher;

internal static partial class LauncherDiagnostics
{
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

    private static void AppendFileContentsSections(
        StringBuilder sb,
        IEnumerable<DiagnosticFile> files
    )
    {
        foreach (var file in files)
            AppendFileContentsSection(sb, file);
    }

    private static void AppendFileSummaries(
        StringBuilder sb,
        IEnumerable<DiagnosticFile> files,
        long inlineContentLimit
    )
    {
        foreach (var file in files)
            AppendFileSummary(sb, file, inlineContentLimit);
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
