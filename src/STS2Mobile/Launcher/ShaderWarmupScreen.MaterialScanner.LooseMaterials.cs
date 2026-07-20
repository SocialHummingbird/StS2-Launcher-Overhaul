using System.Threading.Tasks;
using Godot;

namespace STS2Mobile.Launcher;

internal sealed partial class ShaderWarmupScreen
{
    private static partial class ShaderWarmupMaterialScanner
    {
        private static async Task ScanLooseMaterialsAsync(
            WarmupMaterialCollection materials,
            SceneTree tree,
            ShaderWarmupProgress progress,
            LauncherMonotonicDeadline deadline,
            ShaderWarmupMaterialScanDiagnostics diagnostics
        )
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
    }
}
