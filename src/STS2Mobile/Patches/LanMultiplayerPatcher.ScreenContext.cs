using System;

namespace STS2Mobile.Patches;

internal static partial class LanMultiplayerPatcher
{
    private static void UpdateScreenContext()
    {
        try
        {
            var instance = _activeScreenContextInstance?.GetValue(null);
            _activeScreenContextUpdate?.Invoke(instance, null);
        }
        catch (Exception ex)
        {
            if (!_screenContextUpdateFailureLogged)
            {
                _screenContextUpdateFailureLogged = true;
                PatchHelper.Log($"LAN screen context update failed: {ex.Message}");
            }
        }
    }
}
