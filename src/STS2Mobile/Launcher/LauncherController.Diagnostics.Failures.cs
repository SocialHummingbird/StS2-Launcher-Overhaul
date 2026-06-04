using System;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private enum DiagnosticsExceptionDetail
    {
        MessageOnly,
        FullException,
    }

    private readonly struct DiagnosticsFailureHandling
    {
        private readonly string _context;
        private readonly DiagnosticsExceptionDetail _exceptionDetail;
        private readonly Action<string>? _onFailure;

        private DiagnosticsFailureHandling(
            string context,
            DiagnosticsExceptionDetail exceptionDetail,
            Action<string>? onFailure = null
        )
        {
            _context = context;
            _exceptionDetail = exceptionDetail;
            _onFailure = onFailure;
        }

        internal static DiagnosticsFailureHandling FullException(
            string context,
            Action<string>? onFailure = null
        )
            => new(context, DiagnosticsExceptionDetail.FullException, onFailure);

        internal static DiagnosticsFailureHandling WithDetail(
            string context,
            DiagnosticsExceptionDetail exceptionDetail,
            Action<string>? onFailure = null
        )
            => new(context, exceptionDetail, onFailure);

        internal void Handle(Exception ex)
        {
            PatchHelper.Log(
                $"[Launcher] {_context}: {ExceptionText(ex)}"
            );
            _onFailure?.Invoke(ex.Message);
        }

        internal void Run(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Handle(ex);
            }
        }

        internal string? TryRun(Func<string> action)
        {
            try
            {
                return action();
            }
            catch (Exception ex)
            {
                Handle(ex);
                return default;
            }
        }

        private string ExceptionText(Exception ex)
            => _exceptionDetail == DiagnosticsExceptionDetail.FullException
                ? ex.ToString()
                : ex.Message;
    }

    private void RunDiagnosticsAction(string failureContext, Action action)
    {
        var failure = DiagnosticsFailureHandling.FullException(
            failureContext,
            message => _view.SetStatus($"{failureContext}: {message}")
        );

        failure.Run(action);
    }

    private string? TryWriteDiagnosticsReport(
        string failureContext,
        DiagnosticsExceptionDetail exceptionDetail,
        Action<string>? onFailure = null
    )
    {
        var failure = DiagnosticsFailureHandling.WithDetail(
            failureContext,
            exceptionDetail,
            onFailure
        );

        return failure.TryRun(_model.WriteDiagnosticsReport);
    }
}
