namespace STS2Mobile.Launcher.Sections;

internal sealed partial class ActionSection
{
    private readonly struct SecondaryButtonVisibility
    {
        private SecondaryButtonVisibility(
            bool update,
            bool redownload,
            bool branch,
            bool support,
            bool safeLaunch,
            bool launch
        )
        {
            Update = update;
            Redownload = redownload;
            Branch = branch;
            Support = support;
            SafeLaunch = safeLaunch;
            Launch = launch;
        }

        internal bool Update { get; }
        internal bool Redownload { get; }
        internal bool Branch { get; }
        internal bool Support { get; }
        internal bool SafeLaunch { get; }
        internal bool Launch { get; }

        internal static SecondaryButtonVisibility LaunchReady(bool showUpdate)
            => new(
                update: showUpdate,
                redownload: true,
                branch: true,
                support: true,
                safeLaunch: true,
                launch: true
            );

        internal static SecondaryButtonVisibility Retry()
            => new(
                update: false,
                redownload: false,
                branch: false,
                support: true,
                safeLaunch: false,
                launch: false
            );

        internal static SecondaryButtonVisibility Hidden()
            => new(
                update: false,
                redownload: false,
                branch: false,
                support: false,
                safeLaunch: false,
                launch: false
            );
    }
}
