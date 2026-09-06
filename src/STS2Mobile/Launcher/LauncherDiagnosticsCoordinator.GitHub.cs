using Godot;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherDiagnosticsCoordinator
{
    internal void ReportBugPressed()
        => RunDiagnosticsAction("GitHub issue could not be opened", () =>
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
                _view.SetStatus(clipboardAvailable
                    ? "Could not open GitHub. The redacted log is copied; open the project's Issues page in your browser and paste it into a new bug report."
                    : "Could not open GitHub or copy the log. Use Create support report, then open the project's Issues page in your browser.", LauncherStatusSeverity.Warning);
                return;
            }
            _view.SetStatus(clipboardAvailable
                ? "GitHub issue draft opened with launcher logs. Full redacted log copied. Review it and describe the bug before submitting."
                : "GitHub issue draft opened with a log excerpt. Clipboard unavailable; use Create support report for a full report.", LauncherStatusSeverity.Information);
        });
}
