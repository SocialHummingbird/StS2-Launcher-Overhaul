using SessionState = STS2Mobile.Launcher.LauncherModel.SessionState;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private enum SessionDisplayAction
    {
        None,
        StatusOnly,
        Login,
        LoggedIn,
        Failed,
    }

    private readonly struct SessionDisplay
    {
        private SessionDisplay(SessionDisplayAction action, string status)
        {
            Action = action;
            Status = status;
        }

        private SessionDisplayAction Action { get; }
        private string Status { get; }

        internal static SessionDisplay For(SessionState state)
            => state switch
            {
                SessionState.Connecting => StatusOnly("Connecting to Steam..."),
                SessionState.Authenticating => StatusOnly("Authenticating..."),
                SessionState.VerifyingOwnership => StatusOnly("Verifying game ownership..."),
                SessionState.Disconnected => WithoutStatus(SessionDisplayAction.Login),
                SessionState.LoggedIn => WithoutStatus(SessionDisplayAction.LoggedIn),
                SessionState.Failed => WithoutStatus(SessionDisplayAction.Failed),
                _ => WithoutStatus(SessionDisplayAction.None),
            };

        internal void Apply(LauncherController controller)
        {
            controller._view.HideAllSections();

            switch (Action)
            {
                case SessionDisplayAction.StatusOnly:
                    controller._view.SetStatus(Status);
                    break;

                case SessionDisplayAction.Login:
                    controller.ShowLogin();
                    break;

                case SessionDisplayAction.LoggedIn:
                    controller.ShowLoggedIn();
                    break;

                case SessionDisplayAction.Failed:
                    controller.ShowFailed();
                    break;
            }
        }

        private static SessionDisplay StatusOnly(string status)
            => new(SessionDisplayAction.StatusOnly, status);

        private static SessionDisplay WithoutStatus(SessionDisplayAction action)
            => new(action, status: "");
    }

    // Updates visible sections and status text based on session state transitions.
    private void UpdateUI(SessionState state)
    {
        if (ShouldSuppressSessionUpdate(state))
            return;

        ShowSessionState(state);
    }

    private void ShowSessionState(SessionState state)
        => SessionDisplay.For(state).Apply(this);
}
