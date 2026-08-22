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
            problem = "Selected game version is not downloaded or the downloaded PCK is invalid. Download selected version to continue.";
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
            problem = ex.Message;
            PatchHelper.Log($"[Launcher] Game files ready phase complete: authoritative installed identity -> {ex.Kind}: {problem}");
            return false;
        }

        PatchHelper.Log($"[Launcher] Game files ready phase complete: authoritative installed identity -> {identity.Id}");
        PatchHelper.Log("[Launcher] Game files ready phase: installation state");
        if (!BranchInstallStateStore.Current.TryReadReady(
                dataDir,
                branch,
                identity,
                out _,
                out problem
            ))
        {
            PatchHelper.Log($"[Launcher] Game files ready phase complete: installation state -> blocked: {problem}");
            identity = null;
            return false;
        }

        PatchHelper.Log("[Launcher] Game files ready phase complete: installation state -> ready");
        return true;
    }
}
