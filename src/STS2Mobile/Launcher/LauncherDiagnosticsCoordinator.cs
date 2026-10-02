using System;
using STS2Mobile.Patches;
using Godot;

namespace STS2Mobile.Launcher;
internal sealed partial class LauncherDiagnosticsCoordinator
{
    private readonly LauncherModel _model;
    private readonly LauncherView _view;
    private bool _automaticDiagnosticsWritten;
    internal LauncherDiagnosticsCoordinator(LauncherModel model, LauncherView view)
    {
        _model = model;
        _view = view;
    }

    internal void ShowLastErrorPressed() => RunDiagnosticsAction("Problem summary failed", () => ShowDiagnosticsSummary(_view, _model.BuildDiagnosticsSummaryForDisplay()));
    internal void CopyRawLogPressed() => RunDiagnosticsAction("Launcher log copy failed", () => CopyRawLogToClipboard(_view, _model.BuildRawErrorLogForClipboard()));
    private static void ShowDiagnosticsSummary(LauncherView view, string summary)
    {
        view.SetStatus("Last error opened.", LauncherStatusSeverity.Information);
        view.AppendLog(summary);
        view.ShowDiagnosticsConsole();
    }

    private static void CopyRawLogToClipboard(LauncherView view, string rawLog)
    {
        var clipboardText = new LauncherClipboardText("Public sharing warning: review and redact this launcher log before posting publicly.\n" + "It may include account names, local paths, device details, launcher state, and log excerpts.\n\n" + rawLog);
        clipboardText.CopyToClipboard();
        view.SetStatus("Launcher log copied.", LauncherStatusSeverity.Information);
        view.AppendLog($"Launcher log copied to clipboard ({clipboardText.Length:N0} chars). Review/redact before public posting.");
        view.ShowDiagnosticsConsole();
    }

    internal void DiagnosticsPressed()
    {
        var path = TryWriteDiagnosticsReport(DiagnosticsReportWrite.ManualExport());
        if (path == null)
            return;
        ShowDiagnosticsExportResult(path);
    }

    private void ShowDiagnosticsExportResult(string path)
    {
        _view.SetStatus("Support report ready. Review it before sharing.", LauncherStatusSeverity.Information);
        _view.AppendLog($"Support report saved: {path}");
        _view.ShowDiagnosticsConsole();
        ShareDiagnosticsIfAndroid(path);
    }

    private void ShareDiagnosticsIfAndroid(string path)
    {
        if (!OperatingSystem.IsAndroid())
            return;
        _view.AppendLog(LauncherSharedTextFile.Share(path).AndroidShareSheetLogMessage());
    }

    private readonly struct DiagnosticsReportWrite
    {
        private DiagnosticsReportWrite(string failureContext, bool showFailureStatus)
        {
            FailureContext = failureContext;
            ShowFailureStatus = showFailureStatus;
        }

        internal string FailureContext { get; }
        internal bool ShowFailureStatus { get; }

        internal static DiagnosticsReportWrite ManualExport() => new("Diagnostics export failed", showFailureStatus: true);
        internal static DiagnosticsReportWrite AutomaticSnapshot() => new("Automatic diagnostics snapshot failed", showFailureStatus: false);
    }

    private readonly struct DiagnosticsFailure
    {
        private DiagnosticsFailure(string context, Exception exception, bool showStatus, bool includeStackTrace)
        {
            Context = context;
            Exception = exception;
            ShowStatus = showStatus;
            IncludeStackTrace = includeStackTrace;
        }

        private string Context { get; }
        private Exception Exception { get; }
        private bool ShowStatus { get; }
        private bool IncludeStackTrace { get; }
        private string Detail => IncludeStackTrace ? Exception.ToString() : Exception.Message;

        internal static DiagnosticsFailure ActionFailed(string context, Exception exception) => new(context, exception, showStatus: true, includeStackTrace: true);
        internal static DiagnosticsFailure ReportWriteFailed(string context, Exception exception, bool showStatus) => new(context, exception, showStatus, includeStackTrace: false);
        internal void Show(LauncherView view)
        {
            LogDiagnosticsFailure(Context, Detail);
            if (ShowStatus)
                view.SetStatus($"{Context}. Try again. Technical details were added to the launcher log.", LauncherStatusSeverity.Error);
        }
    }

