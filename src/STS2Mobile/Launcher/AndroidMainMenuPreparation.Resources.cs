using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Godot;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static partial class AndroidMainMenuPreparation
{
    private static async Task PreloadWorkingSetAsync(
        Node gameNode,
        Label startupStatus,
        PreparationState state,
        LauncherMonotonicDeadline overallDeadline,
        LauncherOperationLifecycle lifecycle
    )
    {
        var tree = gameNode.GetTree();
        var timer = Stopwatch.StartNew();
        var resourceDeadline = overallDeadline.CreateChild(
            TimeSpan.FromMilliseconds(ResourceTimeBudgetMs)
        );
        var plan = AndroidMainMenuWorkingSetPlan.Create(
            AndroidMainMenuWorkingSet.Resources,
            path => ResourceLoader.Exists(path)
        );
        state.Missing += plan.MissingLogicalPaths.Count;
        state.MissingLogicalPaths.AddRange(plan.MissingLogicalPaths);
        state.DuplicateResources = plan.DuplicateCount;
        state.RejectedResources = plan.RejectedCount;

        foreach (var entry in plan.Resources)
        {
            if (resourceDeadline.IsExpired)
            {
                state.ResourceBudgetReached = true;
                break;
            }

            state.Attempted++;
            if (!await TryLoadAsync(
                entry,
                state,
                tree,
                resourceDeadline,
                lifecycle
            ))
                break;

            if (state.Attempted % ResourceBatchSize == 0)
            {
                LauncherStartupStatus.Set(
                    startupStatus,
                    $"Preparing home screen resources... {state.Attempted}/{plan.Resources.Count}"
                );
                if (!await LauncherAsyncYield.ProcessFrameAsync(
                    tree,
                    resourceDeadline,
                    lifecycle
                ))
                {
                    state.ResourceBudgetReached = true;
                    break;
                }
            }
        }

        await RenderMaterialsAsync(
            gameNode,
            tree,
            resourceDeadline,
            state,
            lifecycle
        );
        timer.Stop();
        state.ResourceElapsedMs = timer.ElapsedMilliseconds;
    }

    private static async Task<bool> TryLoadAsync(
        AndroidMainMenuResource entry,
        PreparationState state,
        SceneTree tree,
        LauncherMonotonicDeadline deadline,
        LauncherOperationLifecycle lifecycle
    )
    {
        var result = await LauncherThreadedResourceLoader.LoadAsync(
            tree,
            entry.LogicalPath,
            string.Empty,
            deadline,
            lifecycle
        );
        if (result.Outcome == LauncherThreadedLoadOutcome.Cancelled)
        {
            state.ResourceBudgetReached = true;
            PatchHelper.Log(
                $"[MainMenuPreparation] Resource preload cancelled for {entry.LogicalPath}"
            );
            return false;
        }

        if (result.Outcome == LauncherThreadedLoadOutcome.DeadlineExceeded)
        {
            state.ResourceBudgetReached = true;
            PatchHelper.Log(
                $"[MainMenuPreparation] Optional working-set budget exhausted at {entry.LogicalPath}; continuing to rendered-frame stability"
            );
            return false;
        }

        if (result.Outcome == LauncherThreadedLoadOutcome.Failed)
        {
            state.Failed++;
            PatchHelper.Log(
                $"[MainMenuPreparation] Resource preload failed for {entry.LogicalPath}: {result.Failure}"
            );
            return true;
        }

        var resource = result.Resource;
        state.RetainedResources.Add(resource);
        state.Resolutions.Add(
            AndroidMainMenuResourceResolution.Create(
                entry.LogicalPath,
                resource.ResourcePath
            )
        );
        state.Loaded++;
        if (resource is Material material)
            state.RenderMaterials.Add(material);
        else if (resource is Shader shader)
        {
            state.RenderMaterials.Add(
                new ShaderMaterial
                {
                    Shader = shader,
                }
            );
        }

        return true;
    }

    private static async Task RenderMaterialsAsync(
        Node gameNode,
        SceneTree tree,
        LauncherMonotonicDeadline deadline,
        PreparationState state,
        LauncherOperationLifecycle lifecycle
    )
    {
        if (tree == null || state.RenderMaterials.Count == 0 || deadline.IsExpired)
            return;

        var viewport = new SubViewport
        {
            Size = new Vector2I(64, 64),
            RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
            TransparentBg = true,
        };
        gameNode.AddChild(viewport);
        var texture = CreateWhiteTexture();
        try
        {
            for (int index = 0; index < state.RenderMaterials.Count; index += ResourceBatchSize)
            {
                if (deadline.IsExpired)
                {
                    state.ResourceBudgetReached = true;
                    break;
                }

                var nodes = new List<Node>();
                try
                {
                    int end = Math.Min(index + ResourceBatchSize, state.RenderMaterials.Count);
                    for (int materialIndex = index; materialIndex < end; materialIndex++)
                    {
                        var sprite = new Sprite2D
                        {
                            Texture = texture,
                            Material = state.RenderMaterials[materialIndex],
                        };
                        viewport.AddChild(sprite);
                        nodes.Add(sprite);
                    }

                    if (!await LauncherAsyncYield.FramePostDrawAsync(deadline, lifecycle)
                        || !await LauncherAsyncYield.FramePostDrawAsync(deadline, lifecycle))
                    {
                        state.ResourceBudgetReached = true;
                        break;
                    }

                    state.RenderedMaterials += nodes.Count;
                }
                finally
                {
                    foreach (var node in nodes)
                        node.QueueFree();
                }
            }
        }
        finally
        {
            viewport.QueueFree();
        }
    }

    private static ImageTexture CreateWhiteTexture()
    {
        var image = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8);
        image.SetPixel(0, 0, Colors.White);
        return ImageTexture.CreateFromImage(image);
    }
}
