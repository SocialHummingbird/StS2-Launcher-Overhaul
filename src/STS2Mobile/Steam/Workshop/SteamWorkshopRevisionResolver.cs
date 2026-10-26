using System;
using System.Collections.Generic;
using System.Linq;
using SteamKit2;
using SteamKit2.Internal;

namespace STS2Mobile.Steam.Workshop;

internal static class SteamWorkshopRevisionResolver
{
    internal static SteamWorkshopPreparedRevision Resolve(
        PublishedFileDetails detail,
        CPublishedFile_GetItemInfo_Response.WorkshopItemInfo info,
        string gameBranch,
        SteamWorkshopBranchOrder branchOrder = null
    )
    {
        if (detail == null || detail.publishedfileid == 0 || detail.result != (uint)EResult.OK)
            throw new InvalidOperationException("Steam did not return successful complete Workshop item details");
        if (detail.consumer_appid != 0 && detail.consumer_appid != SteamGameApp.AppId)
            throw new InvalidOperationException("Steam returned Workshop content for a different game");
        if (info == null || info.published_file_id != detail.publishedfileid
            || info.time_updated == 0 || info.time_updated != detail.time_updated
            || info.revision != detail.revision)
            throw new InvalidOperationException("Steam Workshop details and item information do not describe one coherent revision; retry preparation");
        if (info.manifest_id == ulong.MaxValue)
            throw new InvalidOperationException("Steam returned an invalid Workshop manifest id");

        var hcontent = detail.hcontent_file == ulong.MaxValue ? 0 : detail.hcontent_file;
        if (info.manifest_id != 0 && hcontent != info.manifest_id)
            throw new InvalidOperationException("Steam Workshop details content handle and depot manifest disagree; retry preparation");
        if (info.manifest_id == 0 && hcontent == 0 && string.IsNullOrWhiteSpace(detail.file_url))
            throw new InvalidOperationException("Steam did not expose a content identity or download source for this Workshop revision");
        if (!SameSnapshots(detail.author_snapshots, info.author_snapshots))
            throw new InvalidOperationException("Steam author snapshot metadata changed between complete details and item information; retry preparation");

        var branch = SteamGameBranch.Normalize(gameBranch);
        var snapshots = detail.author_snapshots;
        var selectedManifest = info.manifest_id;
        long selectedTimestamp = info.time_updated;
        var selectionKind = "current";
        if (snapshots.Count > 0)
        {
            if (info.manifest_id == 0 || snapshots.Any(snapshot => snapshot.manifestid is 0 or ulong.MaxValue || snapshot.timestamp == 0))
                throw new InvalidOperationException("Steam author snapshots did not expose valid exact depot manifests and timestamps");
            var candidates = snapshots.ToList();
            if (!candidates.Any(snapshot => snapshot.manifestid == info.manifest_id))
            {
                // Native Steam downloaded this implicit current candidate over an
                // older compatible declared snapshot (Skin Changer, 2026-10-25).
                candidates.Add(new PublishedFileAuthorSnapshot
                {
                    timestamp = info.time_updated,
                    manifestid = info.manifest_id,
                });
            }

            PublishedFileAuthorSnapshot selected = null;
            foreach (var group in candidates.GroupBy(snapshot => snapshot.timestamp).OrderByDescending(group => group.Key))
            {
                var compatible = group.Where(snapshot => IsCompatible(snapshot, branch, branchOrder)).ToArray();
                if (compatible.Select(snapshot => snapshot.manifestid).Distinct().Count() > 1)
                    throw new InvalidOperationException($"Steam Workshop selection is ambiguous: different compatible manifests have timestamp {group.Key}");
                if (compatible.Length == 0)
                    continue;
                selected = compatible[0];
                break;
            }
            if (selected == null)
                throw new InvalidOperationException($"Steam did not expose a Workshop revision compatible with game branch '{branch}'");
            selectedManifest = selected.manifestid;
            selectedTimestamp = selected.timestamp;
            selectionKind = snapshots.Contains(selected) ? "author-snapshot" : "implicit-current";
        }

        return new SteamWorkshopPreparedRevision
        {
            ManifestId = selectedManifest,
            HContentFile = selectedManifest == info.manifest_id ? hcontent : 0,
            TimeUpdated = selectedTimestamp,
            SelectedGameBranch = branch,
            Provenance = $"published-file:{selectionKind};manifest={selectedManifest};timestamp={selectedTimestamp};native-selection-rule-corroborated;dependencies=item-level",
            BranchSupport = new SteamWorkshopBranchSupport
            {
                Provenance = "steam-published-file:author-snapshots;native-GetSupportedGameVersionData-matched;" + BranchOrderProvenance(branchOrder),
                BranchOrder = branchOrder?.Branches.ToList() ?? new(),
                Ranges = snapshots.Select(snapshot => new SteamWorkshopGameVersionRange
                {
                    MinimumBranch = snapshot.game_branch_min,
                    MaximumBranch = snapshot.game_branch_max,
                }).ToList(),
            },
            // AddDependency relates published item IDs, not payload revisions.
            // The selected package's native manifest owns code dependencies.
            RequiredPublishedFileIds = detail.children
                .Select(child => child.publishedfileid)
                .Where(id => id != 0 && id != detail.publishedfileid)
                .Distinct().OrderBy(id => id).ToArray(),
        };
    }

