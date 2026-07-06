using System;
using System.Collections.Generic;

namespace STS2Mobile.Launcher;

internal sealed partial class ShaderWarmupScreen
{
    private static partial class ShaderWarmupMaterialScanner
    {
        internal sealed class ShaderWarmupMaterialScanDiagnostics
        {
            private const int MaxLoggedFailuresPerCategory = 5;

            private int _directoryEnumerationFailuresLogged;
            private int _propertyReadFailuresLogged;
            private int _resourceLoadFailuresLogged;
            private int _sceneExtractionFailuresLogged;

            internal int DirectoryEnumerationFailureCount { get; private set; }
            internal int MaterialsBeforeDedup { get; set; }
            internal int PropertyReadFailureCount { get; private set; }
            internal int ResourceLoadFailureCount { get; private set; }
            internal int ScannedSceneCount { get; set; }
            internal bool SceneScanStoppedByBudget { get; set; }
            internal int SceneCount { get; set; }
            internal int SceneExtractionFailureCount { get; private set; }
            internal int UniqueMaterialCount { get; set; }

            internal bool HasFailures
                => DirectoryEnumerationFailureCount > 0
                    || PropertyReadFailureCount > 0
                    || ResourceLoadFailureCount > 0
                    || SceneExtractionFailureCount > 0;

            internal void LogSummary()
                => PatchHelper.Log(Message.ScanSummary(this));

            internal string[] ToEvidenceLines()
            {
                var lines = new List<string>
                {
                    $"Scan scenes: {ScannedSceneCount}/{SceneCount}",
                    $"Scan stopped by budget: {SceneScanStoppedByBudget}",
                    $"Scan materials before dedupe: {MaterialsBeforeDedup}",
                    $"Scan unique materials: {UniqueMaterialCount}",
                    $"Scan failures: directories={DirectoryEnumerationFailureCount}; resources={ResourceLoadFailureCount}; scenes={SceneExtractionFailureCount}; properties={PropertyReadFailureCount}",
                };

                lines.Add(HasFailures
                    ? "Scan classification: completed with scanner failures; see focused logcat for first failure samples"
                    : "Scan classification: completed without scanner failures");

                return lines.ToArray();
            }

            internal void RecordDirectoryEnumerationFailure(string dirPath, Exception ex)
            {
                DirectoryEnumerationFailureCount++;
                if (ShouldLogFailure(ref _directoryEnumerationFailuresLogged))
                    PatchHelper.Log(Message.DirectoryEnumerationFailed(dirPath, ex));
            }

            internal void RecordPropertyReadFailure(string propertyName, string scenePath, Exception ex)
            {
                PropertyReadFailureCount++;
                if (ShouldLogFailure(ref _propertyReadFailuresLogged))
                    PatchHelper.Log(Message.PropertyReadFailed(propertyName, scenePath, ex));
            }

            internal void RecordResourceLoadFailure(string cleanPath, Exception ex)
            {
                ResourceLoadFailureCount++;
                if (ShouldLogFailure(ref _resourceLoadFailuresLogged))
                    PatchHelper.Log(Message.ResourceLoadFailed(cleanPath, ex));
            }

            internal void RecordSceneExtractionFailure(string scenePath, Exception ex)
            {
                SceneExtractionFailureCount++;
                if (ShouldLogFailure(ref _sceneExtractionFailuresLogged))
                    PatchHelper.Log(Message.SceneExtractFailed(scenePath, ex));
            }

            private static bool ShouldLogFailure(ref int loggedCount)
            {
                loggedCount++;
                return loggedCount <= MaxLoggedFailuresPerCategory;
            }
        }

        internal readonly struct ShaderWarmupMaterialScanResult
        {
            internal ShaderWarmupMaterialScanResult(
                List<WarmupMaterial> materials,
                ShaderWarmupMaterialScanDiagnostics diagnostics
            )
            {
                Materials = materials;
                Diagnostics = diagnostics;
            }

            internal List<WarmupMaterial> Materials { get; }
            internal ShaderWarmupMaterialScanDiagnostics Diagnostics { get; }
        }
    }
}
