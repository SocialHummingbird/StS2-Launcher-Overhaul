namespace STS2Mobile.Patches;

internal static partial class ModelDbInitPatch
{
    private static bool _suppressContains = false;

    private static bool ContainsPrefix(ref bool __result)
    {
        if (_suppressContains)
        {
            __result = false;
            return false;
        }

        return true;
    }
}