    internal static string BranchOrderProvenance(SteamWorkshopBranchOrder branchOrder)
        => branchOrder == null ? "branch-order-unavailable"
            : branchOrder.Provenance + (branchOrder.IsVerified ? ";order-verified" : ";order-unverified");

    private static bool IsCompatible(PublishedFileAuthorSnapshot snapshot, string branch, SteamWorkshopBranchOrder branchOrder)
    {
        var minimum = snapshot.game_branch_min;
        var maximum = snapshot.game_branch_max;
        if (string.IsNullOrEmpty(minimum) && string.IsNullOrEmpty(maximum))
            return true;
        if (!string.IsNullOrEmpty(minimum) && string.Equals(minimum, maximum, StringComparison.OrdinalIgnoreCase))
            return string.Equals(minimum, branch, StringComparison.OrdinalIgnoreCase);
        if (branchOrder?.IsVerified != true || string.IsNullOrWhiteSpace(branchOrder.Provenance))
            throw new InvalidOperationException($"Steam Workshop branch order is unverified for range '{minimum}' to '{maximum}'");
        var ordered = branchOrder.Branches.ToList();
        if (ordered.Count != ordered.Distinct(StringComparer.OrdinalIgnoreCase).Count())
            throw new InvalidOperationException("Steam Workshop branch order contains ambiguous duplicate names");
        var currentIndex = ordered.FindIndex(name => string.Equals(name, branch, StringComparison.OrdinalIgnoreCase));
        var minimumIndex = string.IsNullOrEmpty(minimum) ? 0
            : ordered.FindIndex(name => string.Equals(name, minimum, StringComparison.OrdinalIgnoreCase));
        var maximumIndex = string.IsNullOrEmpty(maximum) ? ordered.Count - 1
            : ordered.FindIndex(name => string.Equals(name, maximum, StringComparison.OrdinalIgnoreCase));
        if (currentIndex < 0 || minimumIndex < 0 || maximumIndex < 0 || minimumIndex > maximumIndex)
            throw new InvalidOperationException($"Steam Workshop branch range '{minimum}' to '{maximum}' has unknown bounds or order for '{branch}'");
        return currentIndex >= minimumIndex && currentIndex <= maximumIndex;
    }

    private static bool SameSnapshots(IReadOnlyList<PublishedFileAuthorSnapshot> first, IReadOnlyList<PublishedFileAuthorSnapshot> second)
        => first.Count == second.Count && first.Select((snapshot, index) =>
            snapshot.manifestid == second[index].manifestid
            && snapshot.timestamp == second[index].timestamp
            && string.Equals(snapshot.game_branch_min, second[index].game_branch_min, StringComparison.Ordinal)
            && string.Equals(snapshot.game_branch_max, second[index].game_branch_max, StringComparison.Ordinal)).All(equal => equal);

    internal static bool MatchesInstallation(SteamWorkshopInstallation installed, SteamWorkshopPreparedRevision revision)
        => installed != null && !installed.LegacyUnverified
            && (revision.ManifestId != 0
                ? installed.ManifestId == revision.ManifestId
                : installed.ManifestId == 0 && revision.HContentFile != 0 && installed.HContentFile == revision.HContentFile);
}

internal sealed class SteamWorkshopPreparedRevision
{
    internal ulong ManifestId { get; init; }
    internal ulong HContentFile { get; init; }
    internal long TimeUpdated { get; init; }
    internal string SelectedGameBranch { get; init; } = "";
    internal string Provenance { get; init; } = "";
    internal SteamWorkshopBranchSupport BranchSupport { get; init; }
    internal IReadOnlyCollection<ulong> RequiredPublishedFileIds { get; init; } = Array.Empty<ulong>();
}

internal sealed class SteamWorkshopBranchOrder
{
    internal SteamWorkshopBranchOrder(IReadOnlyList<string> branches, string provenance, bool isVerified)
    {
        Branches = branches?.ToArray() ?? Array.Empty<string>();
        Provenance = provenance ?? "";
        IsVerified = isVerified;
    }
    internal IReadOnlyList<string> Branches { get; }
    internal string Provenance { get; }
    internal bool IsVerified { get; }
}
