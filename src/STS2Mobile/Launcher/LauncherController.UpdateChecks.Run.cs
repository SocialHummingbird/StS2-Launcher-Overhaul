using System;
using System.Threading.Tasks;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private void RunUpdateCheck()
        => _ = RunUpdateCheckAsync();

    private async Task RunUpdateCheckAsync()
    {
        if (_updateCheckRunning)
            return;

        _updateCheckRunning = true;
        SetUpdateCheckBusy(busy: true);

        try
        {
            await CheckForUpdatesAsync();
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] Check for updates failed: {ex}");
            FailUpdateCheck(ex.Message);
        }
        finally
        {
            _updateCheckRunning = false;
            SetUpdateCheckBusy(busy: false);
        }
    }

    private void SetUpdateCheckBusy(bool busy)
        => _view.SetUpdateCheckBusy(busy);
}
