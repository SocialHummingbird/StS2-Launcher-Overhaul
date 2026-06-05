using System.Collections.Generic;
using System.Text;

namespace STS2Mobile.Launcher;

internal static partial class LauncherDiagnostics
{
    private static void AppendFileContentsSections(
        StringBuilder sb,
        IEnumerable<DiagnosticFile> files
    )
    {
        foreach (var file in files)
            file.AppendContentsSection(sb);
    }

    private static void AppendFileSummaries(
        StringBuilder sb,
        IEnumerable<DiagnosticFile> files,
        long inlineContentLimit
    )
    {
        foreach (var file in files)
            file.AppendSummary(sb, inlineContentLimit);
    }
}
