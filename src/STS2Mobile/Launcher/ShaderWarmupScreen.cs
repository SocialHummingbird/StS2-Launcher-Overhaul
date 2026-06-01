using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher;

// Compiles shaders on first launch by collecting materials from resources and scenes,
// rendering them in a SubViewport, then writing a version marker to skip on future launches.
internal sealed class ShaderWarmupScreen : Control
{
    private const int WarmupVersion = 5;

    private TaskCompletionSource<bool> _tcs;
    private Label _statusLabel;
    private Label _detailLabel;
    private ProgressBar _progressBar;

    private static string MarkerPath =>
        Path.Combine(OS.GetUserDataDir(), LauncherStorageNames.ShaderWarmupVersion);

    internal static bool NeedsWarmup()
    {
        try
        {
            if (File.Exists(MarkerPath))
            {
                var content = File.ReadAllText(MarkerPath).Trim();
                if (content == WarmupVersion.ToString())
                {
                    PatchHelper.Log(Message.MarkerMatches(content));
                    return false;
                }
                PatchHelper.Log(Message.MarkerMismatch(content, WarmupVersion));
            }
            else
            {
                PatchHelper.Log(Message.MarkerMissing());
            }

            return true;
        }
        catch (Exception ex)
        {
            PatchHelper.Log(Message.MarkerCheckFailed(ex));
            return true;
        }
    }

    internal Task WaitForCompletion()
    {
        _tcs = new TaskCompletionSource<bool>();
        return _tcs.Task;
    }

    internal void Initialize()
    {
        ZIndex = 100;

        try
        {
            var vpSize = GetViewport()?.GetVisibleRect().Size ?? new Vector2(1920, 1080);
            SetAnchorsPreset(LayoutPreset.FullRect);
            Size = vpSize;
            BuildUI(vpSize);
            PatchHelper.Log(Message.ScreenInitialized);
        }
        catch (Exception ex)
        {
            PatchHelper.Log(Message.ScreenBuildFailed(ex));
            _tcs?.TrySetResult(false);
            return;
        }

        Callable.From(RunWarmup).CallDeferred();
    }

    private void BuildUI(Vector2 vpSize)
    {
        SetAnchorsPreset(LayoutPreset.FullRect);

        var scale = Math.Max(vpSize.X, vpSize.Y) / 960f;

        var bg = new ScreenBackground();
        AddChild(bg);

        var panel = new StyledPanel(scale, widthRatio: 0.5f);
        panel.UpdateSizeFromViewport(vpSize);
        AddChild(panel);

        _statusLabel = new StyledLabel("Compiling shaders...", scale, fontSize: 20);
        panel.Content.AddChild(_statusLabel);

        _progressBar = new StyledProgressBar(scale);
        _progressBar.MinValue = 0;
        _progressBar.MaxValue = 100;
        _progressBar.Value = 0;
        _progressBar.ShowPercentage = true;
        panel.Content.AddChild(_progressBar);

        _detailLabel = new StyledLabel("Enumerating resources...", scale, fontSize: 12);
        _detailLabel.Modulate = new Color(0.7f, 0.7f, 0.7f);
        panel.Content.AddChild(_detailLabel);
    }

    private async void RunWarmup()
    {
        try
        {
            await RunWarmupAsync();
        }
        catch (Exception ex)
        {
            PatchHelper.Log(Message.RunFailed(ex));
        }

        _tcs?.TrySetResult(true);
    }

    private async Task RunWarmupAsync()
    {
        var sw = Stopwatch.StartNew();
        var progress = new ShaderWarmupProgress(_statusLabel, _detailLabel, _progressBar);

        progress.ShowScanning();
        await WaitPostDrawAsync();

        var materials = await ShaderWarmupMaterialScanner.CollectAsync(GetTree, progress);
        PatchHelper.Log(Message.Collected(materials.Count));

        progress.ShowCompiling();

        if (materials.Count == 0)
        {
            WriteWarmupVersion();
            return;
        }

        var renderer = new ShaderWarmupRenderer(this, GetTree, progress);
        await renderer.RenderAsync(materials);

        var elapsedMilliseconds = sw.ElapsedMilliseconds;
        progress.Complete(materials.Count, elapsedMilliseconds);
        PatchHelper.Log(Message.Completed(materials.Count, elapsedMilliseconds));

        WriteWarmupVersion();
        await WaitFinishDelayAsync();
    }

