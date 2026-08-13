using System;
using System.IO;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static partial class LauncherPreferences
{
    private readonly struct PreferenceFile
    {
        internal PreferenceFile(string fileName)
        {
            FileName = fileName;
        }

        private string FileName { get; }
        private string Path => PreferencePath(FileName);

        internal string ReadText(string defaultValue)
        {
            try
            {
                if (File.Exists(Path))
                    return File.ReadAllText(Path).Trim();
            }
            catch (Exception ex)
            {
                PatchHelper.Log(
                    $"[Launcher] Preference load failed for {FileName}: {ex.Message}"
                );
            }

            return defaultValue;
        }

        internal void WriteText(string value)
        {
            try
            {
                EnsurePreferenceDirectory(Path);
                File.WriteAllText(Path, value ?? string.Empty);
            }
            catch (Exception ex)
            {
                PatchHelper.Log(
                    $"[Launcher] Preference save failed for {FileName}: {ex.Message}"
                );
            }
        }

        internal bool Exists()
            => File.Exists(Path);
    }

    private static string PreferencePath(string fileName)
        => Path.Combine(AppPaths.AppPrivateDataDir, fileName);

    private static void EnsurePreferenceDirectory(string path)
    {
        var parent = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(parent))
            Directory.CreateDirectory(parent);
    }
}
