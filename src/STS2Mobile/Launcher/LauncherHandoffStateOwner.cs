using System;
using System.Threading.Tasks;
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

internal readonly struct LauncherHandoffStateObservation
{
    internal LauncherHandoffStateObservation(
        LauncherHandoffStateSnapshot state,
        Task changed
    )
    {
        State = state;
        Changed = changed;
    }

    internal LauncherHandoffStateSnapshot State { get; }
    internal Task Changed { get; }
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
    private readonly LauncherMainMenuReadinessOwner _mainMenuReadiness = new();
    private TaskCompletionSource<bool> _changed = CreateChangeSignal();
    private LauncherHandoffState _state = LauncherHandoffState.LauncherVisible;
    private bool _activityForeground;
    private bool _windowFocused;
    private ILauncherHandoffOverlay _overlay;
    private LauncherStartupOperation _operation;

    internal LauncherStartupOperation GetOperation(string attemptId)
    {
        lock (_lock)
            return _operation?.AttemptId == attemptId ? _operation : null;
    }

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

    internal LauncherHandoffStateObservation CaptureObservation()
    {
        lock (_lock)
            return new LauncherHandoffStateObservation(
                CaptureLocked(),
                _changed.Task
            );
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
            SignalChangedLocked();
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
            SignalChangedLocked();
            return true;
        }
    }

    internal bool Begin(string attemptId)
        => BeginPending(attemptId);

    internal bool RestorePending(string attemptId)
    {
        lock (_lock)
        {
            if (
                _state == LauncherHandoffState.HandoffPending
                && _mainMenuReadiness.IsActive(attemptId)
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
        LauncherMainMenuReadinessOwner.ValidateAttemptId(attemptId);

        lock (_lock)
        {
            if (_state != LauncherHandoffState.LauncherVisible)
                return false;
            if (!_mainMenuReadiness.Begin(attemptId))
                return false;

            _state = LauncherHandoffState.HandoffPending;
            _operation = new LauncherStartupOperation(attemptId, ex =>
            {
                if (_recordDiagnostics)
                    PatchHelper.Log($"[Launch] Owned startup task fault attempt={attemptId}: {ex}");
            });
            _activityForeground = false;
            _windowFocused = false;
            SignalChangedLocked();
        }

        if (recordLaunchRequested)
            Record(LauncherHandoffEvent.LaunchRequested, attemptId, true, "launcher_ready");
        return true;
    }

    internal bool MarkMainMenuReady(string attemptId)
    {
        bool overlayVisible;
        PromotionResult promotion;
        lock (_lock)
        {
            if (
                !IsCurrentPending(attemptId)
                || !_mainMenuReadiness.MarkReady(attemptId)
            )
                return false;

            overlayVisible = _overlay?.IsVisible == true;
            promotion = PromoteWhenVisibleLocked();
            SignalChangedLocked();
        }

        Record(LauncherHandoffEvent.MainMenuReady, attemptId, overlayVisible, "main_menu_ready");
        RecordPromotion(attemptId, promotion);
        return true;
    }

    internal bool MarkActiveMainMenuReady()
    {
        var readiness = _mainMenuReadiness.Capture();
        return !string.IsNullOrWhiteSpace(readiness.AttemptId)
            && MarkMainMenuReady(readiness.AttemptId);
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
            mainMenuReady = _mainMenuReadiness.IsReady(attemptId);
            overlayVisible = _overlay?.IsVisible == true;
            promotion = PromoteWhenVisibleLocked();
            if (stateChanged || promotion != PromotionResult.None)
                SignalChangedLocked();
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
            SignalChangedLocked();
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
            && _mainMenuReadiness.IsActive(attemptId);

    private PromotionResult PromoteWhenVisibleLocked()
    {
        var readiness = _mainMenuReadiness.Capture();
        if (!readiness.IsReady || !_activityForeground || !_windowFocused)
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
        _operation?.Fail();
        var readiness = _mainMenuReadiness.Capture();
        if (!string.IsNullOrWhiteSpace(readiness.AttemptId))
            _mainMenuReadiness.Reset(readiness.AttemptId);

        _state = LauncherHandoffState.LauncherVisible;
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
    {
        var readiness = _mainMenuReadiness.Capture();
        return new LauncherHandoffStateSnapshot(
            _state,
            readiness.AttemptId,
            readiness.IsReady,
            _activityForeground,
            _windowFocused
        );
    }

    private void SignalChangedLocked()
    {
        var changed = _changed;
        _changed = CreateChangeSignal();
        changed.TrySetResult(true);
    }

    private static TaskCompletionSource<bool> CreateChangeSignal()
        => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private enum PromotionResult
    {
        None,
        GameVisible,
        OverlayDismissFailed,
    }
}
