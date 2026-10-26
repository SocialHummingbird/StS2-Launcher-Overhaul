using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using MegaCrit.Sts2.Core.Localization;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static class LauncherModLaunchResultStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };
    // This assembly embeds the pinned Harmony closure manifest. Its deterministic
    // MVID therefore changes with launcher code or any shared patching dependency.
    internal static string FrameworkIdentity { get; } = typeof(LauncherModLaunchResultStore).Assembly.ManifestModule.ModuleVersionId.ToString("N")
        + ":" + typeof(object).Assembly.ManifestModule.ModuleVersionId.ToString("N");

    internal static string ConfigurationId(LauncherModLaunchPlan plan, string gameIdentityId, string runtimePackId)
        => Convert.ToHexString(AndroidJavaCrypto.Sha256HashData(Encoding.UTF8.GetBytes(gameIdentityId + "\n" + runtimePackId + "\n" + FrameworkIdentity + "\n" + plan.Fingerprint))).ToLowerInvariant();

    internal static LauncherModLaunchResultDocument Read(string markerPath)
    {
        try
        {
            if (!File.Exists(markerPath)) return null;
            var document = JsonSerializer.Deserialize<LauncherModLaunchResultDocument>(File.ReadAllText(markerPath), JsonOptions);
            return document;
        }
        catch (Exception ex) { PatchHelper.Log($"[Mods] Last framework result is unavailable: {ex.Message}"); return null; }
    }

    internal static bool IsValid(LauncherModLaunchResultDocument document)
        => document?.Version == 2 && !string.IsNullOrWhiteSpace(document.AttemptId)
            && !string.IsNullOrWhiteSpace(document.GameIdentityId) && !string.IsNullOrWhiteSpace(document.RuntimePackId)
            && !string.IsNullOrWhiteSpace(document.ConfigurationId) && !string.IsNullOrWhiteSpace(document.SelectionFingerprint)
            && !string.IsNullOrWhiteSpace(document.FrameworkIdentity)
            && DateTimeOffset.TryParse(document.TimestampUtc, out _)
            && document.LaunchMode is LauncherModSelectionState.VanillaModeName or LauncherModSelectionState.ModdedModeName
            && document.Sources != null && document.Mods != null
            && document.Sources.All(source => source != null && !string.IsNullOrWhiteSpace(source.SelectionKey) && !string.IsNullOrWhiteSpace(source.InstalledGeneration))
            && document.Sources.Select(source => source.SelectionKey).Distinct(StringComparer.OrdinalIgnoreCase).Count() == document.Sources.Length
            && document.Mods.All(mod => mod != null && !string.IsNullOrWhiteSpace(mod.SelectionKey)
                && !string.IsNullOrWhiteSpace(mod.RelativeManifestPath) && !string.IsNullOrWhiteSpace(mod.NativeState)
                && mod.NativeErrors != null && mod.NativeErrors.All(error => error != null)
                && document.Sources.Any(source => source.SelectionKey == mod.SelectionKey && source.InstalledGeneration == mod.PackageGeneration))
            && document.Mods.Select(mod => (mod.SelectionKey, mod.RelativeManifestPath)).Distinct().Count() == document.Mods.Length
            && (document.LaunchMode != LauncherModSelectionState.VanillaModeName || (document.Sources.Length == 0 && document.Mods.Length == 0));

    internal static void WriteCompletion(LauncherModLaunchPlan plan, string attemptId, string gameIdentityId,
        string runtimePackId, string selectionFingerprint, LauncherModLaunchResultDocumentItem[] mods,
        string initializationError = null, string markerPath = null, bool? nativeIsRunningModded = null)
    {
        var document = new LauncherModLaunchResultDocument
        {
            Version = 2,
            AttemptId = attemptId,
            TimestampUtc = DateTime.UtcNow.ToString("O"),
            GameIdentityId = gameIdentityId,
            RuntimePackId = runtimePackId,
            FrameworkIdentity = FrameworkIdentity,
            ConfigurationId = ConfigurationId(plan, gameIdentityId, runtimePackId),
            SelectionFingerprint = selectionFingerprint,
            LaunchMode = plan.Mode == LauncherModPlayMode.Modded ? LauncherModSelectionState.ModdedModeName : LauncherModSelectionState.VanillaModeName,
            Sources = plan.Sources.Select(source => new LauncherModLaunchResultSource
            {
                SelectionKey = source.SelectionKey,
                InstalledGeneration = source.InstalledGeneration,
                ManifestPath = source.ManifestPath,
            }).ToArray(),
            Mods = mods,
            InitializationError = initializationError,
            NativeIsRunningModded = nativeIsRunningModded,
        };
        if (!IsValid(document)) throw new InvalidDataException("Framework completion has incomplete source/launch identity.");
        try
        {
            markerPath ??= AppPaths.AppPrivateLastModLaunchPath;
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(markerPath)));
            var temporary = markerPath + ".tmp";
            File.WriteAllText(temporary, JsonSerializer.Serialize(document, JsonOptions));
            File.Move(temporary, markerPath, overwrite: true);
        }
        catch (Exception ex) { PatchHelper.Log($"[Mods] Could not persist framework completion: {ex}"); }
    }
}

internal sealed class LauncherModLaunchResultDocument
{
    public int Version { get; set; }
    public string AttemptId { get; set; } = "";
    public string TimestampUtc { get; set; } = "";
    public string GameIdentityId { get; set; } = "";
    public string RuntimePackId { get; set; } = "";
    public string FrameworkIdentity { get; set; } = "";
    public string ConfigurationId { get; set; } = "";
    public string SelectionFingerprint { get; set; } = "";
    public string LaunchMode { get; set; } = "";
    public string InitializationError { get; set; }
    public bool? NativeIsRunningModded { get; set; }
    public LauncherModLaunchResultSource[] Sources { get; set; } = Array.Empty<LauncherModLaunchResultSource>();
    public LauncherModLaunchResultDocumentItem[] Mods { get; set; } = Array.Empty<LauncherModLaunchResultDocumentItem>();
}

internal sealed class LauncherModLaunchResultSource
{
    public string SelectionKey { get; set; } = "";
    public string InstalledGeneration { get; set; } = "";
    public string ManifestPath { get; set; }
}

internal sealed class LauncherModLaunchResultDocumentItem
{
    public string SelectionKey { get; set; } = "";
    public string PackageGeneration { get; set; } = "";
    public string RelativeManifestPath { get; set; } = "";
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string NativeState { get; set; } = "";
    public LauncherModNativeError[] NativeErrors { get; set; } = Array.Empty<LauncherModNativeError>();
}

internal sealed class LauncherModNativeError
{
    public string LocTable { get; set; } = "";
    public string LocEntryKey { get; set; } = "";
    public Dictionary<string, object> Variables { get; set; } = new();
    internal static LauncherModNativeError From(LocString value) => new()
    {
        LocTable = value.LocTable,
        LocEntryKey = value.LocEntryKey,
        Variables = value.Variables.ToDictionary(pair => pair.Key, pair => DiagnosticValue(pair.Value)),
    };
    private static object DiagnosticValue(object value) => value switch
    {
        null or string or bool or byte or short or int or long or ushort or uint or ulong or float or double or decimal => value,
        IEnumerable items => items.Cast<object>().Select(DiagnosticValue).ToArray(),
        _ => value.ToString(),
    };
}
