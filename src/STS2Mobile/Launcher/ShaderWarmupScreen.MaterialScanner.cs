using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using Godot;
using System.Linq;

namespace STS2Mobile.Launcher;
internal sealed partial class ShaderWarmupScreen
{
    private static partial class ShaderWarmupMaterialScanner
    {
        private const string GodotShaderExtension = ".gdshader";
        private const string MaterialExtension = ".material";
        private const string RemapExtension = ".remap";
        private const string ResourceRoot = "res://";
        private const string SceneExtension = ".tscn";
        private const string SceneRoot = "res://scenes";
        private const string TresExtension = ".tres";
        internal static async Task<ShaderWarmupMaterialScanResult> CollectAsync(SceneTree tree, ShaderWarmupProgress progress, LauncherMonotonicDeadline deadline)
        {
            var materials = new WarmupMaterialCollection();
            var diagnostics = new ShaderWarmupMaterialScanDiagnostics();
            await ScanLooseMaterialsAsync(materials, tree, progress, deadline, diagnostics);
            if (!deadline.IsExpired)
                await ScanScenesAsync(materials, tree, progress, deadline, diagnostics);
            else
            {
                diagnostics.SceneScanStoppedByBudget = true;
                diagnostics.MarkDeadlineReached();
            }

            diagnostics.MaterialsBeforeDedup = materials.Count;
            var unique = await materials.UniqueByShaderAsync(tree, deadline, diagnostics);
            diagnostics.UniqueMaterialCount = unique.Count;
            PatchHelper.Log(Message.UniqueShaders(materials.Count, unique.Count));
            diagnostics.LogSummary();
            return new ShaderWarmupMaterialScanResult(unique, diagnostics);
        }

        private static bool TryCreateMaterial(Resource resource, out Material material)
        {
            material = resource switch
            {
                Material resMat => resMat,
                Shader resShader => new ShaderMaterial
                {
                    Shader = resShader,
                },
                _ => null,
            };
            return material != null;
        }

        private static string GetShaderKey(Material mat)
        {
            if (mat is ShaderMaterial sm && sm.Shader != null)
                return sm.Shader.ResourcePath ?? sm.Shader.GetRid().ToString();
            if (mat is ParticleProcessMaterial)
                return $"particle#{mat.GetRid()}";
            return mat.ResourcePath ?? mat.GetRid().ToString();
        }

        private static string CleanResourceFileName(string fileName) => fileName.Replace(RemapExtension, "");
        private static string CleanResourcePath(string dirPath, string cleanName) => ChildPath(dirPath, cleanName);
        private static bool IsSceneFile(string cleanName) => cleanName.EndsWith(SceneExtension);
        private static bool IsSupportedMaterialFile(string cleanName) => cleanName.EndsWith(TresExtension) || cleanName.EndsWith(GodotShaderExtension) || cleanName.EndsWith(MaterialExtension);
        private sealed class WarmupMaterialCollection
        {
            private readonly Dictionary<string, Material> _materials = new();
            internal int Count => _materials.Count;

            internal bool Contains(string path) => _materials.ContainsKey(path);
            internal void SetMaterial(string path, Material material) => _materials[path] = material;
            internal void SetResource(string path, Resource resource)
            {
                if (TryCreateMaterial(resource, out var material))
                    SetMaterial(path, material);
            }

            internal void TryAddResource(string path, Resource resource)
            {
                if (TryCreateMaterial(resource, out var material))
                    _materials.TryAdd(path, material);
            }

            internal async Task<List<WarmupMaterial>> UniqueByShaderAsync(SceneTree tree, LauncherMonotonicDeadline deadline, ShaderWarmupMaterialScanDiagnostics diagnostics)
            {
                var unique = new Dictionary<string, WarmupMaterial>();
                int processed = 0;
                foreach (var(path, mat)in _materials)
                {
                    if (deadline.IsExpired)
                    {
                        diagnostics.MarkDeduplicationStoppedByBudget(processed);
                        break;
                    }

                    var shaderKey = GetShaderKey(mat);
                    unique.TryAdd(shaderKey, WarmupMaterial.For(path, mat));
                    processed++;
                    if (processed % 32 == 0 && !await LauncherAsyncYield.ProcessFrameAsync(tree, deadline))
                    {
                        diagnostics.MarkDeduplicationStoppedByBudget(processed);
                        break;
                    }
                }

                diagnostics.DeduplicatedMaterialCount = processed;
                return unique.Values.ToList();
            }
        }

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
            internal bool HasFailures => DirectoryEnumerationFailureCount > 0 || PropertyReadFailureCount > 0 || ResourceLoadFailureCount > 0 || SceneExtractionFailureCount > 0;

