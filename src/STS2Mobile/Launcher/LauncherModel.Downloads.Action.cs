using System;
using System.Threading.Tasks;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal partial class LauncherModel
{
    private const string NotConnectedMessage = "Not connected";

    private readonly struct DepotConnectionAction
    {
        private DepotConnectionAction(
            Action<string> raiseFailure,
            Func<SteamConnection, Task> run
        )
        {
            RaiseFailure = raiseFailure;
            Run = run;
        }

        private Action<string> RaiseFailure { get; }
        private Func<SteamConnection, Task> Run { get; }

        internal static DepotConnectionAction Download(LauncherModel model)
            => new(
                model.RaiseDownloadFailed,
                connection =>
                {
                    model.BeginDownload(connection);
                    return model.RunDownloadAsync();
                }
            );

        internal static DepotConnectionAction UpdateCheck(LauncherModel model)
            => new(
                model.RaiseUpdateCheckFailed,
                model.CheckForUpdatesWithConnectionAsync
            );

        internal static DepotConnectionAction BranchCatalogRefresh(LauncherModel model)
            => new(
                model.RaiseBranchCatalogRefreshFailed,
                model.RefreshBranchCatalogWithConnectionAsync
            );

        internal static DepotConnectionAction WorkshopSync(LauncherModel model)
            => new(
                model.RaiseWorkshopSyncFailed,
                model.SyncWorkshopWithConnectionAsync
            );

        internal async Task RunAsync(SteamConnection connection)
            => await Run(connection).ConfigureAwait(false);

        internal void FailNotConnected()
            => RaiseFailure(NotConnectedMessage);
    }
}
