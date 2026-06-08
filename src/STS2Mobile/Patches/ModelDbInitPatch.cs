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
internal static class ModelDbInitPatch
{
    private const string AllAbstractModelSubtypesProperty = "AllAbstractModelSubtypes";
    private const string ContainsMethodName = "Contains";
    private const string ContentByIdField = "_contentById";
    private const string GetIdMethodName = "GetId";
    private const string HarmonyId = "com.sts2mobile.modeldb";
    private const string SetItemMethod = "set_Item";

    private static bool _suppressContains = false;

    internal static void Apply(Harmony harmony)
    {
        PatchHelper.Patch(
            harmony,
            typeof(ModelDb),
            "Init",
            prefix: PatchHelper.Method(typeof(ModelDbInitPatch), nameof(InitPrefix))
        );
    }

    private static bool ContainsPrefix(ref bool __result)
    {
        if (_suppressContains)
        {
            __result = false;
            return false;
        }
        return true;
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

        // Phase 2: Run constructors on pre-allocated objects
        PatchHelper.Log("Phase 2: Running constructors");

        _suppressContains = true;

        int successCount = 0;
        var failed = new List<Type>();

        foreach (var type in types)
        {
            if (!typeObjects.ContainsKey(type))
                continue;

            try
            {
                RuntimeHelpers.RunClassConstructor(type.TypeHandle);

                var ctor = type.GetConstructor(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                    null,
                    Type.EmptyTypes,
                    null
                );
                if (ctor != null)
                {
                    ctor.Invoke(typeObjects[type], null);
                }

                successCount++;
            }
            catch (Exception ex)
            {
                failed.Add(type);
                var inner = ex;
                while (inner.InnerException != null)
                    inner = inner.InnerException;
                PatchHelper.Log(
                    $"Phase 2 - Failed {type.Name}: {inner.GetType().Name}: {inner.Message}"
                );
            }
        }

        _suppressContains = false;
        harmony.Unpatch(containsMethod, containsPrefix);

        if (failed.Count > 0)
        {
            PatchHelper.Log($"WARNING: {failed.Count}/{types.Length} types had constructor errors:");
            foreach (var type in failed)
                PatchHelper.Log($"  - {type.FullName}");
        }
        else
        {
            PatchHelper.Log($"All {successCount} model types registered successfully");
        }

        return false;
    }

    private static bool TryLoadModelDbInitAccess(
        out Type[] types,
        out MethodInfo getIdMethod,
        out object contentById,
        out MethodInfo setItemMethod,
        out MethodInfo containsMethod
    )
    {
        types = null;
        getIdMethod = null;
        contentById = null;
        setItemMethod = null;
        containsMethod = null;

        var modelDbType = typeof(ModelDb);

        var allSubtypesProp = modelDbType.GetProperty(
            AllAbstractModelSubtypesProperty,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static
        );
        if (allSubtypesProp == null)
        {
            PatchHelper.Log("ModelDb.Init fallback: AllAbstractModelSubtypes property missing");
            return false;
        }

        types = (Type[])allSubtypesProp.GetValue(null);
        if (types == null || types.Length == 0)
        {
            PatchHelper.Log("ModelDb.Init fallback: no model subtypes were exposed");
            return false;
        }

        getIdMethod = modelDbType.GetMethod(
            GetIdMethodName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
            null,
            new[] { typeof(Type) },
            null
        );
        if (getIdMethod == null)
        {
            PatchHelper.Log("ModelDb.Init fallback: GetId method missing");
            return false;
        }

        var contentByIdField = modelDbType.GetField(
            ContentByIdField,
            BindingFlags.NonPublic | BindingFlags.Static
        );
        if (contentByIdField == null)
        {
            PatchHelper.Log("ModelDb.Init fallback: _contentById field missing");
            return false;
        }

        contentById = contentByIdField.GetValue(null);
        if (contentById == null)
        {
            PatchHelper.Log("ModelDb.Init fallback: _contentById is null");
            return false;
        }

        setItemMethod = contentById.GetType().GetMethod(SetItemMethod);
        if (setItemMethod == null)
        {
            PatchHelper.Log("ModelDb.Init fallback: _contentById.set_Item method missing");
            return false;
        }

        containsMethod = modelDbType.GetMethod(
            ContainsMethodName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
            null,
            new[] { typeof(Type) },
            null
        );
        if (containsMethod == null)
        {
            PatchHelper.Log("ModelDb.Init fallback: Contains(Type) method missing");
            return false;
        }

        return true;
    }
}
