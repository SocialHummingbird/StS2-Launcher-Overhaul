using System;
using System.IO;
using Godot;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static partial class LauncherPreferences
{
    private const string TrueValue = "true";
    private const string FalseValue = "false";

    private readonly struct PreferenceFile
    {
        internal PreferenceFile(string fileName)
        {
            FileName = fileName;
        }

        private string FileName { get; }
        private string Path => PreferencePath(FileName);

        internal bool ReadBoolean(bool defaultValue)
        {
            try
            {
                if (File.Exists(Path))
                    return File.ReadAllText(Path).Trim() == TrueValue;
            }
            catch (Exception ex)
            {
                PatchHelper.Log(
                    $"[Launcher] Preference load failed for {FileName}: {ex.Message}"
                );
            }

            return defaultValue;
        }

        internal void WriteBoolean(bool enabled)
        {
            try
            {
                EnsurePreferenceDirectory(Path);
                File.WriteAllText(Path, enabled ? TrueValue : FalseValue);
            }
            catch (Exception ex)
            {
                PatchHelper.Log(
                    $"[Launcher] Preference save failed for {FileName}: {ex.Message}"
                );
            }
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
