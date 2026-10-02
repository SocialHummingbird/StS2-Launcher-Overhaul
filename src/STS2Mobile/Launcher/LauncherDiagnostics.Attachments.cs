using System.Collections.Generic;
using System.Text;
using System;
using System.IO;
using System.Linq;
using BootstrapTraceFile = STS2Mobile.BootstrapTrace;

namespace STS2Mobile.Launcher;
internal static partial class LauncherDiagnostics
{
    private static void AppendSummaryErrorDiagnostics(StringBuilder sb, string dataDir)
    {
        AppendAttachments(sb, SummarySmallFiles(dataDir), SummaryFileStatusPrefix, SummaryFileStatusPrefix);
        foreach (var tail in SummaryInterestingTails(dataDir))
            AppendInterestingFileTail(sb, tail);
        AppendLogcatErrorSummary(sb);
    }

    private static void AppendRawErrorDiagnostics(StringBuilder sb, string dataDir)
    {
        AppendAttachments(sb, RawErrorLogFiles(dataDir));
        AppendRawLogcatTail(sb);
    }

    private static void AppendAttachments(StringBuilder sb, IEnumerable<DiagnosticAttachment> files, string missingPrefix = "", string failedPrefix = "")
    {
        foreach (var file in files)
        {
            sb.AppendLine();
            file.AppendHeader(sb);
            file.AppendTruncatedContent(sb, missingPrefix, failedPrefix);
        }
    }

    private static void AppendInterestingFileTail(StringBuilder sb, InterestingDiagnosticTail file)
    {
        sb.AppendLine();
        file.AppendHeader(sb);
        var read = file.Read();
        if (!read.HasContent())
        {
            read.AppendStatus(sb);
            return;
        }

        foreach (var line in file.InterestingLines(read))
            sb.AppendLine(line);
    }

    private const int LargeAttachmentMaxChars = 256 * 1024;
    private const int SmallAttachmentMaxChars = 64 * 1024;
    private static IEnumerable<DiagnosticAttachment> SummarySmallFiles(string dataDir)
    {
        yield return new DiagnosticAttachment(StartupMarker(dataDir), 2048);
        yield return new DiagnosticAttachment(StartupContext(dataDir), 4096);
        yield return new DiagnosticAttachment(MainMenuPreparation(dataDir), 4096);
        yield return new DiagnosticAttachment(PostStartupHeartbeat(dataDir), 4096);
        yield return new DiagnosticAttachment(PostStartupTrace(dataDir), 8192);
        yield return new DiagnosticAttachment(AppLifecycle(dataDir), 4096);
        yield return new DiagnosticAttachment(GraphicsDevice(dataDir), 4096);
        yield return new DiagnosticAttachment(RendererAttempt(dataDir), 4096);
        yield return new DiagnosticAttachment(ProcessExitInfo(dataDir), 16384);
        yield return new DiagnosticAttachment(SteamAuthFailure(dataDir), 4096);
        yield return new DiagnosticAttachment(ShaderWarmupStatus(dataDir), 4096);
        yield return new DiagnosticAttachment(LaunchAttempt(dataDir), 8192);
        yield return new DiagnosticAttachment(StartupTimeline(dataDir), 4096);
        yield return new DiagnosticAttachment(AndroidUncaughtException(dataDir), 4096);
    }

    private static IEnumerable<InterestingDiagnosticTail> SummaryInterestingTails(string dataDir)
    {
        yield return new InterestingDiagnosticTail(BootstrapTrace(), 80);
        yield return new InterestingDiagnosticTail(StartupSceneSnapshot(dataDir), 80);
    }

    private static IEnumerable<DiagnosticAttachment> RawErrorLogFiles(string dataDir)
    {
        yield return new DiagnosticAttachment(StartupMarker(dataDir), SmallAttachmentMaxChars);
        yield return new DiagnosticAttachment(StartupContext(dataDir), SmallAttachmentMaxChars);
        yield return new DiagnosticAttachment(MainMenuPreparation(dataDir), SmallAttachmentMaxChars);
        yield return new DiagnosticAttachment(PostStartupHeartbeat(dataDir), SmallAttachmentMaxChars);
        yield return new DiagnosticAttachment(PostStartupTrace(dataDir), SmallAttachmentMaxChars);
        yield return new DiagnosticAttachment(AppLifecycle(dataDir), SmallAttachmentMaxChars);
        yield return new DiagnosticAttachment(GraphicsDevice(dataDir), SmallAttachmentMaxChars);
        yield return new DiagnosticAttachment(RendererAttempt(dataDir), SmallAttachmentMaxChars);
        yield return new DiagnosticAttachment(ProcessExitInfo(dataDir), SmallAttachmentMaxChars);
        yield return new DiagnosticAttachment(SteamAuthFailure(dataDir), SmallAttachmentMaxChars);
        yield return new DiagnosticAttachment(ShaderWarmupStatus(dataDir), SmallAttachmentMaxChars);
        yield return new DiagnosticAttachment(LaunchAttempt(dataDir), SmallAttachmentMaxChars);
        yield return new DiagnosticAttachment(StartupTimeline(dataDir), SmallAttachmentMaxChars);
        yield return new DiagnosticAttachment(AndroidUncaughtException(dataDir), SmallAttachmentMaxChars);
        yield return new DiagnosticAttachment(BootstrapTrace(), LargeAttachmentMaxChars);
        yield return new DiagnosticAttachment(StartupSceneSnapshot(dataDir), LargeAttachmentMaxChars);
    }

