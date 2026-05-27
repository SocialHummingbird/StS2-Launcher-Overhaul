using System;
using System.Reflection;
using System.Threading.Tasks;
using SteamKit2;

namespace STS2Mobile.Steam;

// SteamKit generates a machine_id on a background task when SteamClient is
// constructed. On Android, that worker has repeatedly aborted inside the Godot
// Android runtime during Steam login.
//
// Do not patch SteamKit methods at runtime: Harmony/MonoMod's dynamic wrapper
// generation is also unsafe in this Godot Android runtime. Instead, pre-seed
// SteamKit's private HardwareUtils.generationTable for this configuration's
// MachineInfoProvider. SteamClient's constructor calls HardwareUtils.Init(), but
// ConditionalWeakTable.GetValue will then return our completed task instead of
// starting SteamKit's background worker.
internal static class SteamKitAndroidMachineIdPatch
{
    private static readonly object Lock = new();

    public static void Apply(SteamConfiguration configuration)
    {
        if (!OperatingSystem.IsAndroid())
            return;

        if (configuration == null)
            return;

        lock (Lock)
        {
            try
            {
                var type = Type.GetType("SteamKit2.HardwareUtils, SteamKit2");
                var machineIdType = type?.GetNestedType("MachineID", BindingFlags.NonPublic);
                var tableField = type?.GetField(
                    "generationTable",
                    BindingFlags.NonPublic | BindingFlags.Static
                );

                if (machineIdType == null || tableField == null)
                {
                    PatchHelper.Log("[Auth] SteamKit machine-id patch unavailable");
                    return;
                }

                var machineId = Activator.CreateInstance(machineIdType, nonPublic: true);
                machineIdType.GetMethod("SetBB3")?.Invoke(machineId, new object[] { FortyHex('b') });
                machineIdType.GetMethod("SetFF2")?.Invoke(machineId, new object[] { FortyHex('f') });
                machineIdType.GetMethod("Set3B3")?.Invoke(machineId, new object[] { FortyHex('3') });

                var fromResult = typeof(Task)
                    .GetMethod(nameof(Task.FromResult))
                    ?.MakeGenericMethod(machineIdType);
                var task = fromResult?.Invoke(null, new[] { machineId });
                var table = tableField.GetValue(null);
                var provider = configuration.MachineInfoProvider;

                if (task == null || table == null || provider == null)
                {
                    PatchHelper.Log("[Auth] SteamKit machine-id cache seed unavailable");
                    return;
                }

                try
                {
                    table.GetType().GetMethod("Add")?.Invoke(table, new[] { provider, task });
                }
                catch (TargetInvocationException ex)
                    when (ex.InnerException is ArgumentException)
                {
                    // Already seeded for this provider.
                }

                PatchHelper.Log("[Auth] SteamKit Android machine-id cache seeded");
            }
            catch (Exception ex)
            {
                PatchHelper.Log($"[Auth] SteamKit machine-id cache seed failed: {ex}");
            }
        }
    }

    private static string FortyHex(char value) => new(value, 40);
}
