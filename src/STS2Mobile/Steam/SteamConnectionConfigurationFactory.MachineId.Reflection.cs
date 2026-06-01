using System;
using System.Reflection;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal static partial class SteamConnectionConfigurationFactory
{
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
}
