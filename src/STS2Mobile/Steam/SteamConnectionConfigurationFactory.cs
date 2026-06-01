using System;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SteamKit2;

namespace STS2Mobile.Steam;

internal static class SteamConnectionConfigurationFactory
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

    internal static SteamConfiguration Create()
    {
        AndroidJavaHttpMessageHandler.Prime();

        var config = SteamConfiguration.Create(builder =>
        {
            builder.WithProtocolTypes(OperatingSystem.IsAndroid() ? ProtocolTypes.Tcp : ProtocolTypes.WebSocket);

            if (!OperatingSystem.IsAndroid())
                return;

            builder.WithHttpClientFactory(AndroidJavaHttpMessageHandler.CreateClient);
            builder.WithMachineInfoProvider(new AndroidMachineInfoProvider());
        });

        SeedAndroidMachineIdCache(config);
        return config;
    }

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

    private static bool TryLoadSteamKitMachineIdInternals(out Type machineIdType, out FieldInfo tableField)
    {
        var type = Type.GetType(HardwareUtilsTypeName);
        machineIdType = type?.GetNestedType(MachineIdTypeName, BindingFlags.NonPublic);
        tableField = type?.GetField(
            GenerationTableFieldName,
            BindingFlags.NonPublic | BindingFlags.Static
        );

        return machineIdType != null && tableField != null;
    }

    private static object CreateMachineIdTask(Type machineIdType)
    {
        var machineId = Activator.CreateInstance(machineIdType, nonPublic: true);
        SetMachineIdPart(machineIdType, machineId, SetBB3MethodName, BB3SeedValue);
        SetMachineIdPart(machineIdType, machineId, SetFF2MethodName, FF2SeedValue);
        SetMachineIdPart(machineIdType, machineId, Set3B3MethodName, ThreeB3SeedValue);

        return typeof(Task)
            .GetMethod(nameof(Task.FromResult))
            ?.MakeGenericMethod(machineIdType)
            .Invoke(null, new[] { machineId });
    }

    private static void SetMachineIdPart(Type machineIdType, object machineId, string methodName, string value)
    {
        machineIdType.GetMethod(methodName)?.Invoke(machineId, new object[] { value });
    }

    private static void AddMachineIdGenerationTableEntry(object table, object provider, object task)
    {
        try
        {
            table.GetType().GetMethod(AddMethodName)?.Invoke(table, new[] { provider, task });
        }
        catch (TargetInvocationException ex)
            when (ex.InnerException is ArgumentException)
        {
            // Already seeded for this provider.
        }
    }

    private static string FortyHex(char value) => new(value, 40);

    private sealed class AndroidMachineInfoProvider : IMachineInfoProvider
    {
        private static readonly byte[] MacAddress =
        {
            0x02,
            0x53,
            0x54,
            0x53,
            0x32,
            0x41,
        };

        public byte[] GetMachineGuid() => Encoding.ASCII.GetBytes("sts2launcher-android-machine-guid");

        public byte[] GetMacAddress() => (byte[])MacAddress.Clone();

        public byte[] GetDiskId() => Encoding.ASCII.GetBytes("sts2launcher-android-disk");
    }
}
