using System.Collections.Generic;
using Godot;

namespace STS2Mobile.Launcher;

internal sealed partial class ShaderWarmupScreen
{
    private static partial class ShaderWarmupMaterialScanner
    {
        private static List<string> CollectLooseMaterialPaths(
            string dirPath,
            LauncherMonotonicDeadline deadline,
            ShaderWarmupMaterialScanDiagnostics diagnostics
        )
        {
            var paths = new List<string>();
            var seen = new HashSet<string>();
            VisitFiles(dirPath, (currentDir, fileName) =>
                TryCollectMaterialPath(currentDir, fileName, paths, seen), deadline, diagnostics);
            return paths;
        }

        private static void TryCollectMaterialPath(
            string dirPath,
            string fileName,
            List<string> paths,
            HashSet<string> seen
        )
        {
            var cleanName = CleanResourceFileName(fileName);
            if (!IsSupportedMaterialFile(cleanName))
                return;

            var cleanPath = CleanResourcePath(dirPath, cleanName);
            if (seen.Add(cleanPath))
                paths.Add(cleanPath);
        }
    }
}
