namespace STS2Mobile.Launcher;

internal readonly record struct LauncherAutomaticSyncPreparation(
    bool CanContinue,
    string Message
)
{
    internal static LauncherAutomaticSyncPreparation Ready(string message)
        => new(true, message ?? "Automatic save synchronization completed.");

    internal static LauncherAutomaticSyncPreparation Blocked(string message)
        => new(false, string.IsNullOrWhiteSpace(message)
            ? "Launch blocked because automatic save synchronization did not complete."
            : message);
}
