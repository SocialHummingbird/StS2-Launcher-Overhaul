using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using STS2Mobile.Launcher;

namespace STS2Mobile.Patches;

internal static partial class ModLoaderPatches
{
    private const string DiscoveryGate = "Discovery";
    private const string PayloadLoadingGate = "PayloadLoading";
    private const string InitializationGate = "Initialization";
    private const string ActivationGate = "Activation";

    private sealed class RuntimeModGateFact
    {
        public string Gate { get; init; } = "";
        public bool Succeeded { get; init; }
        public string Detail { get; init; } = "";
    }

    private sealed class RuntimeModActivationSummary
    {
        public string SelectionKey { get; init; } = "";
        public string Id { get; init; } = "";
        public bool Discovered { get; init; }
        public bool PayloadLoadingSucceeded { get; init; }
        public bool InitializationSucceeded { get; init; }
        public bool ActivationSucceeded { get; init; }
        public string CompatibilityMode { get; init; } = "";
        public string FirstFailingGate { get; init; } = "";
    }

    private static RuntimeModActivationSummary[] BuildUnavailableActivationEvidence(
        LauncherModLaunchPlan launchPlan,
        string reason
    )
    {
        if (launchPlan == null || launchPlan.Mode != LauncherModPlayMode.Modded)
            return Array.Empty<RuntimeModActivationSummary>();

        var failure = string.IsNullOrWhiteSpace(reason)
            ? "The runtime mod loader was unavailable before discovery could be observed."
            : reason.Trim();
        return launchPlan.EnabledMods
            .Select(mod =>
            {
                var isBaseLib = string.Equals(mod.ManifestId, "BaseLib", StringComparison.Ordinal);
                return new RuntimeModActivationSummary
                {
                    SelectionKey = mod.SelectionKey,
                    Id = mod.ManifestId,
                    CompatibilityMode = isBaseLib ? "partial-android" : "native",
                    FirstFailingGate = DiscoveryGate,
                };
            })
            .ToArray();
    }

    private sealed partial class ModManagerAccess
    {
        internal RuntimeModActivationSummary[] BuildActivationEvidence(
            LauncherModLaunchPlan launchPlan
        )
        {
            if (launchPlan == null || launchPlan.Mode != LauncherModPlayMode.Modded)
                return Array.Empty<RuntimeModActivationSummary>();

            var runtimeMods = (_modsField.GetValue(null) as IEnumerable)?
                .Cast<object>()
                .Where(mod => mod != null)
                .ToArray()
                ?? Array.Empty<object>();
            var evidence = launchPlan.EnabledMods
                .Select(selected => BuildActivationSummary(
                    selected,
                    FindRuntimeMod(selected, runtimeMods)
                ))
                .ToArray();

            PatchHelper.Log(
                $"[Mods] Activation evidence: selected={evidence.Length} "
                    + $"discovered={evidence.Count(mod => mod.Discovered)} "
                    + $"payloadLoaded={evidence.Count(mod => mod.PayloadLoadingSucceeded)} "
                    + $"initialized={evidence.Count(mod => mod.InitializationSucceeded)} "
                    + $"activated={evidence.Count(mod => mod.ActivationSucceeded)} "
                    + $"partial={evidence.Count(mod => mod.CompatibilityMode == "partial-android")} "
                    + $"failed={evidence.Count(mod => !string.IsNullOrWhiteSpace(mod.FirstFailingGate))} "
                    + "uiRegistration=not-exercised"
            );
            return evidence;
        }

