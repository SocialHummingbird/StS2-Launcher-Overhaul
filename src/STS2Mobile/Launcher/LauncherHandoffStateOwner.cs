using System;
using Godot;

namespace STS2Mobile.Launcher;

internal enum LauncherHandoffState
{
    LauncherVisible,
    HandoffPending,
    GameVisible,
}

internal readonly struct LauncherHandoffStateSnapshot
{
    internal LauncherHandoffStateSnapshot(
        LauncherHandoffState state,
        string attemptId,
        bool mainMenuReady,
        bool activityForeground,
        bool windowFocused
    )
    {
        State = state;
        AttemptId = attemptId;
        MainMenuReady = mainMenuReady;
        ActivityForeground = activityForeground;
        WindowFocused = windowFocused;
    }

    internal LauncherHandoffState State { get; }
    internal string AttemptId { get; }
    internal bool MainMenuReady { get; }
    internal bool ActivityForeground { get; }
    internal bool WindowFocused { get; }
}

internal interface ILauncherHandoffOverlay
{
    bool IsAvailable { get; }
    bool IsVisible { get; }
    bool Dismiss();
    bool ReturnToLauncher(string attemptId);
}

internal sealed class LauncherHandoffStateOwner
{
    private readonly object _lock = new();
    private readonly bool _recordDiagnostics;
    private LauncherHandoffState _state = LauncherHandoffState.LauncherVisible;
    private string _attemptId;
    private bool _mainMenuReady;
    private bool _activityForeground;
    private bool _windowFocused;
    private ILauncherHandoffOverlay _overlay;

    internal static LauncherHandoffStateOwner Shared { get; } = new(
        recordDiagnostics: true
    );

    internal LauncherHandoffStateOwner()
        : this(recordDiagnostics: false)
    {
    }

    private LauncherHandoffStateOwner(bool recordDiagnostics)
    {
        _recordDiagnostics = recordDiagnostics;
    }

    internal LauncherHandoffStateSnapshot Capture()
    {
        lock (_lock)
            return CaptureLocked();
    }

    internal LauncherUI ShowLauncher(Node parent, bool inGameMode)
    {
        ArgumentNullException.ThrowIfNull(parent);

        lock (_lock)
        {
            if (_state == LauncherHandoffState.GameVisible)
                throw new InvalidOperationException(
                    "A completed handoff cannot attach another launcher overlay."
                );
            if (_overlay?.IsAvailable == true)
                throw new InvalidOperationException(
                    "The authoritative launcher overlay is already attached."
                );

            _overlay = null;

            var overlay = LauncherHandoffOverlay.Show(parent, inGameMode);
            _overlay = overlay;
            return overlay.Launcher;
        }
    }

    internal Label ShowStartupStatus(string attemptId, Node parent)
    {
        ArgumentNullException.ThrowIfNull(parent);

        Label status;
        lock (_lock)
        {
            if (!IsCurrentPending(attemptId))
                return null;
            if (_overlay is not LauncherHandoffOverlay overlay)
                throw new InvalidOperationException(
                    "The launcher overlay must be attached before startup status is shown."
                );

            status = overlay.ShowStartupStatus(parent);
        }

        if (status != null)
            Record(LauncherHandoffEvent.OverlayShown, attemptId, true, "surface_created");
        return status;
    }

    internal bool AttachOverlay(ILauncherHandoffOverlay overlay)
    {
        ArgumentNullException.ThrowIfNull(overlay);

        lock (_lock)
        {
            if (
                _state == LauncherHandoffState.GameVisible
                || _overlay?.IsAvailable == true
            )
                return false;

            _overlay = overlay;
            return true;
        }
    }

    internal bool Begin(string attemptId)
        => BeginPending(attemptId);

    internal bool RestorePending(string attemptId)
    {
        ValidateAttemptId(attemptId);

        lock (_lock)
        {
            if (
                _state == LauncherHandoffState.HandoffPending
                && string.Equals(_attemptId, attemptId, StringComparison.Ordinal)
            )
                return true;
        }

        return BeginPending(attemptId, recordLaunchRequested: false);
    }

    private bool BeginPending(
        string attemptId,
        bool recordLaunchRequested = true
    )
    {
        ValidateAttemptId(attemptId);

        lock (_lock)
        {
            if (_state != LauncherHandoffState.LauncherVisible)
                return false;

            _state = LauncherHandoffState.HandoffPending;
            _attemptId = attemptId;
            _mainMenuReady = false;
            _activityForeground = false;
            _windowFocused = false;
        }

        if (recordLaunchRequested)
            Record(LauncherHandoffEvent.LaunchRequested, attemptId, true, "launcher_ready");
        return true;
    }

    private static void ValidateAttemptId(string attemptId)
    {
        if (string.IsNullOrWhiteSpace(attemptId))
            throw new ArgumentException(
                "A launch-attempt ID is required.",
                nameof(attemptId)
            );
    }

