using System;
using System.Threading.Tasks;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal partial class LauncherModel
{
    private async Task CheckForUpdatesWithConnectionAsync(SteamConnection connection)
    {
        try
        {
            using var downloader = CreateDownloader(connection);
            bool hasUpdate = await downloader.CheckForUpdatesAsync().ConfigureAwait(false);
            RaiseUpdateCheckCompleted(hasUpdate);
        }
        catch (Exception ex)
        {
            RaiseUpdateCheckFailed(ex.Message);
        }
    }

    private async Task RefreshBranchCatalogWithConnectionAsync(SteamConnection connection)
    {
        try
        {
            using var downloader = CreateDownloader(connection);
            await downloader.RefreshBranchCatalogAsync().ConfigureAwait(false);
            RaiseBranchCatalogRefreshCompleted();
        }
        catch (Exception ex)
        {
            RaiseBranchCatalogRefreshFailed(ex.Message);
        }
    }
}
