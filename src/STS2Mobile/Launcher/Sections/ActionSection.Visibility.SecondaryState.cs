namespace STS2Mobile.Launcher.Sections;

internal sealed partial class ActionSection
{
    private readonly struct SecondaryButtonVisibility
    {
        private SecondaryButtonVisibility(
            bool update,
            bool redownload,
            bool safeLaunch,
            bool launch
        )
        {
            Update = update;
            Redownload = redownload;
            SafeLaunch = safeLaunch;
            Launch = launch;
        }

        internal bool Update { get; }
        internal bool Redownload { get; }
        internal bool SafeLaunch { get; }
        internal bool Launch { get; }

        internal static SecondaryButtonVisibility LaunchReady(bool showUpdate)
            => new(
                update: showUpdate,
                redownload: true,
                safeLaunch: true,
                launch: true
            );

        internal static SecondaryButtonVisibility Retry()
            => new(
                update: false,
                redownload: false,
                safeLaunch: false,
                launch: false
            );

        internal static SecondaryButtonVisibility Hidden()
            => new(
                update: false,
                redownload: false,
                safeLaunch: false,
                launch: false
            );
    }
}
