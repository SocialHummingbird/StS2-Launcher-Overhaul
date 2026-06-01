using System;
using System.Reflection;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal static partial class SteamConnectionConfigurationFactory
{
    private readonly struct MachineIdReflection
    {
        internal MachineIdReflection(Type? machineIdType, FieldInfo? tableField)
        {
            MachineIdType = machineIdType;
            TableField = tableField;
        }

        internal Type? MachineIdType { get; }
        internal FieldInfo? TableField { get; }
        internal bool Available => MachineIdType != null && TableField != null;
    }

    private static MachineIdReflection LoadSteamKitMachineIdReflection()
    {
        var type = Type.GetType(HardwareUtilsTypeName);

        return new MachineIdReflection(
            type?.GetNestedType(MachineIdTypeName, BindingFlags.NonPublic),
            type?.GetField(
                GenerationTableFieldName,
                BindingFlags.NonPublic | BindingFlags.Static
            )
        );
    }

    private static bool MachineIdReflectionAvailable(MachineIdReflection reflection)
        => reflection.Available;

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
