using System;
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
