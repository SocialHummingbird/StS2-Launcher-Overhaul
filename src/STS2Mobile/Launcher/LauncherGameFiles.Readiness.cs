using System.IO;
using STS2Mobile.Patches;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static partial class LauncherGameFiles
{
    internal static bool DownloadedForValidation(
        string dataDir,
        string branch,
        out string problem
    )
    {
        var result = ValidateDownloadedStateForLaunch(dataDir, branch);
        problem = result.Problem;
        return result.Ready;
    }

    private static DownloadValidationResult ValidateDownloadedStateForLaunch(
        string dataDir,
        string branch
    )
    {
        branch = SteamGameBranch.Normalize(branch);
        PatchHelper.Log($"[Launcher] Game files ready phase: resolve PCK path for branch '{branch}'");
        var pckPath = PckPath(dataDir, branch);
        PatchHelper.Log($"[Launcher] Game files ready phase complete: resolve PCK path -> '{pckPath}' length={(pckPath == null ? -1 : pckPath.Length)} rooted={(!string.IsNullOrWhiteSpace(pckPath) && Path.IsPathRooted(pckPath))}");
        PatchHelper.Log("[Launcher] Game files ready phase: validate PCK");
        if (!IsValidPck(pckPath))
        {
            PatchHelper.Log("[Launcher] Game files ready phase complete: validate PCK -> false");
            return DownloadValidationResult.Blocked(
                "Selected game version is not downloaded or the downloaded PCK is invalid. Download selected version to continue."
            );
        }

        PatchHelper.Log("[Launcher] Game files ready phase complete: validate PCK -> true");
        PatchHelper.Log("[Launcher] Game files ready phase: branch marker");
        if (!BranchMarkerReady(dataDir, branch))
        {
            PatchHelper.Log("[Launcher] Game files ready phase complete: branch marker -> false");
            return DownloadValidationResult.Blocked(
                "Selected game version has missing or mismatched branch metadata. Redownload selected version to rebuild the cache safely."
            );
        }

        PatchHelper.Log("[Launcher] Game files ready phase complete: branch marker -> true");
        PatchHelper.Log("[Launcher] Game files ready phase: source assembly path");
        var sourceAssemblyPath = GameRuntimeSlot.FindSourceAssemblyPath(GameDirectoryPath(dataDir, branch));
        PatchHelper.Log($"[Launcher] Game files ready phase complete: source assembly path -> '{sourceAssemblyPath}'");
        PatchHelper.Log("[Launcher] Game files ready phase: source assembly exists");
        var sourceAssemblyExists = File.Exists(sourceAssemblyPath);
        PatchHelper.Log($"[Launcher] Game files ready phase complete: source assembly exists -> {sourceAssemblyExists}");
        return sourceAssemblyExists
            ? DownloadValidationResult.Passed()
            : DownloadValidationResult.Blocked(
                "Selected game version is missing its source game-code assembly. Redownload selected version to rebuild runtime evidence."
            );
    }

    private readonly struct DownloadValidationResult
    {
        private DownloadValidationResult(bool ready, string problem)
        {
            Ready = ready;
            Problem = problem ?? string.Empty;
        }

        internal bool Ready { get; }
        internal string Problem { get; }

        internal static DownloadValidationResult Passed()
            => new(true, string.Empty);

        internal static DownloadValidationResult Blocked(string problem)
            => new(false, problem);
    }
}
