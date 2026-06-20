using System.IO;
using STS2Mobile.Patches;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static partial class LauncherGameFiles
{
    internal static bool Ready(string dataDir)
        => Ready(dataDir, LauncherPreferences.ReadGameBranch());

    internal static bool Ready(string dataDir, string branch)
    {
        if (!DownloadedForValidation(dataDir, branch))
            return false;

        PatchHelper.Log($"[Launcher] Game files ready phase: inspect runtime slot for branch '{SteamGameBranch.Normalize(branch)}'");
        var slot = GameRuntimeSlot.Inspect(dataDir, branch);
        PatchHelper.Log($"[Launcher] Game files ready phase complete: inspect runtime slot -> playable={slot.Playable} runtime={slot.RuntimePairingStatus} patch={slot.PatchCompatibility?.Status ?? "<none>"} pck={slot.PckSha256} source={slot.SourceAssemblySha256}");
        LauncherRuntimeSlotEvidence.Write(
            dataDir,
            slot,
            slot.Playable,
            slot.Playable ? string.Empty : slot.ReadinessProblem()
        );
        return slot.Playable;
    }

    internal static bool DownloadedForValidation(string dataDir, string branch)
    {
        branch = SteamGameBranch.Normalize(branch);
        PatchHelper.Log($"[Launcher] Game files ready phase: resolve PCK path for branch '{branch}'");
        var pckPath = PckPath(dataDir, branch);
        PatchHelper.Log($"[Launcher] Game files ready phase complete: resolve PCK path -> '{pckPath}' length={(pckPath == null ? -1 : pckPath.Length)} rooted={(!string.IsNullOrWhiteSpace(pckPath) && Path.IsPathRooted(pckPath))}");
        PatchHelper.Log("[Launcher] Game files ready phase: validate PCK");
        if (!IsValidPck(pckPath))
        {
            PatchHelper.Log("[Launcher] Game files ready phase complete: validate PCK -> false");
            return false;
        }

        PatchHelper.Log("[Launcher] Game files ready phase complete: validate PCK -> true");
        PatchHelper.Log("[Launcher] Game files ready phase: branch marker");
        if (!BranchMarkerReady(dataDir, branch))
        {
            PatchHelper.Log("[Launcher] Game files ready phase complete: branch marker -> false");
            return false;
        }

        PatchHelper.Log("[Launcher] Game files ready phase complete: branch marker -> true");
        PatchHelper.Log("[Launcher] Game files ready phase: source assembly exists");
        var sourceAssemblyExists = SourceAssemblyExists(GameDirectoryPath(dataDir, branch));
        PatchHelper.Log($"[Launcher] Game files ready phase complete: source assembly exists -> {sourceAssemblyExists}");
        return sourceAssemblyExists;
    }

    internal static string ReadinessProblem(string dataDir, string branch)
    {
        branch = SteamGameBranch.Normalize(branch);
        if (!IsValidPck(PckPath(dataDir, branch)))
            return "Selected game version is not downloaded or the downloaded PCK is invalid. Download selected version to continue.";

        if (HasBranchMetadataProblem(dataDir, branch))
            return "Selected game version has missing or mismatched branch metadata. Redownload selected version to rebuild the cache safely.";

        var runtimeProblem = GameRuntimeSlot.Inspect(dataDir, branch).ReadinessProblem();
        if (!string.IsNullOrWhiteSpace(runtimeProblem))
            return runtimeProblem;

        return null;
    }

    private static bool SourceAssemblyExists(string gameDirectory)
    {
        if (string.IsNullOrWhiteSpace(gameDirectory) || !Directory.Exists(gameDirectory))
            return false;

        foreach (var directory in Directory.EnumerateDirectories(gameDirectory, "data_*", SearchOption.TopDirectoryOnly))
        {
            if (File.Exists(Path.Combine(directory, "sts2.dll")))
                return true;
        }

        return false;
    }
}