    private void RunDiagnosticsAction(string failureContext, Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            DiagnosticsFailure.ActionFailed(failureContext, ex).Show(_view);
        }
    }

    private string TryWriteDiagnosticsReport(DiagnosticsReportWrite report)
    {
        try
        {
            return _model.WriteDiagnosticsReport();
        }
        catch (Exception ex)
        {
            DiagnosticsFailure.ReportWriteFailed(report.FailureContext, ex, report.ShowFailureStatus).Show(_view);
            return default;
        }
    }

    private static void LogDiagnosticsFailure(string context, string detail) => PatchHelper.Log($"[Launcher] {context}: {detail}");
    internal void ReportBugPressed() => RunDiagnosticsAction("GitHub issue could not be opened", () =>
    {
        var log = _model.BuildRedactedIssueLog();
        var clipboardAvailable = true;
        try
        {
            new LauncherClipboardText(log).CopyToClipboard();
        }
        catch (System.Exception)
        {
            clipboardAvailable = false;
            _view.AppendLog("The full bug-report log could not be copied. Opening the issue with an excerpt.");
        }

        var url = LauncherGitHubIssue.BuildUrl(log, clipboardAvailable);
        var result = OS.ShellOpen(url);
        if (result != Error.Ok)
        {
            _view.SetStatus(clipboardAvailable ? "Could not open GitHub. The redacted log is copied; open the project's Issues page in your browser and paste it into a new bug report." : "Could not open GitHub or copy the log. Use Create support report, then open the project's Issues page in your browser.", LauncherStatusSeverity.Warning);
            return;
        }

        _view.SetStatus(clipboardAvailable ? "GitHub issue draft opened with launcher logs. Full redacted log copied. Review it and describe the bug before submitting." : "GitHub issue draft opened with a log excerpt. Clipboard unavailable; use Create support report for a full report.", LauncherStatusSeverity.Information);
    });
    private bool _previousLaunchWarningChecked;
    internal void ShowPreviousLaunchWarningIfNeeded()
    {
        if (_previousLaunchWarningChecked)
            return;
        _previousLaunchWarningChecked = true;
        var previousLaunchPhase = LauncherLaunchMarkers.ReadPreviousLaunchPhase();
        if (previousLaunchPhase == null)
            return;
        ShowPreviousLaunchWarning(previousLaunchPhase, LauncherLaunchMarkers.ReadLastLaunchAttempt());
        WriteAutomaticDiagnosticsOnce();
    }

    private void ShowPreviousLaunchWarning(string previousLaunchPhase, LaunchAttemptSummary launchAttempt) => new PreviousLaunchWarning(previousLaunchPhase, launchAttempt).Show(_view);
    private void WriteAutomaticDiagnosticsOnce()
    {
        if (_automaticDiagnosticsWritten)
            return;
        _automaticDiagnosticsWritten = true;
        PatchHelper.Log("[Launcher] Previous launch warning shown; automatic diagnostics deferred to avoid blocking launcher display.");
        _view.AppendLog("No report was collected automatically. Use Create support report after the launcher is visible so it includes this attempt.");
    }

    private readonly struct PreviousLaunchWarning
    {
        private const string LauncherAvailableMessage = "The launcher stayed open so you can recover or collect diagnostics.";
        private const string DiagnosticsActionMessage = "Open Help to create a new support report for this attempt.";
        internal PreviousLaunchWarning(string previousLaunchPhase, LaunchAttemptSummary launchAttempt)
        {
            PreviousLaunchPhase = previousLaunchPhase;
            LaunchAttempt = launchAttempt;
        }

        private string PreviousLaunchPhase { get; }
        private LaunchAttemptSummary LaunchAttempt { get; }

        internal void Show(LauncherView view)
        {
            view.SetStatus(StatusMessage(), LauncherStatusSeverity.Warning);
            view.ShowHomeHelpAction();
            foreach (var line in LogLines())
                view.AppendLog(line);
        }

        private string[] LogLines()
        {
            var lines = new System.Collections.Generic.List<string>
            {
                StatusMessage() + PreviousLaunchPhaseSuffix(),
                LauncherAvailableMessage,
                DiagnosticsActionMessage
            };
            if (LaunchAttempt.Present)
            {
                AddIfPresent(lines, LaunchAttempt.ShortLine());
                AddIfPresent(lines, LaunchAttempt.RuntimeLine());
                AddIfPresent(lines, LaunchAttempt.PathLine());
                AddIfPresent(lines, LaunchAttempt.IdentityLine());
                AddIfPresent(lines, LaunchAttempt.MarkerLine());
                AddIfPresent(lines, LaunchAttempt.ModLine());
                AddIfPresent(lines, LaunchAttempt.TimingLine());
                AddIfPresent(lines, LaunchAttempt.RecoveryHint());
            }

            return lines.ToArray();
        }

        private string StatusMessage()
        {
            var phase = (LaunchAttempt.Phase ?? string.Empty) + " " + (PreviousLaunchPhase ?? string.Empty);
            if (phase.IndexOf("setup", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return "Launch setup failed last time. Try again, then create a new support report if it repeats.";
            if (string.Equals(LaunchAttempt.FilesReady, "false", System.StringComparison.OrdinalIgnoreCase) || phase.IndexOf("readiness", System.StringComparison.OrdinalIgnoreCase) >= 0 || phase.IndexOf("blocked", System.StringComparison.OrdinalIgnoreCase) >= 0 || phase.IndexOf("checking", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return "Game preparation failed last time. Repair selected branch, then try again.";
            return "The game did not appear last time. Try Safe Start.";
        }

        private static void AddIfPresent(System.Collections.Generic.List<string> lines, string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                lines.Add(value);
        }

        private string PreviousLaunchPhaseSuffix() => string.IsNullOrWhiteSpace(PreviousLaunchPhase) ? "" : $" Last phase: {PreviousLaunchPhase}.";
    }
}
