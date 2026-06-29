using System.Collections.Generic;

namespace STS2Mobile.Launcher;

internal static partial class LauncherDiagnostics
{
    private const int LargeAttachmentMaxChars = 256 * 1024;
    private const int SmallAttachmentMaxChars = 64 * 1024;

    private static IEnumerable<DiagnosticAttachment> SummarySmallFiles(string dataDir)
    {
        yield return new DiagnosticAttachment(StartupMarker(dataDir), 2048);
        yield return new DiagnosticAttachment(StartupContext(dataDir), 4096);
        yield return new DiagnosticAttachment(StartupTimeline(dataDir), 4096);
        yield return new DiagnosticAttachment(AndroidUncaughtException(dataDir), 4096);
    }

    private static IEnumerable<InterestingDiagnosticTail> SummaryInterestingTails(
        string dataDir
    )
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
            StartupContext(dataDir),
            SmallAttachmentMaxChars
        );
        yield return new DiagnosticAttachment(
            StartupTimeline(dataDir),
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
}
