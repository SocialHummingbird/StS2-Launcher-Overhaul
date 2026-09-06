using System;

namespace STS2Mobile.Launcher;

internal sealed class LauncherStartGamePlan
{
    private LauncherStartGamePlan(
        bool safe,
        string action,
        string source,
        string buttonPressedPhase,
        string readinessPhase,
        string readinessPassedPhase,
        string blockedPhase,
        string modReadinessPhase,
        string checkingStatus,
        string startingStatus,
        string readyDetail,
        string preLaunchLog
    )
    {
        IsSafe = safe;
        Action = action;
        Source = source;
        ButtonPressedPhase = buttonPressedPhase;
        ReadinessPhase = readinessPhase;
        ReadinessPassedPhase = readinessPassedPhase;
        BlockedPhase = blockedPhase;
        ModReadinessPhase = modReadinessPhase;
        CheckingStatus = checkingStatus;
        StartingStatus = startingStatus;
        ReadyDetail = readyDetail;
        PreLaunchLog = preLaunchLog;
    }

    internal bool IsSafe { get; }
    internal string Action { get; }
    internal string Source { get; }
    internal string ButtonPressedPhase { get; }
    internal string ReadinessPhase { get; }
    internal string ReadinessPassedPhase { get; }
    internal string BlockedPhase { get; }
    internal string ModReadinessPhase { get; }
    internal string CheckingStatus { get; }
    internal string StartingStatus { get; }
    internal string ReadyDetail { get; }
    internal string PreLaunchLog { get; }

    internal static LauncherStartGamePlan Normal()
        => new(
            safe: false,
            action: "normal",
            source: LauncherLaunchSource.Button,
            buttonPressedPhase: "launch button pressed",
            readinessPhase: "launch readiness",
            readinessPassedPhase: "launch readiness passed",
            blockedPhase: "launch blocked",
            modReadinessPhase: "launch mod readiness",
            checkingStatus: "Preparing selected branch for launch...",
            startingStatus: "Preparation passed. Starting the game...",
            readyDetail: "Start Game readiness passed",
            preLaunchLog: ""
        );

    internal static LauncherStartGamePlan Safe()
        => new(
            safe: true,
            action: "safe",
            source: LauncherLaunchSource.Button,
            buttonPressedPhase: "safe launch button pressed",
            readinessPhase: "safe launch readiness",
            readinessPassedPhase: "safe launch readiness passed",
            blockedPhase: "safe launch blocked",
            modReadinessPhase: "safe launch mod readiness",
            checkingStatus: "Preparing selected branch for Safe Start...",
            startingStatus: "Preparation passed. Starting in safe mode...",
            readyDetail: "Safe launch readiness passed",
            preLaunchLog: "Safe Start requested: OpenGL compatibility on PowerVR, project renderer otherwise, no shader warmup, and local saves only for one run."
        );

    internal static LauncherStartGamePlan AutoNormal()
        => new(
            safe: false,
            action: "normal",
            source: LauncherLaunchSource.AutoLaunch,
            buttonPressedPhase: "auto launch requested",
            readinessPhase: "auto launch readiness",
            readinessPassedPhase: "auto launch readiness passed",
            blockedPhase: "auto launch blocked",
            modReadinessPhase: "auto launch mod readiness",
            checkingStatus: "Preparing selected branch for launch...",
            startingStatus: "Preparation passed. Starting the game...",
            readyDetail: "Auto-launch readiness passed",
            preLaunchLog: ""
        );

    internal static LauncherStartGamePlan AutoSafe()
        => new(
            safe: true,
            action: "safe",
            source: LauncherLaunchSource.AutoLaunch,
            buttonPressedPhase: "auto safe launch requested",
            readinessPhase: "auto safe launch readiness",
            readinessPassedPhase: "auto safe launch readiness passed",
            blockedPhase: "auto safe launch blocked",
            modReadinessPhase: "auto safe launch mod readiness",
            checkingStatus: "Preparing selected branch for Safe Start...",
            startingStatus: "Preparation passed. Starting in safe mode...",
            readyDetail: "Auto-safe-launch readiness passed",
            preLaunchLog: "Safe Start requested: OpenGL compatibility on PowerVR, project renderer otherwise, no shader warmup, and local saves only for one run."
        );

    internal LauncherLaunchHandoffResult Launch(
        LauncherModel model,
        LauncherLaunchReadiness readiness,
        LauncherModLaunchReadiness modReadiness,
        string attemptId,
        Func<LauncherLaunchAttemptTiming> timingSnapshot
    )
    {
        if (IsSafe)
            return model.LaunchSafe(readiness, modReadiness, Source, attemptId, timingSnapshot);

        return model.Launch(readiness, modReadiness, Source, attemptId, timingSnapshot);
    }
}
