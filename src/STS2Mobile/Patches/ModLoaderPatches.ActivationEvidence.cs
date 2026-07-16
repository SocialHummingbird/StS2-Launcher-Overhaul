using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using STS2Mobile.Launcher;

namespace STS2Mobile.Patches;

internal static partial class ModLoaderPatches
{
    private sealed class RuntimeModActivationSummary
    {
        public string Id { get; init; } = "";
        public string Title { get; init; } = "";
        public string Path { get; init; } = "";
        public string State { get; init; } = "";
        public string Assembly { get; init; } = "";
        public int ErrorCount { get; init; }
        public bool ManifestHasDll { get; init; }
        public bool ManifestHasPck { get; init; }
        public bool PayloadReady { get; init; }
        public bool RuntimeLoadSucceeded { get; init; }
        public int HarmonyPatchTypeCount { get; init; }
        public int HarmonyTargetCount { get; init; }
        public string CompatibilityMode { get; init; } = "";
        public string ActivationStatus { get; init; } = "";
        public string CompatibilityLimit { get; init; } = "";
        public bool InGameEffectVerified { get; init; }
    }

    private sealed partial class ModManagerAccess
    {
        internal RuntimeModActivationSummary[] BuildActivationEvidence(
            IReadOnlyList<LauncherKnownMod> knownMods
        )
        {
            var selectedMods = (knownMods ?? Array.Empty<LauncherKnownMod>())
                .Where(mod => mod.Enabled && !mod.IsUnsupported)
                .ToArray();
            var runtimeMods = (_modsField.GetValue(null) as IEnumerable)?
                .Cast<object>()
                .Where(mod => mod != null)
                .ToArray()
                ?? Array.Empty<object>();

            var evidence = selectedMods
                .Select(selected => BuildActivationSummary(selected, FindRuntimeMod(selected, runtimeMods)))
                .ToArray();

            PatchHelper.Log(
                $"[Mods] Activation evidence: selected={evidence.Length} "
                    + $"payloadReady={evidence.Count(mod => mod.PayloadReady)} "
                    + $"runtimePatched={evidence.Count(mod => mod.HarmonyTargetCount > 0)} "
                    + $"partial={evidence.Count(mod => mod.CompatibilityMode == "partial-android")} "
                    + $"substitutes={evidence.Count(mod => mod.CompatibilityMode == "launcher-substitute")} "
                    + $"failed={evidence.Count(mod => !mod.RuntimeLoadSucceeded)} "
                    + "inGameVerified=0"
            );
            return evidence;
        }

        private static RuntimeModActivationSummary BuildActivationSummary(
            LauncherKnownMod selected,
            object runtimeMod
        )
        {
            var id = runtimeMod == null ? selected.Id : TryReadManifestId(runtimeMod) ?? selected.Id;
            var isBaseLib = string.Equals(id, "BaseLib", StringComparison.OrdinalIgnoreCase);
            var state = ReadModState(runtimeMod);
            var errorCount = CountModErrors(runtimeMod);
            var assembly = runtimeMod == null
                ? null
                : TryReadAssemblyMember(runtimeMod)
                    ?? FindLoadedAssemblyBySimpleName(id)
                    ?? FindLoadedAssemblyBySimpleName(SanitizeAssemblyName(selected.Title));
            var manifest = runtimeMod == null
                ? null
                : TryReadMemberValue(runtimeMod, "manifest")
                    ?? TryReadMemberValue(runtimeMod, "Manifest");
            var hasDll = ReadBooleanMember(manifest, "hasDll", "HasDll");
            var hasPck = ReadBooleanMember(manifest, "hasPck", "HasPck") || selected.HasPck;
            var loadedState = runtimeMod != null && IsLoaded(runtimeMod);
            var payloadReady = loadedState
                && errorCount == 0
                && (assembly != null || hasPck);

            var patchTypeCount = CountHarmonyPatchTypes(assembly);
            var ownerCandidates = RuntimeHarmonyOwnerCandidates(id, selected.Title, assembly).ToArray();
            FindHarmonyTargetsForOwners(ownerCandidates, out var harmonyTargetCount);

            var compatibilityMode = isBaseLib ? "partial-android" : "native";
            var runtimeLoadSucceeded = errorCount == 0 && payloadReady;
            var activationStatus = ActivationStatusFor(
                runtimeMod,
                loadedState,
                errorCount,
                payloadReady,
                harmonyTargetCount,
                isBaseLib
            );
            var compatibilityLimit = isBaseLib
                ? "Android-safe BaseLib initialization skips the full upstream PatchAll surface."
                : harmonyTargetCount > 0
                    ? "Runtime patches are installed, but no in-game action was exercised by this marker."
                    : "Payload load is recorded, but no in-game effect was exercised by this marker.";

            return new RuntimeModActivationSummary
            {
                Id = id ?? "",
                Title = selected.Title ?? "",
                Path = selected.Path ?? "",
                State = state,
                Assembly = assembly?.GetName().Name ?? "",
                ErrorCount = errorCount,
                ManifestHasDll = hasDll,
                ManifestHasPck = hasPck,
                PayloadReady = payloadReady,
                RuntimeLoadSucceeded = runtimeLoadSucceeded,
                HarmonyPatchTypeCount = patchTypeCount,
                HarmonyTargetCount = harmonyTargetCount,
                CompatibilityMode = compatibilityMode,
                ActivationStatus = activationStatus,
                CompatibilityLimit = compatibilityLimit,
                InGameEffectVerified = false,
            };
        }

