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
        return TryReadReadyIdentity(
            dataDir,
            branch,
            out _,
            out problem
        );
    }

    internal static bool TryReadReadyIdentity(
        string dataDir,
        string branch,
        out GameIdentity identity,
        out string problem
    )
    {
        branch = SteamGameBranch.Normalize(branch);
        identity = null;
        problem = string.Empty;
        PatchHelper.Log($"[Launcher] Game files ready phase: resolve PCK path for branch '{branch}'");
        var pckPath = PckPath(dataDir, branch);
        PatchHelper.Log($"[Launcher] Game files ready phase complete: resolve PCK path -> '{pckPath}' length={(pckPath == null ? -1 : pckPath.Length)} rooted={(!string.IsNullOrWhiteSpace(pckPath) && Path.IsPathRooted(pckPath))}");
        PatchHelper.Log("[Launcher] Game files ready phase: validate PCK");
        if (!IsValidPck(pckPath))
        {
            PatchHelper.Log("[Launcher] Game files ready phase complete: validate PCK -> false");
            problem = "Download the selected branch to continue.";
            return false;
        }

        PatchHelper.Log("[Launcher] Game files ready phase complete: validate PCK -> true");
        PatchHelper.Log("[Launcher] Game files ready phase: authoritative installed identity");
        try
        {
            identity = GameIdentityReader.ReadInstalled(dataDir, branch);
        }
        catch (GameIdentityException ex)
        {
            problem = "The selected branch is incomplete. Repair selected branch, then download it again.";
            PatchHelper.Log($"[Launcher] Game files ready phase complete: authoritative installed identity -> {ex.Kind}: {ex.Message}");
            return false;
        }

        PatchHelper.Log($"[Launcher] Game files ready phase complete: authoritative installed identity -> {identity.Id}");
        PatchHelper.Log("[Launcher] Game files ready phase: installation state");
        if (!BranchInstallStateStore.Current.TryReadReady(
                dataDir,
                branch,
                identity,
                out _,
                out var stateProblem
            ))
        {
            PatchHelper.Log($"[Launcher] Game files ready phase complete: installation state -> blocked: {stateProblem}");
            problem = "The selected branch is incomplete. Repair selected branch, then download it again.";
            identity = null;
            return false;
        }

        PatchHelper.Log("[Launcher] Game files ready phase complete: installation state -> ready");
        return true;
    }
}
