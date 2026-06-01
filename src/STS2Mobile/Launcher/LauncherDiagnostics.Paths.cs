using System;
using System.IO;
using System.Text;
using BootstrapTraceFile = STS2Mobile.BootstrapTrace;

namespace STS2Mobile.Launcher;

internal static partial class LauncherDiagnostics
{
    private readonly struct DiagnosticFile
    {
        private DiagnosticFile(string label, string path)
        {
            Label = label;
            Path = path;
        }

        private string Label { get; }
        private string Path { get; }

        internal static DiagnosticFile Create(string label, string path)
            => new(label, path);

        internal void AppendHeader(StringBuilder sb)
            => sb.AppendLine(Header(Label, Path));

        internal string InspectFailedMessage(Exception ex)
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
        => DiagnosticFile.Create(
            "Android uncaught exception",
            Path.Combine(dataDir, LauncherStorageNames.AndroidUncaughtException)
        );

    private static DiagnosticFile BootstrapTrace()
        => DiagnosticFile.Create("Bootstrap trace", BootstrapTraceFile.TracePath);

    private static DiagnosticFile GamePck(string dataDir)
        => DiagnosticFile.Create("Game PCK", LauncherGameFiles.PckPath(dataDir));

    private static DiagnosticFile ManualSafeLaunchMarker(string dataDir)
        => DiagnosticFile.Create(
            "Manual safe launch marker",
            Path.Combine(dataDir, LauncherStorageNames.ManualSafeLaunch)
        );

    private static DiagnosticFile StartupMarker(string dataDir)
        => DiagnosticFile.Create(
            "Startup marker",
            Path.Combine(dataDir, LauncherStorageNames.StartupMarker)
        );

    private static DiagnosticFile StartupSceneSnapshot(string dataDir)
        => DiagnosticFile.Create(
            "Startup scene snapshot",
            Path.Combine(dataDir, LauncherStorageNames.StartupSceneSnapshot)
        );
}
