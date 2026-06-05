using System;
using System.IO;
using System.Text;
using BootstrapTraceFile = STS2Mobile.BootstrapTrace;

namespace STS2Mobile.Launcher;

internal static partial class LauncherDiagnostics
{
    private readonly struct DiagnosticFile
    {
        internal DiagnosticFile(string label, string path)
        {
            Label = label;
            Path = path;
        }

        private string Label { get; }
        private string Path { get; }

        internal void AppendHeader(StringBuilder sb)
            => sb.AppendLine(Header(Label, Path));

        internal void AppendSummary(StringBuilder sb, long inlineContentLimit)
        {
            try
            {
                DiagnosticFileSnapshot
                    .From(this)
                    .AppendSummary(sb, this, inlineContentLimit);
            }
            catch (Exception ex)
            {
                sb.AppendLine(InspectFailedMessage(ex));
            }
        }

        internal void AppendContentsSection(StringBuilder sb)
        {
            AppendHeader(sb);

            try
            {
                DiagnosticFileSnapshot.From(this).AppendContentsSection(sb);
            }
            catch (Exception ex)
            {
                sb.AppendLine($"  failed={ex.Message}");
                sb.AppendLine();
            }
        }

        private string InspectFailedMessage(Exception ex)
            => $"{Label}: failed to inspect {Path}: {ex.Message}";

        internal FileInfo Info()
            => new(Path);

        internal FileReadResult Read()
            => ReadFileText(Path);

        internal string SummaryLine()
            => $"{Label}: {Path}";

        internal void WriteAllText(string text)
            => System.IO.File.WriteAllText(Path, text);
    }

    private static DiagnosticFile AndroidUncaughtException(string dataDir)
        => new(
            "Android uncaught exception",
            Path.Combine(dataDir, LauncherStorageNames.AndroidUncaughtException)
        );

    private static DiagnosticFile BootstrapTrace()
        => new("Bootstrap trace", BootstrapTraceFile.TracePath);

    private static DiagnosticFile GamePck(string dataDir)
        => new("Game PCK", LauncherGameFiles.PckPath(dataDir));

    private static DiagnosticFile ManualSafeLaunchMarker(string dataDir)
        => new(
            "Manual safe launch marker",
            Path.Combine(dataDir, LauncherStorageNames.ManualSafeLaunch)
        );

    private static DiagnosticFile StartupMarker(string dataDir)
        => new(
            "Startup marker",
            Path.Combine(dataDir, LauncherStorageNames.StartupMarker)
        );

    private static DiagnosticFile StartupSceneSnapshot(string dataDir)
        => new(
            "Startup scene snapshot",
            Path.Combine(dataDir, LauncherStorageNames.StartupSceneSnapshot)
        );
}