        private static RuntimeModActivationSummary BuildActivationSummary(
            LauncherResolvedMod selected,
            object runtimeMod
        )
        {
            var discovered = runtimeMod != null;
            var id = discovered ? TryReadManifestId(runtimeMod) : null;
            var isBaseLib = string.Equals(
                selected.ManifestId,
                "BaseLib",
                StringComparison.Ordinal
            );
            var errorMarkerObserved = TryCountModErrors(runtimeMod, out var errorCount);
            var assembly = discovered ? TryReadAssemblyMember(runtimeMod) : null;
            var hasDll = !string.IsNullOrWhiteSpace(selected.DllPath);
            var hasPck = !string.IsNullOrWhiteSpace(selected.PckPath);
            var loadedState = IsRuntimeModExplicitlyLoaded(runtimeMod);
            var dllAssemblyObserved = !hasDll || assembly != null;
            var pckFileReady = !hasPck || IsNonEmptyPayload(selected.PckPath);
            var pckLoadInferred = !hasPck || (loadedState && pckFileReady);
            var payloadLoaded = discovered
                && loadedState
                && dllAssemblyObserved
                && pckLoadInferred;
            var initializerTypeCount = CountModInitializerTypes(assembly);
            var initialized = payloadLoaded && errorMarkerObserved && errorCount == 0;
            var patchTypeCount = CountHarmonyPatchTypes(assembly);
            var ownerCandidates = RuntimeHarmonyOwnerCandidates(
                selected.ManifestId
            ).ToArray();
            var harmonyDiagnostics = FindHarmonyTargetsForOwners(
                ownerCandidates,
                out var harmonyTargetCount
            );
            var harmonyInspectionSucceeded = !harmonyDiagnostics.Any(detail =>
                detail.StartsWith("<diagnostic failed:", StringComparison.Ordinal)
            );

            var activated = initialized && ActivationWasObserved(
                isBaseLib,
                hasDll,
                hasPck,
                patchTypeCount,
                harmonyTargetCount,
                harmonyInspectionSucceeded
            );
            var gates = new[]
            {
                Gate(
                    DiscoveryGate,
                    discovered,
                    discovered
                        ? $"ModManager contains exact id '{selected.ManifestId}' at '{selected.RootPath}'."
                        : $"No ModManager entry matched exact id '{selected.ManifestId}' and root '{selected.RootPath}'."
                ),
                Gate(
                    PayloadLoadingGate,
                    payloadLoaded,
                    PayloadDetail(
                        discovered,
                        loadedState,
                        hasDll,
                        dllAssemblyObserved,
                        hasPck,
                        pckFileReady,
                        pckLoadInferred
                    )
                ),
                Gate(
                    InitializationGate,
                    initialized,
                    InitializationDetail(
                        payloadLoaded,
                        hasDll,
                        initializerTypeCount,
                        errorMarkerObserved,
                        errorCount
                    )
                ),
                Gate(
                    ActivationGate,
                    activated,
                    ActivationDetail(
                        initialized,
                        isBaseLib,
                        hasDll,
                        hasPck,
                        patchTypeCount,
                        harmonyTargetCount,
                        harmonyInspectionSucceeded
                    )
                ),
            };
            var firstFailure = gates.FirstOrDefault(gate => !gate.Succeeded);
            var compatibilityMode = isBaseLib ? "partial-android" : "native";
            return new RuntimeModActivationSummary
            {
                SelectionKey = selected.SelectionKey,
                Id = id ?? selected.ManifestId,
                Discovered = discovered,
                PayloadLoadingSucceeded = payloadLoaded,
                InitializationSucceeded = initialized,
                ActivationSucceeded = activated,
                CompatibilityMode = compatibilityMode,
                FirstFailingGate = firstFailure?.Gate ?? "",
            };
        }

        private static bool ActivationWasObserved(
            bool isBaseLib,
            bool hasDll,
            bool hasPck,
            int patchTypeCount,
            int harmonyTargetCount,
            bool harmonyInspectionSucceeded
        )
        {
            if (!hasDll && hasPck)
                return false;
            if (isBaseLib)
                return harmonyInspectionSucceeded && harmonyTargetCount > 0;
            return harmonyInspectionSucceeded
                && patchTypeCount > 0
                && harmonyTargetCount > 0;
        }

        private static string PayloadDetail(
            bool discovered,
            bool loadedState,
            bool hasDll,
            bool dllAssemblyObserved,
            bool hasPck,
            bool pckFileReady,
            bool pckLoadInferred
        )
        {
            if (!discovered)
                return "Not attempted because discovery failed.";
            if (!loadedState)
                return "The runtime state marker is missing or is not exactly Loaded.";
            if (hasDll && !dllAssemblyObserved)
                return "The manifest declares a DLL, but ModManager recorded no loaded assembly.";
            if (hasPck && !pckFileReady)
                return "The manifest declares a PCK, but the exact planned PCK is missing or empty.";
            if (hasPck && !pckLoadInferred)
                return "The declared PCK could not be inferred as loaded from the exact Loaded state.";

            var parts = new List<string>();
            if (hasDll)
                parts.Add("the declared DLL has a ModManager assembly marker");
            if (hasPck)
                parts.Add("the declared PCK load is inferred from its non-empty planned file and exact Loaded state");
            if (parts.Count == 0)
                return "No declared payload was available.";

            var detail = string.Join(" and ", parts);
            return char.ToUpperInvariant(detail[0]) + detail[1..] + ".";
        }

        private static string InitializationDetail(
            bool payloadLoaded,
            bool hasDll,
            int initializerTypeCount,
            bool errorMarkerObserved,
            int errorCount
        )
        {
            if (!payloadLoaded)
                return "Not attempted because payload loading did not complete.";
            if (!errorMarkerObserved)
                return "The runtime error marker is missing, so zero initialization errors cannot be established.";
            if (errorCount > 0)
                return $"ModManager recorded {errorCount} initialization error(s).";
            if (!hasDll)
                return "No managed initializer is required for this PCK-only mod.";
            return initializerTypeCount > 0
                ? $"ModManager finished with zero errors after discovering {initializerTypeCount} initializer type(s)."
                : "ModManager finished its managed PatchAll fallback with zero errors.";
        }

