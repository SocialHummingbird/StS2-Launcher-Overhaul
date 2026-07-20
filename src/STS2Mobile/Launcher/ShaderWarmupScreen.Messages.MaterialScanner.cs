using System;

namespace STS2Mobile.Launcher;

internal sealed partial class ShaderWarmupScreen
{
    private static partial class Message
    {
        internal static string FoundScenes(int sceneCount)
            => $"[ShaderWarmup] Found {sceneCount} scenes to scan";

        internal static string SceneScanStoppedByBudget(int scannedSceneCount, int totalSceneCount)
            => $"[ShaderWarmup] Stopped scene scan after {scannedSceneCount}/{totalSceneCount} scenes due to warmup time budget";

        internal static string UniqueShaders(int materialCount, int uniqueShaderCount)
            => $"[ShaderWarmup] {materialCount} total materials, {uniqueShaderCount} unique shaders";

        internal static string PropertyReadFailed(string propertyName, string scenePath, Exception ex)
            => $"[ShaderWarmup] Failed to read property {propertyName} in {scenePath}: {ex.Message}";

        internal static string SceneExtractFailed(string scenePath, string failure)
            => $"[ShaderWarmup] Failed to extract from {scenePath}: {failure}";

        internal static string FoundLooseMaterials(int materialCount)
            => $"[ShaderWarmup] Found {materialCount} materials from loose resource files";

        internal static string DirectoryEnumerationFailed(string dirPath, Exception ex)
            => $"[ShaderWarmup] Failed to enumerate {dirPath}: {ex.Message}";

        internal static string ResourceLoadFailed(string cleanPath, string failure)
            => $"[ShaderWarmup] Failed to load {cleanPath}: {failure}";

        internal static string ScanSummary(
            ShaderWarmupMaterialScanner.ShaderWarmupMaterialScanDiagnostics diagnostics
        )
            => "[ShaderWarmup] Scan summary: "
                + $"scenes={diagnostics.ScannedSceneCount}/{diagnostics.SceneCount}; "
                + $"loads={diagnostics.ThreadedLoadCompletedCount}/{diagnostics.ThreadedLoadRequestCount}; "
                + $"loadTimeouts={diagnostics.ThreadedLoadTimeoutCount}; "
                + $"materials={diagnostics.UniqueMaterialCount}/{diagnostics.MaterialsBeforeDedup} unique; "
                + $"deduplicated={diagnostics.DeduplicatedMaterialCount}/{diagnostics.MaterialsBeforeDedup}; "
                + $"dedupeBudgetStopped={diagnostics.DeduplicationStoppedByBudget}; "
                + $"budgetStopped={diagnostics.SceneScanStoppedByBudget}; "
                + $"deadlineReached={diagnostics.HardDeadlineReached}; "
                + "failures="
                + $"directories:{diagnostics.DirectoryEnumerationFailureCount}, "
                + $"resources:{diagnostics.ResourceLoadFailureCount}, "
                + $"scenes:{diagnostics.SceneExtractionFailureCount}, "
                + $"properties:{diagnostics.PropertyReadFailureCount}";
    }
}
