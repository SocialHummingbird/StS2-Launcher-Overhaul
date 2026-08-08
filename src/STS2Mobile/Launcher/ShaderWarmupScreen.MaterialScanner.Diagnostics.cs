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
            internal int DeduplicatedMaterialCount { get; set; }
            internal bool DeduplicationStoppedByBudget { get; private set; }
            internal bool HardDeadlineReached { get; private set; }
            internal int LoadedLooseResourceCount { get; set; }
            internal int LooseResourceCount { get; set; }
            internal int MaterialsBeforeDedup { get; set; }
            internal int PropertyReadFailureCount { get; private set; }
            internal int ResourceLoadFailureCount { get; private set; }
            internal int ScannedSceneCount { get; set; }
            internal bool SceneScanStoppedByBudget { get; set; }
            internal int SceneCount { get; set; }
            internal int SceneExtractionFailureCount { get; private set; }
            internal int ThreadedLoadCompletedCount { get; private set; }
            internal int ThreadedLoadRequestCount { get; private set; }
            internal int ThreadedLoadTimeoutCount { get; private set; }
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
                    $"Scan hard deadline reached: {HardDeadlineReached}",
                    $"Scan loose resources: {LoadedLooseResourceCount}/{LooseResourceCount}",
                    $"Scan threaded loads: completed={ThreadedLoadCompletedCount}; requested={ThreadedLoadRequestCount}; timedOut={ThreadedLoadTimeoutCount}",
                    $"Scan materials before dedupe: {MaterialsBeforeDedup}",
                    $"Scan materials deduplicated: {DeduplicatedMaterialCount}/{MaterialsBeforeDedup}",
                    $"Scan deduplication stopped by budget: {DeduplicationStoppedByBudget}",
                    $"Scan unique materials: {UniqueMaterialCount}",
                    $"Scan failures: directories={DirectoryEnumerationFailureCount}; resources={ResourceLoadFailureCount}; scenes={SceneExtractionFailureCount}; properties={PropertyReadFailureCount}",
                };

                lines.Add(HardDeadlineReached
                    ? "Scan classification: stopped cooperatively at the hard deadline"
                    : HasFailures
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

            internal void RecordResourceLoadFailure(string cleanPath, string failure)
            {
                ResourceLoadFailureCount++;
                if (ShouldLogFailure(ref _resourceLoadFailuresLogged))
                    PatchHelper.Log(Message.ResourceLoadFailed(cleanPath, failure));
            }

            internal void RecordSceneExtractionFailure(string scenePath, string failure)
            {
                SceneExtractionFailureCount++;
                if (ShouldLogFailure(ref _sceneExtractionFailuresLogged))
                    PatchHelper.Log(Message.SceneExtractFailed(scenePath, failure));
            }

            internal void MarkDeadlineReached()
                => HardDeadlineReached = true;

            internal void MarkDeduplicationStoppedByBudget(int processed)
            {
                DeduplicatedMaterialCount = processed;
                DeduplicationStoppedByBudget = true;
                MarkDeadlineReached();
            }

            internal void RecordThreadedLoadCompleted()
                => ThreadedLoadCompletedCount++;

            internal void RecordThreadedLoadRequested()
                => ThreadedLoadRequestCount++;

            internal void RecordThreadedLoadTimedOut()
                => ThreadedLoadTimeoutCount++;

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
