using System;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal readonly struct LoginFormFailure
{
    private LoginFormFailure(
        string logContext,
        string statusPrefix,
        string recoveryMessage,
        bool logFullException,
        bool showBaseException
    )
    {
        LogContext = logContext;
        StatusPrefix = statusPrefix;
        RecoveryMessage = recoveryMessage;
        LogFullException = logFullException;
        ShowBaseException = showBaseException;
    }

    private string LogContext { get; }
    private string StatusPrefix { get; }
    private string RecoveryMessage { get; }
    private bool LogFullException { get; }
    private bool ShowBaseException { get; }

    internal static LoginFormFailure LoginHandler()
        => new(
            "Login handler failed",
            "Login failed",
            "Retry sign-in; Steam passwords are not stored by StS2 Mobile.",
            logFullException: true,
            showBaseException: true
        );

    internal static LoginFormFailure AutoConnect()
        => new(
            "Auto-connect failed",
            "Connection failed",
            "Check the connection or sign in again if prompted; Steam passwords are not stored by StS2 Mobile.",
            logFullException: true,
            showBaseException: true
        );

    internal static LoginFormFailure LocalCredentialHandoff()
        => new(
            "Local Steam credential handoff failed",
            "Login failed",
            "Retry sign-in; Steam passwords are not stored by StS2 Mobile.",
            logFullException: true,
            showBaseException: true
        );

    internal void Show(LauncherView view, Exception ex)
    {
        PatchHelper.Log($"[Launcher] {LogContext}: {LogDetail(ex)}");
        view.SetStatus($"{StatusPrefix}: {StatusMessage(ex)}. {RecoveryMessage}");
        view.SetLoginFormVisible(visible: true, disabled: false);
    }

    private string LogDetail(Exception ex)
        => LogFullException ? ex.ToString() : ex.Message;

    private string StatusMessage(Exception ex)
        => ShowBaseException ? ex.GetBaseException().Message : ex.Message;
}
