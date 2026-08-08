namespace STS2Mobile.Launcher;

internal sealed class ShaderWarmupPresentationLifecycle
{
    private enum PresentationState
    {
        Pending,
        Visible,
        CleanupRequested,
    }

    private PresentationState _state;

    internal bool StartupCoverRetained => true;
    internal bool WarmupVisible => _state == PresentationState.Visible;
    internal bool InputBlocked => StartupCoverRetained || WarmupVisible;

    internal bool MarkVisible()
    {
        if (_state != PresentationState.Pending)
            return false;

        _state = PresentationState.Visible;
        return true;
    }

    internal bool TryBeginCleanup()
    {
        if (_state == PresentationState.CleanupRequested)
            return false;

        _state = PresentationState.CleanupRequested;
        return true;
    }
}
