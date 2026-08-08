using System;
using System.IO;

namespace STS2Mobile.Launcher;

internal static partial class LauncherRuntimePatchValidationEvidence
{
    private static string RuntimeCacheValue(string dataDir, string prefix)
    {
        try
        {
            var path = LauncherRuntimeCacheEvidence.MarkerPath(dataDir);
            if (!File.Exists(path))
                return "<missing>";

            foreach (var line in File.ReadLines(path))
            {
                if (!line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    continue;

                var value = line[prefix.Length..].Trim();
                return string.IsNullOrWhiteSpace(value) ? "<empty>" : value;
            }

            return "<missing>";
        }
        catch (Exception ex)
        {
            return $"<unavailable:{ex.GetType().Name}>";
        }
    }

    private static string RuntimePackStatus(string runtimePackDirectory, string runtimePackGameAssembly)
    {
        if (string.IsNullOrWhiteSpace(runtimePackDirectory) || runtimePackDirectory == "<none>")
            return "not required";
        if (string.IsNullOrWhiteSpace(runtimePackGameAssembly) || runtimePackGameAssembly == "<none>")
            return "missing runtime pack assembly";
        return "runtime pack selected";
    }
}
