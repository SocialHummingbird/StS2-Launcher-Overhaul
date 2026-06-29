using System;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherDiagnosticsCoordinator
{
    private readonly struct DiagnosticsReportWrite
    {
        private DiagnosticsReportWrite(string failureContext, bool showFailureStatus)
        {
            FailureContext = failureContext;
            ShowFailureStatus = showFailureStatus;
        }

        internal string FailureContext { get; }
        internal bool ShowFailureStatus { get; }

        internal static DiagnosticsReportWrite ManualExport()
            => new(
                "Diagnostics export failed",
                showFailureStatus: true
            );

        internal static DiagnosticsReportWrite AutomaticSnapshot()
            => new(
                "Automatic diagnostics snapshot failed",
                showFailureStatus: false
            );
    }

    private readonly struct DiagnosticsFailure
    {
        private DiagnosticsFailure(
            string context,
            Exception exception,
            bool showStatus,
            bool includeStackTrace
        )
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
        private string Detail => IncludeStackTrace
            ? Exception.ToString()
            : Exception.Message;

        internal static DiagnosticsFailure ActionFailed(
            string context,
            Exception exception
        )
            => new(
                context,
                exception,
                showStatus: true,
                includeStackTrace: true
            );

        internal static DiagnosticsFailure ReportWriteFailed(
            string context,
            Exception exception,
            bool showStatus
        )
            => new(
                context,
                exception,
                showStatus,
                includeStackTrace: false
            );

        internal void Show(LauncherView view)
        {
            LogDiagnosticsFailure(Context, Detail);
            if (ShowStatus)
                view.SetStatus($"{Context}: {Exception.Message}");
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
            DiagnosticsFailure
                .ReportWriteFailed(
                    report.FailureContext,
                    ex,
                    report.ShowFailureStatus
                )
                .Show(_view);
            return default;
        }
    }

    private static void LogDiagnosticsFailure(
        string context,
        string detail
    )
        => PatchHelper.Log($"[Launcher] {context}: {detail}");
}
