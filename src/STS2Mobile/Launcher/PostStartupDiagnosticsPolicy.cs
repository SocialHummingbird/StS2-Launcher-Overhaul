using System;
using System.Threading;

namespace STS2Mobile.Launcher;

internal static class PostStartupDiagnosticsPolicy
{
    internal const string EnvironmentVariable = "STS2_DETAILED_POST_STARTUP_TRACE";

    internal static bool DetailedTraceEnabled(string environmentValue, bool markerExists)
    {
        if (markerExists)
            return true;

        if (string.IsNullOrWhiteSpace(environmentValue))
            return false;

        return environmentValue.Trim().ToLowerInvariant() switch
        {
            "1" => true,
            "true" => true,
            "yes" => true,
            "on" => true,
            "enabled" => true,
            _ => false,
        };
    }

    internal static bool ShouldWriteFullDiagnostics(
        bool detailedTraceEnabled,
        bool failureOrRecovery
    )
        => detailedTraceEnabled || failureOrRecovery;
}

internal sealed class PostStartupDiagnosticsScheduleGate
{
    private const int Active = 1;
    private const int Stopped = 2;
    private int _state;

    internal bool IsStopped => Volatile.Read(ref _state) == Stopped;

    internal bool TrySchedule()
        => Interlocked.CompareExchange(ref _state, Active, 0) == 0;

    internal bool Stop()
        => Interlocked.Exchange(ref _state, Stopped) == Active;
}
