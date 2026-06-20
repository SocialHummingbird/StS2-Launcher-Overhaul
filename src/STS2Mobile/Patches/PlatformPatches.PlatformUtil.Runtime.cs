using System;
using System.Reflection;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Platform.Null;

namespace STS2Mobile.Patches;

internal static partial class PlatformPatches
{
    private static bool PlatformUtilStaticConstructorPrefix()
    {
        try
        {
            var strategy = GetAndroidNullPlatformStrategy();
            typeof(PlatformUtil)
                .GetField(NullPlatformField, BindingFlags.NonPublic | BindingFlags.Static)
                ?.SetValue(null, strategy);
            PatchHelper.Log("Skipped PlatformUtil desktop static initialization on Android");
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"PlatformUtil static constructor replacement failed: {ex.Message}");
        }

        return false;
    }

    private static bool PrimaryPlatformPrefix(ref PlatformType __result)
    {
        __result = PlatformType.None;
        return false;
    }

    private static bool GetPlatformUtilPrefix(ref object __result)
    {
        __result = GetAndroidNullPlatformStrategy();
        return false;
    }

    private static object GetAndroidNullPlatformStrategy()
    {
        try
        {
            return _nullPlatformStrategy ??= new NullPlatformUtilStrategy();
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"NullPlatformUtilStrategy unavailable on Android: {ex.Message}");
            return null;
        }
    }
}
