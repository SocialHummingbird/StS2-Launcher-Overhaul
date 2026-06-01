using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;
using MegaCrit.Sts2.Core.Saves.Migrations;

namespace STS2Mobile.Patches;

// Injects diagnostic logging into ProgressSaveManager.LoadProgress() via transpiler
// to trace why the game creates a fresh default save instead of loading the pulled one.
internal static class SaveDiagnosticPatches
{
    private const string CreateDefaultMethod = "CreateDefault";
    private const string LoadSaveMethod = "LoadSave";

    internal static void Apply(Harmony harmony)
    {
        PatchHelper.Patch(
            harmony,
            typeof(ProgressSaveManager),
            "LoadProgress",
            transpiler: PatchHelper.Method(typeof(SaveDiagnosticPatches), nameof(TranspileLoadProgress))
        );
    }

    private static IEnumerable<CodeInstruction> TranspileLoadProgress(IEnumerable<CodeInstruction> instructions)
    {
        var codes = new List<CodeInstruction>(instructions);
        var injectedLoadSave = false;
        var injectedCreateDefault = false;

        for (var i = 0; i < codes.Count; i++)
        {
            var ci = codes[i];

            if (!injectedLoadSave && IsLoadSaveCall(ci))
            {
                codes.Insert(i + 1, new CodeInstruction(OpCodes.Dup));
                codes.Insert(i + 2, Call(nameof(LogLoadResult)));
                PatchHelper.Log($"[Diag] Injected LoadSave logger at IL[{i}]");
                injectedLoadSave = true;
                i += 2;
            }

            if (!injectedCreateDefault && IsCreateDefaultCall(ci))
            {
                codes.Insert(i, Call(nameof(LogCreatingDefault)));
                PatchHelper.Log($"[Diag] Injected CreateDefault logger at IL[{i}]");
                injectedCreateDefault = true;
                i++;
            }
        }

        if (!injectedLoadSave)
            PatchHelper.Log("[Diag] WARNING: LoadSave call not found in LoadProgress IL");
        if (!injectedCreateDefault)
            PatchHelper.Log("[Diag] WARNING: CreateDefault call not found in LoadProgress IL");

        return codes;
    }

    private static void LogLoadResult(object result)
    {
        try
        {
            var type = result.GetType();
            var status = type.GetProperty("Status")?.GetValue(result);
            var success = type.GetProperty("Success")?.GetValue(result);
            var hasData = type.GetProperty("SaveData")?.GetValue(result) != null;
            var error = type.GetProperty("ErrorMessage")?.GetValue(result);
            PatchHelper.Log(
                $"[Diag] LoadProgress result: Status={status}, "
                    + $"Success={success}, HasData={hasData}, "
                    + $"Error={error ?? "none"}"
            );
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Diag] LogLoadResult failed: {ex.Message}");
        }
    }

    private static void LogCreatingDefault()
    {
        PatchHelper.Log(
            "[Diag] LoadProgress: creating default empty progress (load failed or file missing)"
        );
    }

    private static bool IsLoadSaveCall(CodeInstruction instruction)
    {
        return IsCall(instruction)
            && instruction.operand is MethodInfo method
            && method.Name == LoadSaveMethod
            && method.DeclaringType?.Name == nameof(MigrationManager);
    }

    private static bool IsCreateDefaultCall(CodeInstruction instruction)
    {
        return IsCall(instruction)
            && instruction.operand is MethodInfo method
            && method.Name == CreateDefaultMethod
            && method.DeclaringType?.Name == nameof(ProgressState);
    }

    private static bool IsCall(CodeInstruction instruction)
    {
        return instruction.opcode == OpCodes.Call || instruction.opcode == OpCodes.Callvirt;
    }

    private static CodeInstruction Call(string methodName) =>
        new(
            OpCodes.Call,
            AccessTools.Method(typeof(SaveDiagnosticPatches), methodName)
        );
}