    private static void WriteWarmupVersion()
    {
        try
        {
            File.WriteAllText(MarkerPath, WarmupVersion.ToString());
        }
        catch (Exception ex)
        {
            PatchHelper.Log(Message.MarkerWriteFailed(ex));
        }
    }

    private async Task WaitPostDrawAsync()
    {
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
    }

    private async Task WaitFinishDelayAsync()
    {
        await ToSignal(GetTree().CreateTimer(0.5), SceneTreeTimer.SignalName.Timeout);
    }

    private static class Message
    {
        private const string ScanningStatus = "Scanning for shaders...";
        private const string CompilingStatus = "Compiling shaders...";
        private const string DoneStatus = "Done!";
        private const string ScreenInitialized = "[ShaderWarmup] Screen initialized";

        private static string Collected(int materialCount)
            => $"[ShaderWarmup] Collected {materialCount} materials to warm";

        private static string FoundScenes(int sceneCount)
            => $"[ShaderWarmup] Found {sceneCount} scenes to scan";

        private static string Compiled(int materialCount, long elapsedMilliseconds)
            => $"Compiled {materialCount} shaders in {elapsedMilliseconds}ms";

        private static string Completed(int materialCount, long elapsedMilliseconds)
            => $"[ShaderWarmup] Completed: {materialCount} materials in {elapsedMilliseconds}ms";

        private static string UniqueShaders(int materialCount, int uniqueShaderCount)
            => $"[ShaderWarmup] {materialCount} total materials, {uniqueShaderCount} unique shaders";

        private static string PropertyReadFailed(string propertyName, string scenePath, Exception ex)
            => $"[ShaderWarmup] Failed to read property {propertyName} in {scenePath}: {ex.Message}";

        private static string SceneExtractFailed(string scenePath, Exception ex)
            => $"[ShaderWarmup] Failed to extract from {scenePath}: {ex.Message}";

        private static string FoundLooseMaterials(int materialCount)
            => $"[ShaderWarmup] Found {materialCount} materials from loose resource files";

        private static string FoundMaterialsDetail(int materialCount)
            => $"Found {materialCount} materials...";

        private static string DirectoryEnumerationFailed(string dirPath, Exception ex)
            => $"[ShaderWarmup] Failed to enumerate {dirPath}: {ex.Message}";

        private static string ResourceLoadFailed(string cleanPath, Exception ex)
            => $"[ShaderWarmup] Failed to load {cleanPath}: {ex.Message}";

        private static string ScanningScenes(int index, int total)
            => $"Scanning scenes... {index} / {total}";

        private static string MarkerCheckFailed(Exception ex)
            => $"[ShaderWarmup] NeedsWarmup check failed: {ex.Message}";

        private static string MarkerMissing()
            => "[ShaderWarmup] NeedsWarmup=true (no marker file)";

        private static string MarkerMatches(string content)
            => $"[ShaderWarmup] NeedsWarmup=false (marker v{content} matches)";

        private static string MarkerMismatch(string content, int expectedVersion)
            => $"[ShaderWarmup] NeedsWarmup=true (marker v{content} != v{expectedVersion})";

        private static string MarkerWriteFailed(Exception ex)
            => $"[ShaderWarmup] Failed to write version marker: {ex.Message}";

        private static string ScreenBuildFailed(Exception ex)
            => $"[ShaderWarmup] BuildUI failed: {ex}";

        private static string RunFailed(Exception ex)
            => $"[ShaderWarmup] Failed: {ex}";
    }

    private sealed class ShaderWarmupProgress
    {
        private readonly Label _statusLabel;
        private readonly Label _detailLabel;
        private readonly ProgressBar _progressBar;

        private ShaderWarmupProgress(Label statusLabel, Label detailLabel, ProgressBar progressBar)
        {
            _statusLabel = statusLabel;
            _detailLabel = detailLabel;
            _progressBar = progressBar;
        }

        private void SetStatus(string text) => _statusLabel.Text = text;

        private void SetDetail(string text) => _detailLabel.Text = text;

        private void SetProgress(double progress) => _progressBar.Value = progress;

