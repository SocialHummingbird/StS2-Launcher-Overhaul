using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace STS2Mobile.Patches;

// Extends ModManager to scan Android mod roots after the built-in game scan.
// Workshop sync stages into app-private storage; shared storage remains available
// for manual sideloading through /storage/emulated/0/StS2Launcher/Mods/.
internal static class ModLoaderPatches
{
    private static readonly BindingFlags AllStatic =
        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private static readonly BindingFlags AllInstance =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private const BindingFlags LoadedMarkerFlags =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

    internal static void Apply(Harmony harmony)
    {
        PatchHelper.Patch(
            harmony,
            typeof(ModManager),
            "Initialize",
            postfix: PatchHelper.Method(typeof(ModLoaderPatches), nameof(InitializePostfix))
        );
    }

    // Runs after the original Initialize() to pick up Android-only mod roots.
    private static void InitializePostfix()
    {
        try
        {
            if (!TryLoadModManagerAccess(out var access))
            {
                return;
            }

            var loadedRoots = LoadAndroidModRoots(access);
            if (loadedRoots == 0)
            {
                PatchHelper.Log("[Mods] No Android mod roots were available for scanning");
                return;
            }

            access.RebuildLoadedModsCacheIfAvailable();
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Mods] Failed to load Android mods: {ex}");
        }
    }

    private static bool TryLoadModManagerAccess(out ModManagerAccess access)
    {
        var modManagerType = typeof(ModManager);
        access = null;

        var scanMethod = FindScanMethod(modManagerType);
        if (scanMethod == null)
        {
            PatchHelper.Log("[Mods] Failed to locate a compatible ModManager mod scan method");
            return false;
        }

        var modsField = modManagerType.GetField("_mods", AllStatic);
        if (modsField == null)
        {
            PatchHelper.Log("[Mods] Failed to locate ModManager._mods");
            return false;
        }

        var initializedField = modManagerType.GetField("_initialized", AllStatic);
        var stateProperty = modManagerType.GetProperty("State", AllStatic);
        if (initializedField == null && stateProperty == null)
        {
            PatchHelper.Log("[Mods] Failed to locate a ModManager runtime load gate");
            return false;
        }

        var sourceType = scanMethod.GetParameters()[1].ParameterType;
        var sourceValue = ResolveModSource(sourceType);
        if (sourceValue == null)
        {
            PatchHelper.Log("[Mods] Failed to resolve ModSource.ModsDirectory");
            return false;
        }

        var tryLoadMod = FindTryLoadMod(modManagerType);
        var modType = ResolveModType(scanMethod, tryLoadMod, modsField);
        var newModsListType = modType == null ? null : typeof(List<>).MakeGenericType(modType);

        access = new ModManagerAccess(
            initializedField,
            stateProperty,
            scanMethod,
            tryLoadMod,
            modManagerType.GetMethod("SortModList", AllStatic),
            modsField,
            modManagerType.GetField("_loadedMods", AllStatic),
            modManagerType.GetField("_settings", AllStatic),
            sourceValue,
            newModsListType
        );
        return true;
    }

    private static MethodInfo FindScanMethod(Type modManagerType)
    {
        foreach (var methodName in new[] { "ReadModsInDirRecursive", "LoadModsInDirRecursive" })
        {
            foreach (var method in modManagerType.GetMethods(AllStatic))
            {
                if (!string.Equals(method.Name, methodName, StringComparison.Ordinal))
                    continue;

                var parameters = method.GetParameters();
                if (parameters.Length is < 2 or > 3)
                    continue;

                if (!parameters[1].ParameterType.IsEnum)
                    continue;

                if (parameters[0].ParameterType == typeof(string)
                    || typeof(DirAccess).IsAssignableFrom(parameters[0].ParameterType))
                {
                    return method;
                }
            }
        }

        return null;
    }

    private static MethodInfo FindTryLoadMod(Type modManagerType)
    {
        foreach (var method in modManagerType.GetMethods(AllStatic))
        {
            if (!string.Equals(method.Name, "TryLoadMod", StringComparison.Ordinal))
                continue;

            if (method.GetParameters().Length == 1)
                return method;
        }

        return null;
    }

    private static Type ResolveModType(
        MethodInfo scanMethod,
        MethodInfo tryLoadMod,
        FieldInfo modsField
    )
    {
        var tryLoadParameters = tryLoadMod?.GetParameters();
        if (tryLoadParameters?.Length == 1)
            return tryLoadParameters[0].ParameterType;

        var scanParameters = scanMethod.GetParameters();
        if (scanParameters.Length == 3)
        {
            var scanListElementType = ResolveEnumerableElementType(scanParameters[2].ParameterType);
            if (scanListElementType != null)
                return scanListElementType;
        }

        return ResolveEnumerableElementType(modsField.FieldType);
    }

    private static Type ResolveEnumerableElementType(Type type)
    {
        if (type == null)
            return null;

        if (type.IsArray)
            return type.GetElementType();

        if (type.IsGenericType && type.GetGenericArguments().Length == 1)
            return type.GetGenericArguments()[0];

        foreach (var interfaceType in type.GetInterfaces())
        {
            if (interfaceType.IsGenericType
                && interfaceType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return interfaceType.GetGenericArguments()[0];
            }
        }

        return null;
    }

    private static object ResolveModSource(Type sourceType)
    {
        try
        {
            return Enum.Parse(sourceType, "ModsDirectory");
        }
        catch
        {
            return null;
        }
    }

    private static int LoadAndroidModRoots(ModManagerAccess access)
    {
        AppPaths.EnsureWorkshopDirectories();

        var loadedRoots = 0;
        foreach (var root in AndroidModRoots())
        {
            using var dirAccess = DirAccess.Open(root.Path);
            if (dirAccess == null)
            {
                PatchHelper.Log(
                    $"[Mods] {root.Label} not found: {root.Path} (error: {DirAccess.GetOpenError()})"
                );
                continue;
            }

            PatchHelper.Log($"[Mods] Scanning {root.Label}: {root.Path}");
            if (access.LoadModsInRoot(root, dirAccess))
                loadedRoots++;
        }

        return loadedRoots;
    }

    private static IEnumerable<ModRoot> AndroidModRoots()
    {
        yield return new ModRoot(
            "Workshop staged mods",
            AppPaths.AppPrivateWorkshopStagedModsDir,
            requiresWorkshopConsent: true
        );
        yield return new ModRoot(
            "external sideloaded mods",
            AppPaths.ExternalModsDir,
            requiresWorkshopConsent: false
        );
    }

    private static object CreateLoadedModsList(IEnumerable allMods, Type targetListType)
    {
        try
        {
            if (!typeof(IEnumerable).IsAssignableFrom(targetListType))
                return null;

            if (Activator.CreateInstance(targetListType) is not IList targetList)
            {
                PatchHelper.Log("[Mods] _loadedMods field is not a concrete IList");
                return null;
            }

            foreach (var mod in allMods)
            {
                if (mod == null || !IsLoaded(mod))
                    continue;

                targetList.Add(mod);
            }

            return targetList;
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Mods] Rebuild of _loadedMods failed: {ex.Message}");
            return null;
        }
    }

    private static bool IsLoaded(object mod)
    {
        foreach (var member in LoadedMarkerCandidates(mod.GetType()))
        {
            if (member is null)
                continue;

            object value = member switch
            {
                FieldInfo field => field.GetValue(mod),
                PropertyInfo prop => prop.GetValue(mod),
                _ => null,
            };

            if (value is bool loaded)
                return loaded;

            if (value != null && value.GetType().IsEnum)
                return string.Equals(value.ToString(), "Loaded", StringComparison.Ordinal);
        }

        // Without a reliable marker, assume loaded to avoid dropping mods.
        return true;
    }

    private static IEnumerable<MemberInfo> LoadedMarkerCandidates(Type modType)
    {
        yield return modType.GetField("wasLoaded", LoadedMarkerFlags);
        yield return modType.GetField("WasLoaded", LoadedMarkerFlags);
        yield return modType.GetField("isLoaded", LoadedMarkerFlags);
        yield return modType.GetField("IsLoaded", LoadedMarkerFlags);
        yield return modType.GetField("state", LoadedMarkerFlags);
        yield return modType.GetField("State", LoadedMarkerFlags);
        yield return modType.GetProperty("wasLoaded", LoadedMarkerFlags);
        yield return modType.GetProperty("WasLoaded", LoadedMarkerFlags);
        yield return modType.GetProperty("isLoaded", LoadedMarkerFlags);
        yield return modType.GetProperty("IsLoaded", LoadedMarkerFlags);
        yield return modType.GetProperty("state", LoadedMarkerFlags);
        yield return modType.GetProperty("State", LoadedMarkerFlags);
    }

    private sealed class ModManagerAccess
    {
        private readonly FieldInfo _initializedField;
        private readonly PropertyInfo _stateProperty;
        private readonly MethodInfo _scanMethod;
        private readonly MethodInfo _tryLoadMod;
        private readonly MethodInfo _sortModList;
        private readonly FieldInfo _modsField;
        private readonly FieldInfo _loadedModsField;
        private readonly FieldInfo _settingsField;
        private readonly object _sourceValue;
        private readonly Type _newModsListType;

        internal ModManagerAccess(
            FieldInfo initializedField,
            PropertyInfo stateProperty,
            MethodInfo scanMethod,
            MethodInfo tryLoadMod,
            MethodInfo sortModList,
            FieldInfo modsField,
            FieldInfo loadedModsField,
            FieldInfo settingsField,
            object sourceValue,
            Type newModsListType
        )
        {
            _initializedField = initializedField;
            _stateProperty = stateProperty;
            _scanMethod = scanMethod;
            _tryLoadMod = tryLoadMod;
            _sortModList = sortModList;
            _modsField = modsField;
            _loadedModsField = loadedModsField;
            _settingsField = settingsField;
            _sourceValue = sourceValue;
            _newModsListType = newModsListType;
        }

        internal bool LoadModsInRoot(ModRoot root, DirAccess dirAccess)
        {
            var newMods = CreateNewModsList();
            var scanArguments = BuildScanArguments(root.Path, dirAccess, newMods);
            if (scanArguments == null)
            {
                PatchHelper.Log($"[Mods] Unsupported scan signature for {root.Label}");
                return false;
            }

            using var loadWindow = BeginRuntimeLoadWindow();
            try
            {
                ApplyWorkshopConsentIfAvailable(root);
                _scanMethod.Invoke(null, scanArguments);
                var discovered = Count(newMods);

                SortModsIfAvailable();
                var attempted = TryLoadNewMods(newMods);
                var loadedTotal = CountLoadedMods();

                PatchHelper.Log(
                    $"[Mods] {root.Label} scan complete. Discovered: {discovered}, load attempts: {attempted}, total loaded: {loadedTotal}"
                );
                return true;
            }
            catch (TargetInvocationException ex)
            {
                PatchHelper.Log($"[Mods] {root.Label} scan failed: {ex.InnerException ?? ex}");
                return false;
            }
            catch (Exception ex)
            {
                PatchHelper.Log($"[Mods] {root.Label} scan failed: {ex}");
                return false;
            }
        }

        internal void RebuildLoadedModsCacheIfAvailable()
        {
            if (_loadedModsField == null)
            {
                PatchHelper.Log($"[Mods] External scan complete. Total loaded: {CountLoadedMods()}");
                return;
            }

            var allMods = _modsField.GetValue(null) as IEnumerable;
            if (allMods == null)
            {
                PatchHelper.Log("[Mods] ModManager._mods was null; skipping loaded-mod rebuild");
                return;
            }

            var loadedMods = CreateLoadedModsList(allMods, _loadedModsField.FieldType);
            if (loadedMods == null)
            {
                PatchHelper.Log("[Mods] Failed to rebuild _loadedMods via reflection");
                return;
            }

            _loadedModsField.SetValue(null, loadedMods);
            PatchHelper.Log(
                $"[Mods] External scan complete. Total loaded: {(loadedMods as ICollection)?.Count ?? 0}"
            );
        }

        private object CreateNewModsList()
        {
            if (_newModsListType == null)
                return null;

            try
            {
                return Activator.CreateInstance(_newModsListType);
            }
            catch (Exception ex)
            {
                PatchHelper.Log($"[Mods] Failed to create new-mod capture list: {ex.Message}");
                return null;
            }
        }

        private object[] BuildScanArguments(string path, DirAccess dirAccess, object newMods)
        {
            var parameters = _scanMethod.GetParameters();
            if (parameters.Length == 2)
            {
                if (parameters[0].ParameterType == typeof(string))
                    return new[] { path, _sourceValue };

                if (typeof(DirAccess).IsAssignableFrom(parameters[0].ParameterType))
                    return new object[] { dirAccess, _sourceValue };
            }

            if (parameters.Length == 3 && parameters[0].ParameterType == typeof(string))
                return new[] { path, _sourceValue, newMods };

            return null;
        }

        private RuntimeLoadWindow BeginRuntimeLoadWindow()
        {
            return new RuntimeLoadWindow(_initializedField, _stateProperty);
        }

        private void ApplyWorkshopConsentIfAvailable(ModRoot root)
        {
            if (!root.RequiresWorkshopConsent)
                return;

            if (!WorkshopModConsent.IsAccepted())
            {
                PatchHelper.Log(
                    "[Mods] Workshop staged mods require the launcher Workshop mod consent marker; game mod warning remains active"
                );
                return;
            }

            if (TrySetPlayerAgreedToModLoading())
                PatchHelper.Log("[Mods] Workshop mod consent marker applied to game mod settings");
            else
                PatchHelper.Log("[Mods] Workshop mod consent marker present but game mod settings could not be updated");
        }

        private bool TrySetPlayerAgreedToModLoading()
        {
            if (_settingsField == null)
                return false;

            try
            {
                var settings = _settingsField.GetValue(null);
                if (settings == null)
                {
                    settings = Activator.CreateInstance(_settingsField.FieldType);
                    _settingsField.SetValue(null, settings);
                }

                var property = settings.GetType().GetProperty(
                    "PlayerAgreedToModLoading",
                    AllInstance
                );
                if (property == null || !property.CanWrite)
                    return false;

                property.SetValue(settings, true);
                return true;
            }
            catch (Exception ex)
            {
                PatchHelper.Log($"[Mods] Failed to apply Workshop mod consent: {ex.Message}");
                return false;
            }
        }

        private void SortModsIfAvailable()
        {
            if (_sortModList == null)
                return;

            try
            {
                var parameters = _sortModList.GetParameters();
                if (parameters.Length != 1)
                    return;

                var modList = ResolveSettingsModList(parameters[0].ParameterType);
                if (modList == null)
                    return;

                _sortModList.Invoke(null, new[] { modList });
            }
            catch (Exception ex)
            {
                PatchHelper.Log($"[Mods] Mod sort skipped: {ex.Message}");
            }
        }

        private object ResolveSettingsModList(Type parameterType)
        {
            var settings = _settingsField?.GetValue(null);
            if (settings != null)
            {
                var modList = settings.GetType()
                    .GetProperty("ModList", AllInstance)
                    ?.GetValue(settings);
                if (modList != null && parameterType.IsInstanceOfType(modList))
                    return modList;
            }

            try
            {
                return Activator.CreateInstance(parameterType);
            }
            catch
            {
                return null;
            }
        }

        private int TryLoadNewMods(object newMods)
        {
            if (_tryLoadMod == null || newMods is not IEnumerable mods)
                return 0;

            var newModSet = new HashSet<object>();
            foreach (var mod in mods)
            {
                if (mod != null)
                    newModSet.Add(mod);
            }

            var attempts = 0;
            foreach (var mod in OrderedNewMods(newModSet))
            {
                if (mod == null)
                    continue;

                try
                {
                    _tryLoadMod.Invoke(null, new[] { mod });
                    attempts++;
                }
                catch (TargetInvocationException ex)
                {
                    PatchHelper.Log($"[Mods] Mod load failed: {ex.InnerException ?? ex}");
                }
                catch (Exception ex)
                {
                    PatchHelper.Log($"[Mods] Mod load failed: {ex}");
                }
            }

            return attempts;
        }

        private IEnumerable<object> OrderedNewMods(ISet<object> newMods)
        {
            if (newMods == null || newMods.Count == 0)
                yield break;

            if (_modsField.GetValue(null) is not IEnumerable allMods)
                yield break;

            foreach (var mod in allMods)
            {
                if (mod != null && newMods.Contains(mod))
                    yield return mod;
            }
        }

        private int CountLoadedMods()
        {
            var allMods = _modsField.GetValue(null) as IEnumerable;
            if (allMods == null)
                return 0;

            var count = 0;
            foreach (var mod in allMods)
            {
                if (mod != null && IsLoaded(mod))
                    count++;
            }

            return count;
        }

        private static int Count(object list)
        {
            return list switch
            {
                ICollection collection => collection.Count,
                IEnumerable enumerable => CountEnumerable(enumerable),
                _ => 0,
            };
        }

        private static int CountEnumerable(IEnumerable enumerable)
        {
            var count = 0;
            foreach (var _ in enumerable)
                count++;
            return count;
        }
    }

    private sealed class RuntimeLoadWindow : IDisposable
    {
        private readonly FieldInfo _initializedField;
        private readonly PropertyInfo _stateProperty;
        private readonly object _previousInitialized;
        private readonly object _previousState;
        private readonly bool _changedInitialized;
        private readonly bool _changedState;

        internal RuntimeLoadWindow(FieldInfo initializedField, PropertyInfo stateProperty)
        {
            _initializedField = initializedField;
            _stateProperty = stateProperty;

            if (_initializedField != null)
            {
                _previousInitialized = _initializedField.GetValue(null);
                _initializedField.SetValue(null, false);
                _changedInitialized = true;
            }

            if (_stateProperty != null && TryResolveStateValue(_stateProperty.PropertyType, out var noneState))
            {
                _previousState = _stateProperty.GetValue(null);
                SetState(_stateProperty, noneState);
                _changedState = true;
            }
        }

        public void Dispose()
        {
            if (_changedState)
                SetState(_stateProperty, _previousState);

            if (_changedInitialized)
                _initializedField.SetValue(null, _previousInitialized);
        }

        private static bool TryResolveStateValue(Type stateType, out object noneState)
        {
            try
            {
                noneState = Enum.Parse(stateType, "None");
                return true;
            }
            catch
            {
                noneState = null;
                return false;
            }
        }

        private static void SetState(PropertyInfo stateProperty, object value)
        {
            var setter = stateProperty.GetSetMethod(true);
            if (setter != null)
            {
                setter.Invoke(null, new[] { value });
                return;
            }

            stateProperty.SetValue(null, value);
        }
    }

    private readonly struct ModRoot
    {
        internal ModRoot(string label, string path, bool requiresWorkshopConsent)
        {
            Label = label;
            Path = path;
            RequiresWorkshopConsent = requiresWorkshopConsent;
        }

        internal string Label { get; }
        internal string Path { get; }
        internal bool RequiresWorkshopConsent { get; }
    }
}
