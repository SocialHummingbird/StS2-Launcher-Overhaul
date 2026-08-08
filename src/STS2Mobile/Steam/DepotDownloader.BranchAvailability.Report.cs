using System;
using System.Collections.Generic;
using System.Linq;
using SteamKit2;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private sealed partial class BranchAvailabilityReport
    {
        private readonly List<BranchAvailability> _branches;

        private BranchAvailabilityReport(string selectedBranch, List<BranchAvailability> branches)
        {
            SelectedBranch = SteamGameBranch.Normalize(selectedBranch);
            _branches = branches
                .OrderByDescending(branch => branch.Downloadable)
                .ThenBy(branch => string.Equals(branch.Name, SteamGameBranch.Public, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .ThenBy(branch => branch.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        internal string SelectedBranch { get; }

        internal BranchAvailability SelectedBranchAvailability
        {
            get
            {
                var branch = _branches.FirstOrDefault(
                    candidate => string.Equals(candidate.Name, SelectedBranch, StringComparison.OrdinalIgnoreCase)
                );
                return string.IsNullOrWhiteSpace(branch.Name)
                    ? BranchAvailability.Missing(SelectedBranch)
                    : branch;
            }
        }

        internal static BranchAvailabilityReport FromDepots(KeyValue depotSection, string selectedBranch)
        {
            var byName = new Dictionary<string, BranchAvailabilityBuilder>(StringComparer.OrdinalIgnoreCase);

            foreach (var branch in BranchMetadataChildren(depotSection))
            {
                if (string.IsNullOrWhiteSpace(branch.Name))
                    continue;

                GetOrCreate(byName, branch.Name).ApplyMetadata(branch);
            }

            foreach (var depot in depotSection.Children)
            {
                if (!uint.TryParse(depot.Name, out _))
                    continue;

                if (!DepotIsWindowsCompatible(depot))
                    continue;

                var manifests = depot["manifests"];
                if (manifests == KeyValue.Invalid)
                    continue;

                foreach (var manifestBranch in manifests.Children)
                {
                    if (string.IsNullOrWhiteSpace(manifestBranch.Name))
                        continue;

                    var manifestId = ReadKeyValueUInt64(manifestBranch["gid"]);
                    GetOrCreate(byName, manifestBranch.Name).ApplyManifest(manifestId);
                }
            }

            if (!byName.ContainsKey(SteamGameBranch.Public))
                byName[SteamGameBranch.Public] = new BranchAvailabilityBuilder(SteamGameBranch.Public);

            return new BranchAvailabilityReport(
                selectedBranch,
                byName.Values.Select(builder => builder.Build()).ToList()
            );
        }

        internal string LogSummary()
            => _branches.Count == 0
                ? "Visible Steam branches: <none returned>"
                : "Visible Steam branches: " + string.Join("; ", _branches.Select(branch => branch.Summary()));

        internal string FailureSummary()
        {
            var selected = SelectedBranchAvailability;
            var visible = _branches.Count == 0
                ? "<none returned>"
                : string.Join(
                    ", ",
                    _branches.Select(branch => $"{branch.Name} ({branch.DownloadabilityText})")
                );

            return $"{SteamBranchAvailabilityMarkerFields.SelectedBranchVisibility} {selected.VisibilityText}; "
                + $"{SteamBranchAvailabilityMarkerFields.SelectedBranchWindowsDepotManifests} {selected.WindowsManifestDepotCount}; "
                + $"visible branches: {visible}.";
        }

        private static IEnumerable<KeyValue> BranchMetadataChildren(KeyValue depotSection)
        {
            var branches = depotSection["branches"];
            return branches == KeyValue.Invalid ? Array.Empty<KeyValue>() : branches.Children;
        }

        private static BranchAvailabilityBuilder GetOrCreate(
            Dictionary<string, BranchAvailabilityBuilder> byName,
            string branchName
        )
        {
            branchName = SteamGameBranch.Normalize(branchName);
            if (!byName.TryGetValue(branchName, out var builder))
            {
                builder = new BranchAvailabilityBuilder(branchName);
                byName[branchName] = builder;
            }

            return builder;
        }

        private static bool DepotIsWindowsCompatible(KeyValue depot)
        {
            var config = depot["config"];
            if (config == KeyValue.Invalid)
                return true;

            var oslist = config["oslist"]?.Value;
            return string.IsNullOrEmpty(oslist)
                || oslist.Contains("windows", StringComparison.OrdinalIgnoreCase);
        }
    }
}
