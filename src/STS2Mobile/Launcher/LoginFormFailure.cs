using System;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal readonly struct LoginFormFailure
{
    private LoginFormFailure(
        string logContext,
        string statusPrefix,
        string recoveryMessage,
        bool logFullException
    )
    {
        LogContext = logContext;
        StatusPrefix = statusPrefix;
        RecoveryMessage = recoveryMessage;
        LogFullException = logFullException;
    }

    private string LogContext { get; }
    private string StatusPrefix { get; }
    private string RecoveryMessage { get; }
    private bool LogFullException { get; }

    internal static LoginFormFailure LoginHandler()
        => new(
            "Login handler failed",
            "Login failed",
            "Retry sign-in; Steam passwords are not stored by StS2 Launcher.",
            logFullException: true
        );

    internal static LoginFormFailure AutoConnect()
        => new(
            "Auto-connect failed",
            "Connection failed",
            "Check the connection or sign in again if prompted; Steam passwords are not stored by StS2 Launcher.",
            logFullException: true
        );

    internal static LoginFormFailure LocalCredentialHandoff()
        => new(
            "Local Steam credential handoff failed",
            "Login failed",
            "Retry sign-in; Steam passwords are not stored by StS2 Launcher.",
            logFullException: true
        );

    internal void Show(LauncherView view, Exception ex)
    {
        var authFailure = SteamAuthFailureReport.From(ex);
        LauncherLaunchMarkers.RecordSteamAuthFailure(authFailure, LogContext);
        PatchHelper.Log($"[Launcher] {LogContext}: {LogDetail(ex)}");
        view.SetStatus(
            $"{StatusPrefix}: {authFailure.UserMessage} {RecoveryMessage}",
            LauncherStatusSeverity.Error
        );
        view.SetLoginFormVisible(visible: true, disabled: false);
    }

    private string LogDetail(Exception ex)
        => LogFullException ? ex.ToString() : ex.Message;
}
