using System;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private readonly struct LoginFormFailure
    {
        private LoginFormFailure(
            string logContext,
            string statusPrefix,
            bool logFullException,
            bool showBaseException
        )
        {
            LogContext = logContext;
            StatusPrefix = statusPrefix;
            LogFullException = logFullException;
            ShowBaseException = showBaseException;
        }

        private string LogContext { get; }
        private string StatusPrefix { get; }
        private bool LogFullException { get; }
        private bool ShowBaseException { get; }

        internal static LoginFormFailure LoginHandler()
            => new(
                "Login handler failed",
                "Login failed",
                logFullException: true,
                showBaseException: true
            );

        internal static LoginFormFailure AutoConnect()
            => new(
                "Auto-connect failed",
                "Connection failed",
                logFullException: true,
                showBaseException: true
            );

        internal static LoginFormFailure LocalCredentialHandoff()
            => new(
                "Local Steam credential handoff failed",
                "Login failed",
                logFullException: true,
                showBaseException: true
            );

        internal void Show(LauncherController controller, Exception ex)
        {
            PatchHelper.Log($"[Launcher] {LogContext}: {LogDetail(ex)}");
            controller.ShowLoginForm($"{StatusPrefix}: {StatusMessage(ex)}");
        }

        private string LogDetail(Exception ex)
            => LogFullException ? ex.ToString() : ex.Message;

        private string StatusMessage(Exception ex)
            => ShowBaseException ? ex.GetBaseException().Message : ex.Message;
    }
}