            internal void LogSummary() => PatchHelper.Log(Message.ScanSummary(this));
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
                lines.Add(HardDeadlineReached ? "Scan classification: stopped cooperatively at the hard deadline" : HasFailures ? "Scan classification: completed with scanner failures; see focused logcat for first failure samples" : "Scan classification: completed without scanner failures");
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

            internal void MarkDeadlineReached() => HardDeadlineReached = true;
            internal void MarkDeduplicationStoppedByBudget(int processed)
            {
                DeduplicatedMaterialCount = processed;
                DeduplicationStoppedByBudget = true;
                MarkDeadlineReached();
            }

            internal void RecordThreadedLoadCompleted() => ThreadedLoadCompletedCount++;
            internal void RecordThreadedLoadRequested() => ThreadedLoadRequestCount++;
            internal void RecordThreadedLoadTimedOut() => ThreadedLoadTimeoutCount++;
            private static bool ShouldLogFailure(ref int loggedCount)
            {
                loggedCount++;
                return loggedCount <= MaxLoggedFailuresPerCategory;
            }
        }

        internal readonly struct ShaderWarmupMaterialScanResult
        {
            internal ShaderWarmupMaterialScanResult(List<WarmupMaterial> materials, ShaderWarmupMaterialScanDiagnostics diagnostics)
            {
                Materials = materials;
                Diagnostics = diagnostics;
            }

            internal List<WarmupMaterial> Materials { get; }
            internal ShaderWarmupMaterialScanDiagnostics Diagnostics { get; }
        }

        private static void VisitFiles(string dirPath, Action<string, string> visitFile, LauncherMonotonicDeadline deadline, ShaderWarmupMaterialScanDiagnostics diagnostics)
        {
            try
            {
                if (deadline.IsExpired)
                {
                    diagnostics.MarkDeadlineReached();
                    return;
                }

                using var dir = DirAccess.Open(dirPath);
                if (dir == null)
                    return;
                dir.ListDirBegin();
                string fileName;
                while ((fileName = dir.GetNext()) != "")
                {
                    if (deadline.IsExpired)
                    {
                        diagnostics.MarkDeadlineReached();
                        break;
                    }

                    if (ShouldSkip(fileName))
                        continue;
                    if (dir.CurrentIsDir())
                    {
                        VisitFiles(ChildPath(dirPath, fileName), visitFile, deadline, diagnostics);
                        continue;
                    }

                    visitFile(dirPath, fileName);
                }

                dir.ListDirEnd();
            }
            catch (Exception ex)
            {
                diagnostics.RecordDirectoryEnumerationFailure(dirPath, ex);
            }
        }

        private static string ChildPath(string dirPath, string fileName) => $"{dirPath}/{fileName}";
        private static bool ShouldSkip(string fileName) => fileName is "." or ".." or "debug";
        private static async Task ScanLooseMaterialsAsync(WarmupMaterialCollection materials, SceneTree tree, ShaderWarmupProgress progress, LauncherMonotonicDeadline deadline, ShaderWarmupMaterialScanDiagnostics diagnostics)
        {
            var paths = CollectLooseMaterialPaths(ResourceRoot, deadline, diagnostics);
            diagnostics.LooseResourceCount = paths.Count;
            for (int index = 0; index < paths.Count; index++)
            {
                if (deadline.IsExpired)
                {
                    diagnostics.MarkDeadlineReached();
                    break;
                }

                var path = paths[index];
                diagnostics.RecordThreadedLoadRequested();
                var result = await LoadMaterialResourceAsync(tree, path, deadline);
                if (result.Outcome == LauncherThreadedLoadOutcome.DeadlineExceeded)
                {
                    diagnostics.RecordThreadedLoadTimedOut();
                    diagnostics.MarkDeadlineReached();
                    break;
                }

                if (result.Outcome == LauncherThreadedLoadOutcome.Failed)
                {
                    diagnostics.RecordResourceLoadFailure(path, result.Failure);
                }
                else
                {
                    diagnostics.RecordThreadedLoadCompleted();
                    materials.SetResource(path, result.Resource);
                    diagnostics.LoadedLooseResourceCount++;
                }

                if ((index + 1) % 32 == 0)
                {
                    progress.ShowMaterialsFound(materials.Count);
                    if (!await LauncherAsyncYield.ProcessFrameAsync(tree, deadline))
                    {
                        diagnostics.MarkDeadlineReached();
                        break;
                    }
                }
            }

            PatchHelper.Log(Message.FoundLooseMaterials(materials.Count));
            progress.ShowMaterialsFound(materials.Count);
            await LauncherAsyncYield.ProcessFrameAsync(tree, deadline);
        }

