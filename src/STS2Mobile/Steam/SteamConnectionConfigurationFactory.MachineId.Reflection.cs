using System;
using System.Reflection;
using System.Threading.Tasks;
using SteamKit2;

namespace STS2Mobile.Steam;

internal static partial class SteamConnectionConfigurationFactory
{
    private readonly struct MachineIdReflection
    {
        private MachineIdReflection(Type? machineIdType, FieldInfo? tableField)
        {
            MachineIdType = machineIdType;
            TableField = tableField;
        }

        private Type? MachineIdType { get; }
        private FieldInfo? TableField { get; }
        private bool Available => MachineIdType != null && TableField != null;

        internal static MachineIdReflection Create(Type? machineIdType, FieldInfo? tableField)
            => new(machineIdType, tableField);

        internal bool TrySeed(SteamConfiguration configuration)
        {
            if (!Available)
            {
                PatchHelper.Log(MachineIdPatchUnavailableLogMessage);
                return false;
            }

            var task = CreateMachineIdTask(MachineIdType!);
            var table = TableField!.GetValue(null);
            var provider = configuration.MachineInfoProvider;

            if (task == null || table == null || provider == null)
            {
                PatchHelper.Log(MachineIdSeedUnavailableLogMessage);
                return false;
            }

            AddMachineIdGenerationTableEntry(table, provider, task);
            return true;
        }
    }

    private static MachineIdReflection LoadSteamKitMachineIdReflection()
    {
        var type = Type.GetType(HardwareUtilsTypeName);

        return MachineIdReflection.Create(
            type?.GetNestedType(MachineIdTypeName, BindingFlags.NonPublic),
            type?.GetField(
                GenerationTableFieldName,
                BindingFlags.NonPublic | BindingFlags.Static
            )
        );
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
}