        private void Complete(int materialCount, long elapsedMilliseconds)
        {
            _progressBar.Value = 100;
            _statusLabel.Text = Message.DoneStatus;
            _detailLabel.Text = Message.Compiled(materialCount, elapsedMilliseconds);
        }

        private void ShowCompiling() => SetStatus(Message.CompilingStatus);

        private void ShowScanning() => SetStatus(Message.ScanningStatus);
    }

    private sealed class ShaderWarmupRenderer
    {
        private const int BatchSize = 8;
        private const int ParticleAmount = 1;
        private const int TextureHeight = 1;
        private const int TextureWidth = 1;
        private const int ViewportHeight = 64;
        private const int ViewportWidth = 64;

        private readonly Control _parent;
        private readonly Func<SceneTree> _getTree;
        private readonly ShaderWarmupProgress _progress;

        private ShaderWarmupRenderer(
            Control parent,
            Func<SceneTree> getTree,
            ShaderWarmupProgress progress
        )
        {
            _parent = parent;
            _getTree = getTree;
            _progress = progress;
        }

        private async Task RenderAsync(List<(string path, Material mat)> materials)
        {
            var viewport = CreateViewport();
            _parent.AddChild(viewport);
            try
            {
                await RenderBatchesAsync(viewport, CreateWhiteTexture(), materials);
            }
            finally
            {
                viewport.QueueFree();
            }
        }

        private async Task RenderBatchesAsync(
            SubViewport viewport,
            ImageTexture whiteTexture,
            List<(string path, Material mat)> materials
        )
        {
            int total = materials.Count;
            for (int i = 0; i < total; i += BatchSize)
            {
                int batchEnd = Math.Min(i + BatchSize, total);
                var batchNodes = AddBatchNodes(
                    viewport,
                    whiteTexture,
                    materials,
                    i,
                    batchEnd
                );

                ReportProgress(batchEnd, total);

                await WaitForRenderFramesAsync();
                ClearBatch(batchNodes);
            }
        }

        private async Task WaitForRenderFramesAsync()
        {
            var tree = _getTree();
            if (tree == null)
                return;

            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }

        private static SubViewport CreateViewport()
            => new()
            {
                Size = new Vector2I(ViewportWidth, ViewportHeight),
                RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
                TransparentBg = true,
            };

        private static ImageTexture CreateWhiteTexture()
        {
            var whiteImage = Image.CreateEmpty(
                TextureWidth,
                TextureHeight,
                false,
                Image.Format.Rgba8
            );
            whiteImage.SetPixel(0, 0, Colors.White);
            return ImageTexture.CreateFromImage(whiteImage);
        }

        private static List<Node> AddBatchNodes(
            SubViewport viewport,
            ImageTexture whiteTexture,
            List<(string path, Material mat)> materials,
            int start,
            int end
        )
        {
            var batchNodes = new List<Node>();
            for (int i = start; i < end; i++)
            {
                var (path, mat) = materials[i];
                try
                {
                    Node node = CreateNode(mat, whiteTexture);
                    if (node != null)
                    {
                        viewport.AddChild(node);
                        batchNodes.Add(node);
                    }
                }
                catch (Exception ex)
                {
                    PatchHelper.Log($"[ShaderWarmup] Failed to create node for {path}: {ex.Message}");
                }
            }

            return batchNodes;
        }

        private static Node CreateNode(Material mat, ImageTexture whiteTexture)
            => mat is ParticleProcessMaterial particleMat
                ? new GpuParticles2D
                {
                    ProcessMaterial = particleMat,
                    Amount = ParticleAmount,
                    Emitting = true,
                    OneShot = false,
                    Texture = whiteTexture,
                }
                : new Sprite2D
                {
                    Texture = whiteTexture,
                    Material = mat,
                };

        private static void ClearBatch(List<Node> nodes)
        {
            foreach (var node in nodes)
                node.QueueFree();
        }

        private void ReportProgress(int completed, int total)
        {
            double pct = 50 + (double)completed / total * 50;
            _progress.SetProgress(pct);
            _progress.SetDetail($"Compiling {completed} / {total}");
        }
    }

    private static class ShaderWarmupMaterialScanner
    {
        private const string GodotShaderExtension = ".gdshader";
        private const string MaterialExtension = ".material";
        private const string RemapExtension = ".remap";
        private const string ResourceRoot = "res://";
        private const string SceneExtension = ".tscn";
        private const string SceneRoot = "res://scenes";
        private const string TresExtension = ".tres";

