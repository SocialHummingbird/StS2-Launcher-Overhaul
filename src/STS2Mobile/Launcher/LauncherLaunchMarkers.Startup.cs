using System;
using System.IO;
using System.Threading;
using Godot;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static partial class LauncherLaunchMarkers
{
    private const string PhasePrefix = "Phase:";

    internal static string? ReadPreviousLaunchPhase()
        => ReadStartupPhase(logFailures: false);

    internal static void WriteStartupPhase(string phase)
        => WriteStartupPhase(phase, null);

    internal static void WriteStartupPhase(string phase, string detail)
    {
        var entry = StartupPhaseEntry.Create(phase, detail);
        TryWriteMarker(
            StartupMarkerPath,
            entry.MarkerText(),
            "Failed to write startup marker"
        );
        WriteStartupEvidence(entry);
    }

    internal static void RecordPhase(string phase)
        => RecordPhase(phase, null);

    internal static void RecordPhase(string phase, string detail)
    {
        var entry = StartupPhaseEntry.Create(phase, detail);
        WriteStartupEvidence(entry);
    }

    private static void WriteStartupEvidence(StartupPhaseEntry entry)
    {
        TryWriteMarker(
            StartupContextPath,
            entry.ContextText(),
            "Failed to write startup context"
        );
        TryAppendMarker(
            StartupTimelinePath,
            entry.TimelineText(),
            "Failed to append startup timeline"
        );
        PatchHelper.Log(entry.LogText());
    }

    internal static string? ReadStartupPhase()
        => ReadStartupPhase(logFailures: true);

    internal static void ClearStartupMarker()
    {
        TryDeleteMarker(
            StartupMarkerPath,
            "Failed to clear startup marker",
            out _
        );
    }

    private static string? ReadStartupPhase(bool logFailures)
    {
        try
        {
            return ReadStartupPhaseFile();
        }
        catch (Exception ex)
        {
            if (logFailures)
                PatchHelper.Log($"Failed to read startup marker: {ex.Message}");
            return null;
        }
    }

    private static string? ReadStartupPhaseFile()
    {
        if (!File.Exists(StartupMarkerPath))
            return null;

        var lines = File.ReadAllLines(StartupMarkerPath);
        var phase = ReadPhase(lines);
        return string.IsNullOrWhiteSpace(phase) ? null : phase;
    }

    private static string? ReadPhase(string[] lines)
    {
        foreach (var line in lines)
        {
            if (line.StartsWith(PhasePrefix, StringComparison.OrdinalIgnoreCase))
                return line.Substring(PhasePrefix.Length).Trim();
        }

        return lines.Length >= 2 ? lines[1].Trim() : null;
    }

    private readonly struct StartupPhaseEntry
    {
        private StartupPhaseEntry(
            int sequence,
            DateTime utc,
            long elapsedMs,
            string phase,
            string detail
        )
        {
            Sequence = sequence;
            Utc = utc;
            ElapsedMs = elapsedMs;
            Phase = Normalize(phase);
            Detail = Normalize(detail);
        }

        private int Sequence { get; }
        private DateTime Utc { get; }
        private long ElapsedMs { get; }
        private string Phase { get; }
        private string Detail { get; }

        internal static StartupPhaseEntry Create(string phase, string detail)
            => new(
                Interlocked.Increment(ref _phaseSequence),
                DateTime.UtcNow,
                ProcessTimer.ElapsedMilliseconds,
                phase,
                detail
            );

        internal string MarkerText()
            => new[]
            {
                Utc.ToString("O"),
                Phase,
                $"{PhasePrefix} {Phase}",
                $"Detail: {Detail}",
                $"Elapsed ms: {ElapsedMs}",
                $"Sequence: {Sequence}",
            }.JoinLines();

        internal string ContextText()
            => new[]
            {
                "StS2 Mobile startup context",
                $"UTC: {Utc:O}",
                $"{PhasePrefix} {Phase}",
                $"Detail: {Detail}",
                $"Elapsed ms: {ElapsedMs}",
                $"Sequence: {Sequence}",
                $"Data dir: {SafeDataDir()}",
                $"Selected branch: {SafeBranch()}",
            }.JoinLines();

        internal string TimelineText()
            => $"{Utc:O}\tseq={Sequence}\telapsedMs={ElapsedMs}\tphase={Phase}\tdetail={Detail}{System.Environment.NewLine}";

        internal string LogText()
            => $"[StartupTiming] seq={Sequence} elapsedMs={ElapsedMs} phase={Phase} detail={Detail}";

        private static string Normalize(string value)
            => string.IsNullOrWhiteSpace(value)
                ? "<none>"
                : value.Replace('\r', ' ').Replace('\n', ' ').Trim();

        private static string SafeDataDir()
        {
            try
            {
                return OS.GetDataDir();
            }
            catch (Exception ex)
            {
                return $"<unavailable:{ex.GetType().Name}>";
            }
        }

        private static string SafeBranch()
        {
            try
            {
                return LauncherPreferences.ReadGameBranch();
            }
            catch (Exception ex)
            {
                return $"<unavailable:{ex.GetType().Name}>";
            }
        }
    }
}

internal static class LauncherStartupMarkerText
{
    internal static string JoinLines(this string[] lines)
        => string.Join(System.Environment.NewLine, lines) + System.Environment.NewLine;
}