        private static object FindRuntimeMod(LauncherKnownMod selected, IEnumerable<object> runtimeMods)
        {
            var candidates = runtimeMods as object[] ?? runtimeMods.ToArray();
            var byPath = candidates.FirstOrDefault(mod => PathsMatch(RuntimeModPath(mod), selected.Path));
            if (byPath != null)
                return byPath;

            var byId = candidates.FirstOrDefault(mod =>
                string.Equals(TryReadManifestId(mod), selected.Id, StringComparison.OrdinalIgnoreCase));
            if (byId != null)
                return byId;

            return candidates.FirstOrDefault(mod => RuntimePathIsWithinSelectedRoot(
                RuntimeModPath(mod),
                selected.Path
            ));
        }

        private static string RuntimeModPath(object mod)
            => TryReadStringMember(mod, "path") ?? TryReadStringMember(mod, "Path");

        private static string ReadModState(object mod)
        {
            if (mod == null)
                return "not-discovered";

            var value = TryReadMemberValue(mod, "state")
                ?? TryReadMemberValue(mod, "State");
            return value?.ToString() ?? (IsLoaded(mod) ? "Loaded" : "unknown");
        }

        private static int CountModErrors(object mod)
        {
            if (mod == null)
                return 0;

            var errors = TryReadMemberValue(mod, "errors")
                ?? TryReadMemberValue(mod, "Errors");
            if (errors == null)
                return 0;
            if (errors is ICollection collection)
                return collection.Count;
            if (errors is IEnumerable enumerable)
                return enumerable.Cast<object>().Count();
            return 1;
        }

        private static bool ReadBooleanMember(object target, params string[] names)
        {
            foreach (var name in names)
            {
                if (TryReadMemberValue(target, name) is bool value)
                    return value;
            }
            return false;
        }

        private static IEnumerable<string> RuntimeHarmonyOwnerCandidates(
            string id,
            string title,
            Assembly assembly
        )
        {
            var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Add(id);
            Add(title);
            Add(SanitizeAssemblyName(title));
            Add(assembly?.GetName().Name);
            return candidates;

            void Add(string value)
            {
                if (!string.IsNullOrWhiteSpace(value))
                    candidates.Add(value.Trim());
            }
        }

        private static string ActivationStatusFor(
            object runtimeMod,
            bool loadedState,
            int errorCount,
            bool payloadReady,
            int harmonyTargetCount,
            bool isBaseLib
        )
        {
            if (runtimeMod == null)
                return "not-discovered";
            if (errorCount > 0)
                return "loaded-with-errors";
            if (!loadedState)
                return "failed";
            if (!payloadReady)
                return "loaded-without-payload-evidence";
            if (isBaseLib)
                return "partial-android-compatibility";
            if (harmonyTargetCount > 0)
                return "runtime-patches-installed";
            return "payload-loaded-unverified";
        }

        private static bool PathsMatch(string left, string right)
        {
            if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
                return false;

            return string.Equals(
                NormalizeRuntimePath(left),
                NormalizeRuntimePath(right),
                StringComparison.OrdinalIgnoreCase
            );
        }

        private static bool RuntimePathIsWithinSelectedRoot(string runtimePath, string selectedRoot)
        {
            if (string.IsNullOrWhiteSpace(runtimePath) || string.IsNullOrWhiteSpace(selectedRoot))
                return false;

            var normalizedRuntimePath = NormalizeRuntimePath(runtimePath);
            var normalizedSelectedRoot = NormalizeRuntimePath(selectedRoot);
            return normalizedRuntimePath.StartsWith(
                normalizedSelectedRoot + "/",
                StringComparison.OrdinalIgnoreCase
            );
        }

        private static string NormalizeRuntimePath(string path)
            => path.Replace('\\', '/').TrimEnd('/');
    }
}