        private static async Task<LauncherThreadedLoadResult> LoadMaterialResourceAsync(SceneTree tree, string path, LauncherMonotonicDeadline deadline)
        {
            return await LauncherThreadedResourceLoader.LoadAsync(tree, path, string.Empty, deadline);
        }

        private static List<string> CollectLooseMaterialPaths(string dirPath, LauncherMonotonicDeadline deadline, ShaderWarmupMaterialScanDiagnostics diagnostics)
        {
            var paths = new List<string>();
            var seen = new HashSet<string>();
            VisitFiles(dirPath, (currentDir, fileName) => TryCollectMaterialPath(currentDir, fileName, paths, seen), deadline, diagnostics);
            return paths;
        }

        private static void TryCollectMaterialPath(string dirPath, string fileName, List<string> paths, HashSet<string> seen)
        {
            var cleanName = CleanResourceFileName(fileName);
            if (!IsSupportedMaterialFile(cleanName))
                return;
            var cleanPath = CleanResourcePath(dirPath, cleanName);
            if (seen.Add(cleanPath))
                paths.Add(cleanPath);
        }
    }

    private static partial class Message
    {
        internal static string FoundScenes(int sceneCount) => $"[ShaderWarmup] Found {sceneCount} scenes to scan";
        internal static string SceneScanStoppedByBudget(int scannedSceneCount, int totalSceneCount) => $"[ShaderWarmup] Stopped scene scan after {scannedSceneCount}/{totalSceneCount} scenes due to warmup time budget";
        internal static string UniqueShaders(int materialCount, int uniqueShaderCount) => $"[ShaderWarmup] {materialCount} total materials, {uniqueShaderCount} unique shaders";
        internal static string PropertyReadFailed(string propertyName, string scenePath, Exception ex) => $"[ShaderWarmup] Failed to read property {propertyName} in {scenePath}: {ex.Message}";
        internal static string SceneExtractFailed(string scenePath, string failure) => $"[ShaderWarmup] Failed to extract from {scenePath}: {failure}";
        internal static string FoundLooseMaterials(int materialCount) => $"[ShaderWarmup] Found {materialCount} materials from loose resource files";
        internal static string DirectoryEnumerationFailed(string dirPath, Exception ex) => $"[ShaderWarmup] Failed to enumerate {dirPath}: {ex.Message}";
        internal static string ResourceLoadFailed(string cleanPath, string failure) => $"[ShaderWarmup] Failed to load {cleanPath}: {failure}";
        internal static string ScanSummary(ShaderWarmupMaterialScanner.ShaderWarmupMaterialScanDiagnostics diagnostics) => "[ShaderWarmup] Scan summary: " + $"scenes={diagnostics.ScannedSceneCount}/{diagnostics.SceneCount}; " + $"loads={diagnostics.ThreadedLoadCompletedCount}/{diagnostics.ThreadedLoadRequestCount}; " + $"loadTimeouts={diagnostics.ThreadedLoadTimeoutCount}; " + $"materials={diagnostics.UniqueMaterialCount}/{diagnostics.MaterialsBeforeDedup} unique; " + $"deduplicated={diagnostics.DeduplicatedMaterialCount}/{diagnostics.MaterialsBeforeDedup}; " + $"dedupeBudgetStopped={diagnostics.DeduplicationStoppedByBudget}; " + $"budgetStopped={diagnostics.SceneScanStoppedByBudget}; " + $"deadlineReached={diagnostics.HardDeadlineReached}; " + "failures=" + $"directories:{diagnostics.DirectoryEnumerationFailureCount}, " + $"resources:{diagnostics.ResourceLoadFailureCount}, " + $"scenes:{diagnostics.SceneExtractionFailureCount}, " + $"properties:{diagnostics.PropertyReadFailureCount}";
    }
}
