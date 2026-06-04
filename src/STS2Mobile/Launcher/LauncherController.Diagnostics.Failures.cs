using System;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private readonly struct DiagnosticsFailureHandling
    {
        private readonly string _context;
        private readonly Func<Exception, string> _formatException;
        private readonly Action<string> _onFailure;

        private DiagnosticsFailureHandling(
            string context,
            Func<Exception, string> formatException,
            Action<string> onFailure = null
        )
        {
            _context = context;
            _formatException = formatException;
            _onFailure = onFailure;
        }

        internal static DiagnosticsFailureHandling FullException(
            string context,
            Action<string> onFailure = null
        )
            => new(context, ex => ex.ToString(), onFailure);

        internal static DiagnosticsFailureHandling MessageOnly(
            string context,
            Action<string> onFailure = null
        )
            => new(context, ex => ex.Message, onFailure);

        internal void Handle(Exception ex)
        {
            PatchHelper.Log($"[Launcher] {_context}: {_formatException(ex)}");
            if (_onFailure != null)
                _onFailure(ex.Message);
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

        internal string TryRun(Func<string> action)
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
    }

    private void RunDiagnosticsAction(string failureContext, Action action)
    {
        var failure = DiagnosticsFailureHandling.FullException(
            failureContext,
            message => _view.SetStatus($"{failureContext}: {message}")
        );

        failure.Run(action);
    }

    private string TryWriteDiagnosticsReport(
        string failureContext,
        Action<string> onFailure = null
    )
    {
        var failure = DiagnosticsFailureHandling.MessageOnly(
            failureContext,
            onFailure
        );

        return failure.TryRun(_model.WriteDiagnosticsReport);
    }
}