    private readonly struct DiagnosticAttachment
    {
        internal DiagnosticAttachment(DiagnosticFile file, int maxChars)
        {
            File = file;
            MaxChars = maxChars;
        }

        private DiagnosticFile File { get; }
        private int MaxChars { get; }

        internal void AppendHeader(StringBuilder sb) => File.AppendHeader(sb);
        internal FileReadResult Read() => File.Read();
        internal void AppendTruncatedContent(StringBuilder sb, string missingPrefix = "", string failedPrefix = "")
        {
            var read = Read();
            if (read.HasContent())
            {
                sb.AppendLine(TruncatedContent(read));
                return;
            }

            read.AppendStatus(sb, missingPrefix, failedPrefix);
        }

        private string TruncatedContent(FileReadResult read) => TruncateForDisplay(read.ContentText(), MaxChars);
    }

    private const int MaxReportDirectoriesPerDirectory = 40;
    private const int MaxReportFilesPerDirectory = 80;
    private readonly struct DiagnosticDirectory
    {
        internal DiagnosticDirectory(string label, string path, int maxDepth)
        {
            Label = label;
            Path = path;
            MaxDepth = maxDepth;
        }

        private string Label { get; }
        private string Path { get; }
        private int MaxDepth { get; }

        internal void AppendListing(StringBuilder sb)
        {
            sb.AppendLine($"{Label}: {Path}");
            try
            {
                if (!Directory.Exists(Path))
                {
                    sb.AppendLine("  exists=False");
                    return;
                }

                AppendDirectoryTree(sb, Path, depth: 0, maxDepth: MaxDepth);
            }
            catch (Exception ex)
            {
                sb.AppendLine($"  failed={ex.Message}");
            }
        }
    }

    private static void AppendDirectoryListing(StringBuilder sb, DiagnosticDirectory directory) => directory.AppendListing(sb);
    private static void AppendDirectoryTree(StringBuilder sb, string path, int depth, int maxDepth)
    {
        if (depth > maxDepth)
            return;
        var indent = new string (' ', 2 + depth * 2);
        foreach (var dir in Directory.GetDirectories(path).OrderBy(p => p).Take(MaxReportDirectoriesPerDirectory))
        {
            sb.AppendLine($"{indent}[dir] {Path.GetFileName(dir)}");
            AppendDirectoryTree(sb, dir, depth + 1, maxDepth);
        }

        foreach (var filePath in Directory.GetFiles(path).OrderBy(p => p).Take(MaxReportFilesPerDirectory))
        {
            var file = new FileInfo(filePath);
            sb.AppendLine($"{indent}{file.Name} bytes={file.Length} modifiedUtc={file.LastWriteTimeUtc:O}");
        }
    }

    private enum DiagnosticFileMetadataStyle
    {
        MultiLine,
        Inline,
    }

    private readonly struct DiagnosticFileSnapshot
    {
        private DiagnosticFileSnapshot(FileReadResult read, long bytes, DateTime modifiedUtc)
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

        internal void AppendSummary(StringBuilder sb, DiagnosticFile file, long inlineContentLimit)
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
                sb.AppendLine($"  exists=True bytes={Bytes} modifiedUtc={ModifiedUtc:O}");
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

        private static string SingleLine(string text) => text.Replace('\n', ' ').Replace('\r', ' ');
    }

    private readonly struct FileReadResult
    {
        private enum ReadState
        {
            Content,
            Missing,
            Failed,
        }

        private FileReadResult(ReadState state, string text)
        {
            State = state;
            Text = text;
        }

        private ReadState State { get; }
        private string Text { get; }
        private bool HasText => State == ReadState.Content;
        private bool IsMissing => State == ReadState.Missing;

        internal static FileReadResult Read(string text) => new(ReadState.Content, text);
        internal static FileReadResult Missing() => new(ReadState.Missing, string.Empty);
        internal static FileReadResult Failed(string error) => new(ReadState.Failed, error);
        internal void AppendFileStatus(StringBuilder sb) => sb.AppendLine(IsMissing ? "  exists=False" : $"  failed={Text}");
        internal void AppendStatus(StringBuilder sb, string missingPrefix = "", string failedPrefix = "") => sb.AppendLine(Status(missingPrefix, failedPrefix));
        internal string ContentText() => HasText ? Text : string.Empty;
        internal bool HasContent() => HasText;
        internal string[] ContentLines() => ContentText().Replace("\r\n", "\n").Split('\n');
        private string Status(string missingPrefix = "", string failedPrefix = "") => IsMissing ? $"{missingPrefix}<missing>" : $"{failedPrefix}<failed to read: {Text}>";
    }

