using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace STS2Mobile.Patches;

// Replaces ModelDb.Init() with a two-phase initialization to avoid circular dependency
// crashes. Phase 1 pre-populates the registry with uninitialized objects so cross-type
// references resolve during construction. Phase 2 runs the actual constructors.
internal static partial class ModelDbInitPatch
{
    private const string AllAbstractModelSubtypesProperty = "AllAbstractModelSubtypes";
    private const string ContainsMethodName = "Contains";
    private const string ContentByIdField = "_contentById";
    private const string GetIdMethodName = "GetId";
    private const string HarmonyId = "com.sts2mobile.modeldb";
    private const string SetItemMethod = "set_Item";

    internal static void Apply(Harmony harmony)
    {
        PatchHelper.Patch(
            harmony,
            typeof(ModelDb),
            "Init",
            prefix: PatchHelper.Method(typeof(ModelDbInitPatch), nameof(InitPrefix))
        );
    }

    private static bool InitPrefix()
    {
        PatchHelper.Log("Running patched ModelDb.Init()");

        if (!TryLoadModelDbInitAccess(
                out var types,
                out var getIdMethod,
                out var contentById,
                out var setItemMethod,
                out var containsMethod
            ))
        {
            return true;
        }

        // Phase 1: Pre-populate dictionary with uninitialized objects
        PatchHelper.Log($"Phase 1: Pre-registering {types.Length} types with uninitialized objects");

        var typeObjects = new Dictionary<Type, object>();
        int preRegCount = 0;

        for (int i = 0; i < types.Length; i++)
        {
            try
            {
                var type = types[i];
                var id = getIdMethod.Invoke(null, new object[] { type });
                var model = RuntimeHelpers.GetUninitializedObject(type);
                setItemMethod.Invoke(contentById, new[] { id, model });
                typeObjects[type] = model;
                preRegCount++;
            }
            catch (Exception ex)
            {
                PatchHelper.Log($"Phase 1 - Failed to pre-register {types[i].Name}: {ex.Message}");
            }
        }

        PatchHelper.Log($"Phase 1 complete: {preRegCount} types pre-registered");

        // Temporarily suppress Contains() during Phase 2 so constructors don't
        // short-circuit when they check if their type is already registered.
        var harmony = new Harmony(HarmonyId);
        var containsPrefix = typeof(ModelDbInitPatch).GetMethod(
            nameof(ContainsPrefix),
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static
        );
        if (containsPrefix == null)
        {
            PatchHelper.Log("ModelDb.Init fallback: ContainsPrefix method missing");
            return true;
        }
        harmony.Patch(containsMethod, new HarmonyMethod(containsPrefix));

        var phase2 = RunConstructors(types, typeObjects);
        harmony.Unpatch(containsMethod, containsPrefix);

        LogPhase2Result(types.Length, phase2.SuccessCount, phase2.Failed);

        return false;
    }
}
