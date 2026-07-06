using System;
using System.Diagnostics;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal sealed class LaunchAttemptContext
{
    internal LaunchAttemptContext(
        string attemptId,
        Stopwatch attemptTimer,
        string branch,
        LauncherLaunchReadiness pendingReadiness
    )
    {
        AttemptId = string.IsNullOrWhiteSpace(attemptId)
            ? CreateAttemptId()
            : attemptId;
        AttemptTimer = attemptTimer;
        Branch = SteamGameBranch.Normalize(branch);
        PendingReadiness = pendingReadiness;
    }

    internal string AttemptId { get; }
    internal Stopwatch AttemptTimer { get; }
    internal Stopwatch ReadinessTimer { get; private set; }
    internal Stopwatch ModReadinessTimer { get; private set; }
    internal string Branch { get; }
    internal LauncherLaunchReadiness PendingReadiness { get; }

    internal void StartReadinessTiming()
        => ReadinessTimer = Stopwatch.StartNew();

    internal void StopReadinessTiming()
        => ReadinessTimer?.Stop();

    internal void StartModReadinessTiming()
        => ModReadinessTimer = Stopwatch.StartNew();

    internal void StopModReadinessTiming()
        => ModReadinessTimer?.Stop();

    internal void StopAttemptTiming()
        => AttemptTimer.Stop();

    internal LauncherLaunchAttemptTiming BlockedTiming()
        => LauncherLaunchAttemptTiming.Blocked(AttemptTimer, ReadinessTimer);

    internal LauncherLaunchAttemptTiming FailedTiming()
        => LauncherLaunchAttemptTiming.Failed(AttemptTimer, ReadinessTimer, ModReadinessTimer);

    internal LauncherLaunchAttemptTiming ReadyTiming()
        => LauncherLaunchAttemptTiming.Ready(AttemptTimer, ReadinessTimer, ModReadinessTimer);

    internal static string CreateAttemptId()
        => Guid.NewGuid().ToString("N");
}