        private static async Task<List<(string path, Material mat)>> CollectAsync(
            Func<SceneTree> getTree,
            ShaderWarmupProgress progress
        )
        {
            var materials = new Dictionary<string, Material>();

            await ScanLooseMaterialsAsync(materials, getTree, progress);
            await ScanScenesAsync(materials, getTree, progress);

            var unique = new Dictionary<string, (string path, Material mat)>();
            foreach (var (path, mat) in materials)
            {
                var shaderKey = GetShaderKey(mat);
                unique.TryAdd(shaderKey, (path, mat));
            }

            PatchHelper.Log(Message.UniqueShaders(materials.Count, unique.Count));
            return unique.Values.ToList();
        }

        private static async Task ScanLooseMaterialsAsync(
            Dictionary<string, Material> materials,
            Func<SceneTree> getTree,
            ShaderWarmupProgress progress
        )
        {
            CollectLooseMaterials(ResourceRoot, materials);
            PatchHelper.Log(Message.FoundLooseMaterials(materials.Count));
            progress.SetDetail(Message.FoundMaterialsDetail(materials.Count));
            var tree = getTree();
            if (tree != null)
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }

        private static async Task ScanScenesAsync(
            Dictionary<string, Material> materials,
            Func<SceneTree> getTree,
            ShaderWarmupProgress progress
        )
        {
            var scenePaths = new List<string>();
            CollectScenePaths(SceneRoot, scenePaths);
            PatchHelper.Log(Message.FoundScenes(scenePaths.Count));

            for (int i = 0; i < scenePaths.Count; i++)
            {
                ExtractSceneMaterials(scenePaths[i], materials);
                await ReportSceneScanProgressIfNeededAsync(getTree, progress, i, scenePaths.Count);
            }
        }

        private static async Task ReportSceneScanProgressIfNeededAsync(
            Func<SceneTree> getTree,
            ShaderWarmupProgress progress,
            int index,
            int total
        )
        {
            if (index % 50 != 0)
                return;

            progress.SetDetail(Message.ScanningScenes(index, total));
            if (total > 0)
                progress.SetProgress((double)index / total * 50);
            var tree = getTree();
            if (tree != null)
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }

        private static void CollectLooseMaterials(string dirPath, Dictionary<string, Material> materials)
        {
            try
            {
                using var dir = DirAccess.Open(dirPath);
                if (dir == null)
                    return;

                dir.ListDirBegin();
                string fileName;
                while ((fileName = dir.GetNext()) != "")
                {
                    if (ShouldSkip(fileName))
                        continue;

                    if (dir.CurrentIsDir())
                    {
                        CollectLooseMaterials(ChildPath(dirPath, fileName), materials);
                        continue;
                    }

                    TryCollectMaterialFile(dirPath, fileName, materials);
                }
                dir.ListDirEnd();
            }
            catch (Exception ex)
            {
                PatchHelper.Log(Message.DirectoryEnumerationFailed(dirPath, ex));
            }
        }

        private static void TryCollectMaterialFile(
            string dirPath,
            string fileName,
            Dictionary<string, Material> materials
        )
        {
            var cleanName = fileName.Replace(RemapExtension, "");
            if (!IsSupportedMaterialFile(cleanName))
                return;

            var cleanPath = ChildPath(dirPath, cleanName);
            if (materials.ContainsKey(cleanPath))
                return;

            TryLoadMaterialResource(cleanName, cleanPath, materials);
        }

        private static void CollectScenePaths(string dirPath, List<string> paths)
        {
            try
            {
                using var dir = DirAccess.Open(dirPath);
                if (dir == null)
                    return;

                dir.ListDirBegin();
                string fileName;
                while ((fileName = dir.GetNext()) != "")
                {
                    if (ShouldSkip(fileName))
                        continue;

                    if (dir.CurrentIsDir())
                    {
                        CollectScenePaths(ChildPath(dirPath, fileName), paths);
                        continue;
                    }

                    var cleanName = fileName.Replace(RemapExtension, "");
                    if (!cleanName.EndsWith(SceneExtension))
                        continue;

                    var cleanPath = ChildPath(dirPath, cleanName);
                    if (ResourceLoader.Exists(cleanPath))
                        paths.Add(cleanPath);
                }
                dir.ListDirEnd();
            }
            catch (Exception ex)
            {
                PatchHelper.Log(Message.DirectoryEnumerationFailed(dirPath, ex));
            }
        }

