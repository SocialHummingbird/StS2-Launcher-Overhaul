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

    private static DiagnosticFile GraphicsDevice(string dataDir)
        => new(
            "Graphics device",
            Path.Combine(dataDir, LauncherStorageNames.GraphicsDevice)
        );

    private static DiagnosticFile ManualSafeLaunchMarker(string dataDir)
        => new(
            "Manual safe launch marker",
            Path.Combine(dataDir, LauncherStorageNames.ManualSafeLaunch)
        );

    private static DiagnosticFile MainMenuPreparation(string dataDir)
        => new(
            "Main-menu preparation",
            Path.Combine(dataDir, LauncherStorageNames.MainMenuPreparation)
        );

    private static DiagnosticFile LaunchAttempt(string dataDir)
        => new(
            "Launch attempt",
            Path.Combine(dataDir, LauncherStorageNames.LaunchAttempt)
        );

    private static DiagnosticFile StartupMarker(string dataDir)
        => new(
            "Startup marker",
            Path.Combine(dataDir, LauncherStorageNames.StartupMarker)
        );

    private static DiagnosticFile StartupContext(string dataDir)
        => new(
            "Startup context",
            Path.Combine(dataDir, LauncherStorageNames.StartupContext)
        );

    private static DiagnosticFile PostStartupTrace(string dataDir)
        => new(
            "Post-startup trace",
            Path.Combine(dataDir, LauncherStorageNames.PostStartupTrace)
        );

    private static DiagnosticFile PostStartupHeartbeat(string dataDir)
        => new(
            "Post-startup heartbeat",
            Path.Combine(dataDir, LauncherStorageNames.PostStartupHeartbeat)
        );

    private static DiagnosticFile AppLifecycle(string dataDir)
        => new(
            "App lifecycle",
            Path.Combine(dataDir, LauncherStorageNames.AppLifecycle)
        );

    private static DiagnosticFile ProcessExitInfo(string dataDir)
        => new(
            "Historical process exit info",
            Path.Combine(dataDir, LauncherStorageNames.ProcessExitInfo)
        );

    private static DiagnosticFile RendererAttempt(string dataDir)
        => new(
            "Renderer attempt",
            Path.Combine(dataDir, LauncherStorageNames.RendererAttempt)
        );

    private static DiagnosticFile SteamAuthFailure(string dataDir)
        => new(
            "Steam auth failure",
            Path.Combine(dataDir, LauncherStorageNames.SteamAuthFailure)
        );

    private static DiagnosticFile ShaderWarmupStatus(string dataDir)
        => new(
            "Shader warmup status",
            Path.Combine(dataDir, LauncherStorageNames.ShaderWarmupStatus)
        );

    private static DiagnosticFile StartupTimeline(string dataDir)
        => new(
            "Startup timeline",
            Path.Combine(dataDir, LauncherStorageNames.StartupTimeline)
        );

    private static DiagnosticFile StartupSceneSnapshot(string dataDir)
        => new(
            "Startup scene snapshot",
            Path.Combine(dataDir, LauncherStorageNames.StartupSceneSnapshot)
        );
}
