using System;
using System.Text;

namespace STS2Mobile.Launcher;

internal static partial class LauncherDiagnostics
{
    internal sealed partial class Snapshot
    {
        private static readonly ErrorReportPlan SummaryErrorReport =
            ErrorReportPlan.Summary(AppendSummaryErrorDiagnostics);

        private static readonly ErrorReportPlan RawErrorReport =
            ErrorReportPlan.Raw(AppendRawErrorDiagnostics);

        internal string BuildDiagnosticsSummary()
            => SummaryErrorReport.BuildText(this);

        internal string BuildRawErrorLog()
            => RawErrorReport.BuildText(this);

        private void AppendLauncherState(StringBuilder sb, LauncherStateDetail detail)
            => _state.AppendTo(sb, detail);

        private void AppendErrorDiagnostics(
            StringBuilder sb,
            Action<StringBuilder, string> appendDiagnostics
        )
            => appendDiagnostics(sb, _state.DataDir);
    }
}
