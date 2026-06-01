using System;
using SteamKit2;

namespace STS2Mobile.Steam;

internal static partial class SteamConnectionConfigurationFactory
{
    private const string AddMethodName = "Add";
    private const string GenerationTableFieldName = "generationTable";
    private const string HardwareUtilsTypeName = "SteamKit2.HardwareUtils, SteamKit2";
    private const string MachineIdTypeName = "MachineID";
    private const string MachineIdPatchUnavailableLogMessage = "[Auth] SteamKit machine-id patch unavailable";
    private const string MachineIdSeedUnavailableLogMessage = "[Auth] SteamKit machine-id cache seed unavailable";
    private const string Set3B3MethodName = "Set3B3";
    private const string SetBB3MethodName = "SetBB3";
    private const string SetFF2MethodName = "SetFF2";
    private static readonly object MachineIdPatchLock = new();
    private static readonly string BB3SeedValue = FortyHex('b');
    private static readonly string FF2SeedValue = FortyHex('f');
    private static readonly string ThreeB3SeedValue = FortyHex('3');

    private static void SeedAndroidMachineIdCache(SteamConfiguration configuration)
    {
        if (!OperatingSystem.IsAndroid() || configuration == null)
            return;

        lock (MachineIdPatchLock)
        {
            if (TrySeedAndroidMachineIdCache(configuration))
                PatchHelper.Log("[Auth] SteamKit Android machine-id cache seeded");
        }
    }

    private static bool TrySeedAndroidMachineIdCache(SteamConfiguration configuration)
    {
        try
        {
            if (!TryLoadSteamKitMachineIdInternals(out var machineIdType, out var tableField))
            {
                PatchHelper.Log(MachineIdPatchUnavailableLogMessage);
                return false;
            }

            var task = CreateMachineIdTask(machineIdType);
            var table = tableField.GetValue(null);
            var provider = configuration.MachineInfoProvider;

            if (task == null || table == null || provider == null)
            {
                PatchHelper.Log(MachineIdSeedUnavailableLogMessage);
                return false;
            }

            AddMachineIdGenerationTableEntry(table, provider, task);
            return true;
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Auth] SteamKit machine-id cache seed failed: {ex}");
            return false;
        }
    }

    private static string FortyHex(char value) => new(value, 40);
}