    internal bool MarkMainMenuReady(string attemptId)
    {
        bool overlayVisible;
        PromotionResult promotion;
        lock (_lock)
        {
            if (!IsCurrentPending(attemptId) || _mainMenuReady)
                return false;

            _mainMenuReady = true;
            overlayVisible = _overlay?.IsVisible == true;
            promotion = PromoteWhenVisibleLocked();
        }

        Record(LauncherHandoffEvent.MainMenuReady, attemptId, overlayVisible, "main_menu_ready");
        RecordPromotion(attemptId, promotion);
        return true;
    }

    internal bool ObserveVisibility(
        string attemptId,
        bool activityForeground,
        bool windowFocused
    )
    {
        bool focusChanged;
        bool stateChanged;
        bool mainMenuReady;
        bool overlayVisible;
        PromotionResult promotion;
        lock (_lock)
        {
            if (!IsCurrentPending(attemptId))
                return false;

            focusChanged = _windowFocused != windowFocused;
            stateChanged = _activityForeground != activityForeground
                || focusChanged;
            _activityForeground = activityForeground;
            _windowFocused = windowFocused;
            mainMenuReady = _mainMenuReady;
            overlayVisible = _overlay?.IsVisible == true;
            promotion = PromoteWhenVisibleLocked();
        }

        if (focusChanged)
        {
            Record(
                windowFocused
                    ? LauncherHandoffEvent.WindowFocusGained
                    : LauncherHandoffEvent.WindowFocusLost,
                attemptId,
                overlayVisible,
                mainMenuReady ? "main_menu_ready" : "not_ready"
            );
        }
        RecordPromotion(attemptId, promotion);
        return stateChanged || promotion != PromotionResult.None;
    }

    internal bool Cancel(string attemptId)
        => ReturnToLauncher(attemptId, recordFailure: false);

    internal bool Fail(string attemptId)
        => ReturnToLauncher(attemptId, recordFailure: true);

    private bool ReturnToLauncher(string attemptId, bool recordFailure)
    {
        bool overlayVisible;
        bool launcherRestored;
        lock (_lock)
        {
            if (!IsCurrentPending(attemptId))
                return false;

            launcherRestored = _overlay?.ReturnToLauncher(attemptId) != false;
            if (!launcherRestored)
                _overlay = null;
            ResetToLauncherLocked();
            overlayVisible = _overlay?.IsVisible == true;
        }

        if (recordFailure)
        {
            Record(
                LauncherHandoffEvent.HandoffFailed,
                attemptId,
                overlayVisible,
                launcherRestored ? "handoff_failed" : "launcher_restore_failed"
            );
        }
        return true;
    }

    private bool IsCurrentPending(string attemptId)
        => _state == LauncherHandoffState.HandoffPending
            && !string.IsNullOrWhiteSpace(attemptId)
            && string.Equals(_attemptId, attemptId, StringComparison.Ordinal);

    private PromotionResult PromoteWhenVisibleLocked()
    {
        if (!_mainMenuReady || !_activityForeground || !_windowFocused)
            return PromotionResult.None;

        if (_overlay != null && !_overlay.Dismiss())
        {
            ResetToLauncherLocked();
            return PromotionResult.OverlayDismissFailed;
        }

        _overlay = null;
        _state = LauncherHandoffState.GameVisible;
        return PromotionResult.GameVisible;
    }

    private void ResetToLauncherLocked()
    {
        _state = LauncherHandoffState.LauncherVisible;
        _attemptId = null;
        _mainMenuReady = false;
        _activityForeground = false;
        _windowFocused = false;
    }

    private void RecordPromotion(string attemptId, PromotionResult promotion)
    {
        if (promotion == PromotionResult.GameVisible)
        {
            Record(LauncherHandoffEvent.OverlayHidden, attemptId, false, "main_menu_ready");
            Record(LauncherHandoffEvent.HandoffCompleted, attemptId, false, "handoff_completed");
        }
        else if (promotion == PromotionResult.OverlayDismissFailed)
        {
            Record(LauncherHandoffEvent.HandoffFailed, attemptId, true, "overlay_dismiss_failed");
        }
    }

    private void Record(
        string eventName,
        string attemptId,
        bool overlayVisible,
        string godotReadiness
    )
    {
        if (!_recordDiagnostics)
            return;

        LauncherHandoffDiagnostics.Record(
            eventName,
            overlayVisible,
            godotReadiness,
            attemptId
        );
    }

    private LauncherHandoffStateSnapshot CaptureLocked()
        => new(
            _state,
            _attemptId,
            _mainMenuReady,
            _activityForeground,
            _windowFocused
        );

    private enum PromotionResult
    {
        None,
        GameVisible,
        OverlayDismissFailed,
    }
}
