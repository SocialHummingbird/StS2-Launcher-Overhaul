#nullable enable

using System;
using System.IO;

namespace STS2Mobile.Launcher;

internal static partial class LauncherDiagnostics
{
    internal static string WriteSaveRecoveryBundle(
        string json,
        string fallbackDirectory
    )
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidDataException("Recovery export bundle is empty");
        var fileName =
            $"sts2-save-recovery-bundle-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.json";
        var targetPath = TryGetExternalDiagnosticsPath(fileName)
            ?? Path.Combine(fallbackDirectory, fileName);
        var parent = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrWhiteSpace(parent))
            Directory.CreateDirectory(parent);

        var stagingPath = $"{targetPath}.{Guid.NewGuid():N}.tmp";
        try
        {
            File.WriteAllText(stagingPath, json);
            File.Move(stagingPath, targetPath, overwrite: true);
            if (!File.Exists(targetPath)
                || !string.Equals(
                    File.ReadAllText(targetPath),
                    json,
                    StringComparison.Ordinal
                ))
            {
                throw new IOException(
                    "Recovery export bundle failed read-back verification"
                );
            }
            return targetPath;
        }
        finally
        {
            if (File.Exists(stagingPath))
                File.Delete(stagingPath);
        }
    }
}