    private static void AppendFileContentsSections(StringBuilder sb, IEnumerable<DiagnosticFile> files)
    {
        foreach (var file in files)
            file.AppendContentsSection(sb);
    }

    private static void AppendFileSummaries(StringBuilder sb, IEnumerable<DiagnosticFile> files, long inlineContentLimit)
    {
        foreach (var file in files)
            file.AppendSummary(sb, inlineContentLimit);
    }

    private static FileReadResult ReadFileText(string path)
    {
        try
        {
            if (!System.IO.File.Exists(path))
                return FileReadResult.Missing();
            return FileReadResult.Read(System.IO.File.ReadAllText(path));
        }
        catch (Exception ex)
        {
            return FileReadResult.Failed(ex.Message);
        }
    }

    private static string TruncateForDisplay(string text, int maxChars)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxChars)
            return text ?? string.Empty;
        return text.Substring(0, maxChars) + "\n<truncated>";
    }

    private readonly struct DiagnosticFile
    {
        internal DiagnosticFile(string label, string path)
        {
            Label = label;
            Path = path;
        }

        private string Label { get; }
        private string Path { get; }

        internal void AppendHeader(StringBuilder sb) => sb.AppendLine(Header(Label, Path));
        internal void AppendSummary(StringBuilder sb, long inlineContentLimit)
        {
            try
            {
                DiagnosticFileSnapshot.From(this).AppendSummary(sb, this, inlineContentLimit);
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

        private string InspectFailedMessage(Exception ex) => $"{Label}: failed to inspect {Path}: {ex.Message}";
        internal FileInfo Info() => new(Path);
        internal FileReadResult Read() => ReadFileText(Path);
        internal string SummaryLine() => $"{Label}: {Path}";
        internal void WriteAllText(string text) => System.IO.File.WriteAllText(Path, text);
    }

    private static DiagnosticFile AndroidUncaughtException(string dataDir) => new("Android uncaught exception", Path.Combine(dataDir, LauncherStorageNames.AndroidUncaughtException));
    private static DiagnosticFile BootstrapTrace() => new("Bootstrap trace", BootstrapTraceFile.TracePath);
    private static DiagnosticFile GamePck(string dataDir) => new("Game PCK", LauncherGameFiles.PckPath(dataDir));
    private static DiagnosticFile GraphicsDevice(string dataDir) => new("Graphics device", Path.Combine(dataDir, LauncherStorageNames.GraphicsDevice));
    private static DiagnosticFile ManualSafeLaunchMarker(string dataDir) => new("Manual safe launch marker", Path.Combine(dataDir, LauncherStorageNames.ManualSafeLaunch));
    private static DiagnosticFile MainMenuPreparation(string dataDir) => new("Main-menu preparation", Path.Combine(dataDir, LauncherStorageNames.MainMenuPreparation));
    private static DiagnosticFile LaunchAttempt(string dataDir) => new("Launch attempt", Path.Combine(dataDir, LauncherStorageNames.LaunchAttempt));
    private static DiagnosticFile StartupMarker(string dataDir) => new("Startup marker", Path.Combine(dataDir, LauncherStorageNames.StartupMarker));
    private static DiagnosticFile StartupContext(string dataDir) => new("Startup context", Path.Combine(dataDir, LauncherStorageNames.StartupContext));
    private static DiagnosticFile PostStartupTrace(string dataDir) => new("Post-startup trace", Path.Combine(dataDir, LauncherStorageNames.PostStartupTrace));
    private static DiagnosticFile PostStartupHeartbeat(string dataDir) => new("Post-startup heartbeat", Path.Combine(dataDir, LauncherStorageNames.PostStartupHeartbeat));
    private static DiagnosticFile AppLifecycle(string dataDir) => new("App lifecycle", Path.Combine(dataDir, LauncherStorageNames.AppLifecycle));
    private static DiagnosticFile ProcessExitInfo(string dataDir) => new("Historical process exit info", Path.Combine(dataDir, LauncherStorageNames.ProcessExitInfo));
    private static DiagnosticFile RendererAttempt(string dataDir) => new("Renderer attempt", Path.Combine(dataDir, LauncherStorageNames.RendererAttempt));
    private static DiagnosticFile SteamAuthFailure(string dataDir) => new("Steam auth failure", Path.Combine(dataDir, LauncherStorageNames.SteamAuthFailure));
    private static DiagnosticFile ShaderWarmupStatus(string dataDir) => new("Shader warmup status", Path.Combine(dataDir, LauncherStorageNames.ShaderWarmupStatus));
    private static DiagnosticFile StartupTimeline(string dataDir) => new("Startup timeline", Path.Combine(dataDir, LauncherStorageNames.StartupTimeline));
    private static DiagnosticFile StartupSceneSnapshot(string dataDir) => new("Startup scene snapshot", Path.Combine(dataDir, LauncherStorageNames.StartupSceneSnapshot));
}