        private static void ExtractSceneMaterials(
            string scenePath,
            Dictionary<string, Material> materials
        )
        {
            try
            {
                var packed = ResourceLoader.Load<PackedScene>(
                    scenePath,
                    null,
                    ResourceLoader.CacheMode.Reuse
                );
                if (packed != null)
                    ExtractMaterials(packed, scenePath, materials);
            }
            catch (Exception ex)
            {
                PatchHelper.Log(Message.SceneExtractFailed(scenePath, ex));
            }
        }

        private static void ExtractMaterials(
            PackedScene packed,
            string scenePath,
            Dictionary<string, Material> materials
        )
        {
            var state = packed.GetState();
            int nodeCount = state.GetNodeCount();
            for (int nodeIndex = 0; nodeIndex < nodeCount; nodeIndex++)
            {
                int propertyCount = state.GetNodePropertyCount(nodeIndex);
                for (int propertyIndex = 0; propertyIndex < propertyCount; propertyIndex++)
                {
                    var propName = state.GetNodePropertyName(nodeIndex, propertyIndex).ToString();
                    if (!IsMaterialProperty(propName))
                        continue;

                    TryExtractMaterialProperty(
                        state,
                        scenePath,
                        nodeIndex,
                        propertyIndex,
                        propName,
                        materials
                    );
                }
            }
        }

        private static bool IsMaterialProperty(string propName)
            => propName is "material" or "process_material" or "surface_material_override/0";

        private static void TryExtractMaterialProperty(
            SceneState state,
            string scenePath,
            int nodeIndex,
            int propertyIndex,
            string propName,
            Dictionary<string, Material> materials
        )
        {
            try
            {
                var val = state.GetNodePropertyValue(nodeIndex, propertyIndex);
                if (val.Obj is Resource resource && TryCreateMaterial(resource, out var material))
                    materials.TryAdd($"{scenePath}#node{nodeIndex}#{propName}", material);
            }
            catch (Exception ex)
            {
                PatchHelper.Log(Message.PropertyReadFailed(propName, scenePath, ex));
            }
        }

        private static void TryLoadMaterialResource(
            string cleanName,
            string cleanPath,
            Dictionary<string, Material> materials
        )
        {
            try
            {
                if (!ResourceLoader.Exists(cleanPath))
                    return;

                if (cleanName.EndsWith(TresExtension))
                {
                    if (TryLoadTresMaterial(cleanPath, out var mat))
                        materials[cleanPath] = mat;
                    return;
                }

                var res = ResourceLoader.Load(cleanPath, null, ResourceLoader.CacheMode.Reuse);
                if (TryCreateMaterial(res, out var material))
                    materials[cleanPath] = material;
            }
            catch (Exception ex)
            {
                PatchHelper.Log(Message.ResourceLoadFailed(cleanPath, ex));
            }
        }

        private static bool TryLoadTresMaterial(string cleanPath, out Material material)
        {
            var resource = ResourceLoader.Load(
                cleanPath,
                "Material",
                ResourceLoader.CacheMode.Reuse
            );
            if (TryCreateMaterial(resource, out material))
                return true;

            resource = ResourceLoader.Load(
                cleanPath,
                "Shader",
                ResourceLoader.CacheMode.Reuse
            );
            return TryCreateMaterial(resource, out material);
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

        private static string ChildPath(string dirPath, string fileName)
            => $"{dirPath}/{fileName}";

        private static bool IsSupportedMaterialFile(string cleanName)
            => cleanName.EndsWith(TresExtension)
                || cleanName.EndsWith(GodotShaderExtension)
                || cleanName.EndsWith(MaterialExtension);

        private static bool ShouldSkip(string fileName)
            => fileName is "." or ".." or "debug";

        private static string GetShaderKey(Material mat)
        {
            if (mat is ShaderMaterial sm && sm.Shader != null)
                return sm.Shader.ResourcePath ?? sm.Shader.GetRid().ToString();
            if (mat is ParticleProcessMaterial)
                return $"particle#{mat.GetRid()}";
            return mat.ResourcePath ?? mat.GetRid().ToString();
        }
    }
}