        private static string ActivationDetail(
            bool initialized,
            bool isBaseLib,
            bool hasDll,
            bool hasPck,
            int patchTypeCount,
            int harmonyTargetCount,
            bool harmonyInspectionSucceeded
        )
        {
            if (!initialized)
                return "Not attempted because initialization did not complete.";
            if (!hasDll && hasPck)
                return "No concrete activation marker exists for this PCK-only mod.";
            if (!harmonyInspectionSucceeded)
                return "Harmony ownership inspection failed, so activation cannot be established.";
            if (!isBaseLib && patchTypeCount <= 0)
                return "The DLL exposes no Harmony patch type and no other production activation marker exists.";
            if (harmonyTargetCount <= 0)
                return $"The DLL exposes {patchTypeCount} Harmony patch type(s), but no installed target matched its owner.";
            if (isBaseLib)
                return $"Observed {harmonyTargetCount} exact-owner BaseLib Harmony target(s); broader PatchAll and custom-save compatibility remains partial.";
            return $"Observed {harmonyTargetCount} installed Harmony target(s) for {patchTypeCount} patch type(s).";
        }

        private static object FindRuntimeMod(
            LauncherResolvedMod selected,
            IEnumerable<object> runtimeMods
        )
        {
            return (runtimeMods ?? Array.Empty<object>()).FirstOrDefault(mod =>
                PathsMatch(RuntimeModPath(mod), selected.RootPath)
                && string.Equals(
                    TryReadManifestId(mod),
                    selected.ManifestId,
                    StringComparison.Ordinal
                )
            );
        }

        private static string RuntimeModPath(object mod)
            => TryReadStringMember(mod, "path") ?? TryReadStringMember(mod, "Path");

        private static bool TryCountModErrors(object mod, out int count)
        {
            count = 0;
            if (mod == null)
                return false;

            try
            {
                var type = mod.GetType();
                var field = type.GetField("errors", AllInstance)
                    ?? type.GetField("Errors", AllInstance);
                var property = field == null
                    ? type.GetProperty("errors", AllInstance)
                        ?? type.GetProperty("Errors", AllInstance)
                    : null;
                if (field == null && property == null)
                    return false;

                var errors = field != null ? field.GetValue(mod) : property?.GetValue(mod);
                count = errors switch
                {
                    null => 0,
                    ICollection collection => collection.Count,
                    IEnumerable enumerable => enumerable.Cast<object>().Count(),
                    _ => 1,
                };
                return true;
            }
            catch
            {
                count = 0;
                return false;
            }
        }

        private static int CountModInitializerTypes(Assembly assembly)
        {
            if (assembly == null)
                return 0;

            try
            {
                return assembly.GetTypes().Count(HasModInitializerAttribute);
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types
                    .Where(type => type != null)
                    .Count(HasModInitializerAttribute);
            }
            catch
            {
                return 0;
            }
        }

        private static bool HasModInitializerAttribute(Type type)
        {
            try
            {
                return type.GetCustomAttributesData().Any(attribute => string.Equals(
                    attribute.AttributeType.FullName,
                    "MegaCrit.Sts2.Core.Modding.ModInitializerAttribute",
                    StringComparison.Ordinal
                ));
            }
            catch
            {
                return false;
            }
        }

        private static bool IsNonEmptyPayload(string path)
        {
            try
            {
                return !string.IsNullOrWhiteSpace(path)
                    && File.Exists(path)
                    && new FileInfo(path).Length > 0;
            }
            catch
            {
                return false;
            }
        }

        private static IEnumerable<string> RuntimeHarmonyOwnerCandidates(string id)
        {
            if (!string.IsNullOrWhiteSpace(id))
                yield return id.Trim();
        }

        private static bool PathsMatch(string left, string right)
        {
            if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
                return false;

            return string.Equals(
                NormalizeRuntimePath(left),
                NormalizeRuntimePath(right),
                StringComparison.Ordinal
            );
        }

        private static string NormalizeRuntimePath(string path)
            => path.Replace('\\', '/').TrimEnd('/');
    }

    private const string BaseLibCompatibilityLimit =
        "Android-safe BaseLib initialization deliberately skips full PatchAll and custom-save extensions; concrete activation may be observed, but full Android compatibility is not claimed.";

    private static RuntimeModGateFact Gate(string gate, bool succeeded, string detail)
        => new()
        {
            Gate = gate,
            Succeeded = succeeded,
            Detail = detail ?? "",
        };
}
