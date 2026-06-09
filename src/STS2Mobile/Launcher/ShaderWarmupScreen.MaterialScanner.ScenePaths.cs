using System.Collections.Generic;
using System;
using Godot;

namespace STS2Mobile.Launcher;

internal sealed partial class ShaderWarmupScreen
{
    private static partial class ShaderWarmupMaterialScanner
    {
        private static void CollectScenePaths(string dirPath, List<string> paths)
        {
            VisitFiles(dirPath, (currentDir, fileName) =>
            {
                var cleanName = CleanResourceFileName(fileName);
                if (!IsSceneFile(cleanName))
                    return;

                var cleanPath = CleanResourcePath(currentDir, cleanName);
                if (ShouldSkipScenePathForWarmup(cleanPath))
                    return;

                if (ResourceLoader.Exists(cleanPath))
                    paths.Add(cleanPath);
            });
        }

        private static bool ShouldSkipScenePathForWarmup(string scenePath)
            => scenePath.Contains("/daily_run/", StringComparison.OrdinalIgnoreCase);
    }
}
