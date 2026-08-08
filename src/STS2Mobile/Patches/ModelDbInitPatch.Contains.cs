using System;

namespace STS2Mobile.Patches;

internal static partial class ModelDbInitPatch
{
    private static bool _suppressContains = false;
    private static Type _constructingType = null;

    private static bool ContainsPrefix(Type __0, ref bool __result)
    {
        if (_suppressContains && __0 == _constructingType)
        {
            __result = false;
            return false;
        }

        return true;
    }
}
