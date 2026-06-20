using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace STS2Mobile.Patches;

internal static partial class ModelDbInitPatch
{
    private readonly struct ConstructorRunResult
    {
        internal ConstructorRunResult(int successCount, List<Type> failed)
        {
            SuccessCount = successCount;
            Failed = failed;
        }

        internal int SuccessCount { get; }
        internal List<Type> Failed { get; }
    }

    private static ConstructorRunResult RunConstructors(
        IEnumerable<Type> types,
        IReadOnlyDictionary<Type, object> typeObjects
    )
    {
        // Phase 2: Run constructors on pre-allocated objects
        PatchHelper.Log("Phase 2: Running constructors");

        int successCount = 0;
        var failed = new List<Type>();

        _suppressContains = true;
        try
        {
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
                    LogConstructorFailure(type, ex);
                }
            }
        }
        finally
        {
            _suppressContains = false;
        }

        return new ConstructorRunResult(successCount, failed);
    }

    private static void LogConstructorFailure(Type type, Exception ex)
    {
        var inner = ex;
        while (inner.InnerException != null)
            inner = inner.InnerException;
        PatchHelper.Log(
            $"Phase 2 - Failed {type.Name}: {inner.GetType().Name}: {inner.Message}"
        );
    }

    private static void LogPhase2Result(int totalCount, int successCount, IReadOnlyList<Type> failed)
    {
        if (failed.Count > 0)
        {
            PatchHelper.Log($"WARNING: {failed.Count}/{totalCount} types had constructor errors:");
            foreach (var type in failed)
                PatchHelper.Log($"  - {type.FullName}");
        }
        else
        {
            PatchHelper.Log($"All {successCount} model types registered successfully");
        }
    }
}
