using System;

namespace STS2Mobile.Launcher;

internal readonly struct LauncherMainMenuReadinessSnapshot
{
    internal LauncherMainMenuReadinessSnapshot(string attemptId, bool isReady)
    {
        AttemptId = attemptId;
        IsReady = isReady;
    }

    internal string AttemptId { get; }
    internal bool IsReady { get; }
}

internal sealed class LauncherMainMenuReadinessOwner
{
    private readonly object _lock = new();
    private string _attemptId;
    private bool _isReady;

    internal bool Begin(string attemptId)
    {
        ValidateAttemptId(attemptId);

        lock (_lock)
        {
            if (string.Equals(_attemptId, attemptId, StringComparison.Ordinal))
                return false;

            _attemptId = attemptId;
            _isReady = false;
            return true;
        }
    }

    internal bool MarkReady(string attemptId)
    {
        lock (_lock)
        {
            if (!IsActiveLocked(attemptId) || _isReady)
                return false;

            _isReady = true;
            return true;
        }
    }

    internal bool IsReady(string attemptId)
    {
        lock (_lock)
            return IsActiveLocked(attemptId) && _isReady;
    }

    internal bool IsActive(string attemptId)
    {
        lock (_lock)
            return IsActiveLocked(attemptId);
    }

    internal bool Reset(string attemptId)
    {
        lock (_lock)
        {
            if (!IsActiveLocked(attemptId))
                return false;

            _attemptId = null;
            _isReady = false;
            return true;
        }
    }

    internal LauncherMainMenuReadinessSnapshot Capture()
    {
        lock (_lock)
            return new LauncherMainMenuReadinessSnapshot(_attemptId, _isReady);
    }

    private bool IsActiveLocked(string attemptId)
        => !string.IsNullOrWhiteSpace(attemptId)
            && string.Equals(_attemptId, attemptId, StringComparison.Ordinal);

    internal static void ValidateAttemptId(string attemptId)
    {
        if (string.IsNullOrWhiteSpace(attemptId))
            throw new ArgumentException(
                "A launch-attempt ID is required.",
                nameof(attemptId)
            );
    }
}
