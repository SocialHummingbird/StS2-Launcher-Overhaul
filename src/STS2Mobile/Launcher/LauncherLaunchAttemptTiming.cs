using System.Diagnostics;

namespace STS2Mobile.Launcher;

internal sealed class LauncherLaunchAttemptTiming
{
    private const long NotMeasuredValue = -1;

    private LauncherLaunchAttemptTiming(
        long attemptElapsedMs,
        long readinessElapsedMs,
        long modReadinessElapsedMs
    )
    {
        AttemptElapsedMs = attemptElapsedMs;
        ReadinessElapsedMs = readinessElapsedMs;
        ModReadinessElapsedMs = modReadinessElapsedMs;
    }

    internal long AttemptElapsedMs { get; }
    internal long ReadinessElapsedMs { get; }
    internal long ModReadinessElapsedMs { get; }

    internal static LauncherLaunchAttemptTiming NotMeasured()
        => new(NotMeasuredValue, NotMeasuredValue, NotMeasuredValue);

    internal static LauncherLaunchAttemptTiming Blocked(
        Stopwatch attemptTimer,
        Stopwatch readinessTimer
    )
        => new(ElapsedMs(attemptTimer), ElapsedMs(readinessTimer), NotMeasuredValue);

    internal static LauncherLaunchAttemptTiming Ready(
        Stopwatch attemptTimer,
        Stopwatch readinessTimer,
        Stopwatch modReadinessTimer
    )
        => new(
            ElapsedMs(attemptTimer),
            ElapsedMs(readinessTimer),
            ElapsedMs(modReadinessTimer)
        );

    internal static LauncherLaunchAttemptTiming Failed(
        Stopwatch attemptTimer,
        Stopwatch readinessTimer,
        Stopwatch modReadinessTimer
    )
        => Ready(attemptTimer, readinessTimer, modReadinessTimer);

    internal string AttemptElapsedText => Format(AttemptElapsedMs);
    internal string ReadinessElapsedText => Format(ReadinessElapsedMs);
    internal string ModReadinessElapsedText => Format(ModReadinessElapsedMs);

    private static long ElapsedMs(Stopwatch timer)
        => timer == null ? NotMeasuredValue : timer.ElapsedMilliseconds;

    private static string Format(long value)
        => value < 0 ? "<not measured>" : value.ToString();
}
