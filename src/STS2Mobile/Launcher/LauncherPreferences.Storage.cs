using System;
using System.IO;
using Godot;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static partial class LauncherPreferences
{
    private const string TrueValue = "true";
    private const string FalseValue = "false";

    private static bool LoadBoolean(string fileName, bool defaultValue)
    {
        try
        {
            var path = PreferencePath(fileName);
            if (File.Exists(path))
                return File.ReadAllText(path).Trim() == TrueValue;
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] Preference load failed for {fileName}: {ex.Message}");
        }

        return defaultValue;
    }

    private static void SaveBoolean(string fileName, bool enabled)
    {
        try
        {
            var path = PreferencePath(fileName);
            EnsurePreferenceDirectory(path);
            File.WriteAllText(path, enabled ? TrueValue : FalseValue);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] Preference save failed for {fileName}: {ex.Message}");
        }
    }

    private static string PreferencePath(string fileName)
        => Path.Combine(OS.GetDataDir(), fileName);

    private static void EnsurePreferenceDirectory(string path)
    {
        var parent = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(parent))
            Directory.CreateDirectory(parent);
    }
}
