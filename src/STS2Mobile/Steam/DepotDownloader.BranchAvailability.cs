using System;
using SteamKit2;
using System.IO;
using System.Linq;
using STS2Mobile.Patches;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;
internal sealed partial class DepotDownloader
{
    private const int MaxBranchAvailabilityMarkerBranches = 64;
    private const int MaxBranchAvailabilityMarkerValueLength = 256;
    private sealed class BranchAvailabilityBuilder
    {
        private int _windowsManifestDepotCount;
        private string _description = "";
        private string _buildId = "";
        private string _passwordRequired = "unknown";
        private bool _metadataVisible;
        internal BranchAvailabilityBuilder(string name)
        {
            Name = SteamGameBranch.Normalize(name);
        }

        private string Name { get; }

        internal void ApplyMetadata(KeyValue branch)
        {
            _metadataVisible = true;
            _description = FirstValue(branch, "description", "desc");
            _buildId = FirstValue(branch, "buildid", "build_id");
            _passwordRequired = BoolValue(branch, "pwdrequired", "passwordrequired", "password_required", "protected");
        }

        internal void ApplyManifest(ulong? manifestId)
        {
            if (manifestId.HasValue && manifestId.Value != 0)
                _windowsManifestDepotCount++;
        }

        internal BranchAvailability Build() => new(Name, _metadataVisible, _description, _buildId, _passwordRequired, _windowsManifestDepotCount);
        private static string FirstValue(KeyValue branch, params string[] keys)
        {
            foreach (var key in keys)
            {
                var value = branch[key];
                if (value != KeyValue.Invalid && !string.IsNullOrWhiteSpace(value.Value))
                    return value.Value.Trim();
            }

            return "";
        }

        private static string BoolValue(KeyValue branch, params string[] keys)
        {
            var value = FirstValue(branch, keys);
            if (string.IsNullOrWhiteSpace(value))
                return "unknown";
            return value.Equals("1", StringComparison.OrdinalIgnoreCase) || value.Equals("true", StringComparison.OrdinalIgnoreCase) || value.Equals("yes", StringComparison.OrdinalIgnoreCase) ? "true" : "false";
        }
    }

    private sealed partial class BranchAvailabilityReport
    {
        internal void WriteMarker(string dataDir)
        {
            var path = SteamGameInstallPaths.BranchAvailabilityMarkerPath(dataDir);
            try
            {
                var text = $"{SteamBranchAvailabilityMarkerFields.Utc} {DateTime.UtcNow:O}\n" + $"{SteamBranchAvailabilityMarkerFields.SelectedBranch} {SelectedBranch}\n" + $"{SteamBranchAvailabilityMarkerFields.SelectedBranchVisibility} {SelectedBranchAvailability.VisibilityText}\n" + $"{SteamBranchAvailabilityMarkerFields.SelectedBranchWindowsDepotManifests} {SelectedBranchAvailability.WindowsManifestDepotCount}\n" + $"{SteamBranchAvailabilityMarkerFields.VisibleBranchCount} {_branches.Count}\n";
                foreach (var branch in _branches.Take(MaxBranchAvailabilityMarkerBranches))
                    text += $"{SteamBranchAvailabilityMarkerFields.VisibleBranch} {branch.Summary()}\n";
                if (_branches.Count > MaxBranchAvailabilityMarkerBranches)
                    text += $"{SteamBranchAvailabilityMarkerFields.VisibleBranchOverflowCount} {_branches.Count - MaxBranchAvailabilityMarkerBranches}\n";
                Directory.CreateDirectory(Path.GetDirectoryName(path) ?? dataDir);
                File.WriteAllText(path, text);
            }
            catch (Exception ex)
            {
                PatchHelper.Log($"[Depot] Failed to write Steam branch availability marker: {ex.Message}");
            }
        }

        private readonly List<BranchAvailability> _branches;
        private BranchAvailabilityReport(string selectedBranch, List<BranchAvailability> branches)
        {
            SelectedBranch = SteamGameBranch.Normalize(selectedBranch);
            _branches = branches.OrderByDescending(branch => branch.Downloadable).ThenBy(branch => string.Equals(branch.Name, SteamGameBranch.Public, StringComparison.OrdinalIgnoreCase) ? 0 : 1).ThenBy(branch => branch.Name, StringComparer.OrdinalIgnoreCase).ToList();
        }

        internal string SelectedBranch { get; }

        internal BranchAvailability SelectedBranchAvailability
        {
            get
            {
                var branch = _branches.FirstOrDefault(candidate => string.Equals(candidate.Name, SelectedBranch, StringComparison.OrdinalIgnoreCase));
                return string.IsNullOrWhiteSpace(branch.Name) ? BranchAvailability.Missing(SelectedBranch) : branch;
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
            return new BranchAvailabilityReport(selectedBranch, byName.Values.Select(builder => builder.Build()).ToList());
        }

        internal string LogSummary() => _branches.Count == 0 ? "Visible Steam branches: <none returned>" : "Visible Steam branches: " + string.Join("; ", _branches.Select(branch => branch.Summary()));
        internal string FailureSummary()
        {
            var selected = SelectedBranchAvailability;
            var visible = _branches.Count == 0 ? "<none returned>" : string.Join(", ", _branches.Select(branch => $"{branch.Name} ({branch.DownloadabilityText})"));
            return $"{SteamBranchAvailabilityMarkerFields.SelectedBranchVisibility} {selected.VisibilityText}; " + $"{SteamBranchAvailabilityMarkerFields.SelectedBranchWindowsDepotManifests} {selected.WindowsManifestDepotCount}; " + $"visible branches: {visible}.";
        }

        private static IEnumerable<KeyValue> BranchMetadataChildren(KeyValue depotSection)
        {
            var branches = depotSection["branches"];
            return branches == KeyValue.Invalid ? Array.Empty<KeyValue>() : branches.Children;
        }

        private static BranchAvailabilityBuilder GetOrCreate(Dictionary<string, BranchAvailabilityBuilder> byName, string branchName)
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
            return string.IsNullOrEmpty(oslist) || oslist.Contains("windows", StringComparison.OrdinalIgnoreCase);
        }
    }

    private readonly struct BranchAvailability
    {
        internal BranchAvailability(string name, bool metadataVisible, string description, string buildId, string passwordRequired, int windowsManifestDepotCount)
        {
            Name = SteamGameBranch.Normalize(name);
            MetadataVisible = metadataVisible;
            Description = description ?? "";
            BuildId = buildId ?? "";
            PasswordRequired = passwordRequired ?? "unknown";
            WindowsManifestDepotCount = windowsManifestDepotCount;
        }

        internal string Name { get; }
        private bool MetadataVisible { get; }
        private string Description { get; }
        private string BuildId { get; }
        private string PasswordRequired { get; }
        internal int WindowsManifestDepotCount { get; }
        internal bool Downloadable => WindowsManifestDepotCount > 0 && !PasswordRequired.Equals("true", StringComparison.OrdinalIgnoreCase);
        internal string DownloadabilityText => PasswordRequired.Equals("true", StringComparison.OrdinalIgnoreCase) ? "password-protected" : WindowsManifestDepotCount > 0 ? "downloadable" : "no Windows manifest";
        internal string VisibilityText => MetadataVisible ? "visible in Steam branch metadata" : "not listed in Steam branch metadata";

        internal static BranchAvailability Missing(string branch) => new(branch, false, "", "", "unknown", 0);
        internal string Summary()
        {
            var details = new List<string>
            {
                $"{SteamBranchAvailabilityMarkerFields.WindowsManifestDepotsKey}={WindowsManifestDepotCount}",
                $"{SteamBranchAvailabilityMarkerFields.MetadataVisibleKey}={MetadataVisible.ToString().ToLowerInvariant()}",
                $"{SteamBranchAvailabilityMarkerFields.PasswordRequiredKey}={SafeMarkerValue(PasswordRequired)}",
            };
            if (!string.IsNullOrWhiteSpace(BuildId))
                details.Add($"{SteamBranchAvailabilityMarkerFields.BuildIdKey}={SafeMarkerValue(BuildId)}");
            if (!string.IsNullOrWhiteSpace(Description))
                details.Add($"{SteamBranchAvailabilityMarkerFields.DescriptionKey}={SafeMarkerValue(Description)}");
            return $"{SafeMarkerValue(Name)} [{string.Join(", ", details)}]";
        }

        private static string SafeMarkerValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "";
            value = value.Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' ').Trim();
            while (value.Contains("  ", StringComparison.Ordinal))
                value = value.Replace("  ", " ", StringComparison.Ordinal);
            return value.Length <= MaxBranchAvailabilityMarkerValueLength ? value : value[..MaxBranchAvailabilityMarkerValueLength] + "...";
        }
    }

    // Returns true if any depot has a newer manifest than what's cached locally.
    internal Task<bool> CheckForUpdatesAsync(CancellationToken ct = default) => RunWithSuspendedIdleTimeoutAsync(() => CheckForUpdatesCoreAsync(ct));
    internal Task RefreshBranchCatalogAsync(CancellationToken ct = default) => RunWithSuspendedIdleTimeoutAsync(async () =>
    {
        ct.ThrowIfCancellationRequested();
        Log("Refreshing Steam branch catalog...");
        await GetMainAppDepotsAsync().ConfigureAwait(false);
        Log("Steam branch catalog refreshed.");
    });
    private async Task<bool> CheckForUpdatesCoreAsync(CancellationToken ct)
    {
        var depots = await PrepareAndGetMainAppDepotsAsync(requireAny: true);
        foreach (var depot in depots)
        {
            ct.ThrowIfCancellationRequested();
            if (_stateStore.ManifestChanged(depot))
            {
                Log($"Update available: depot {depot.DepotId} manifest changed");
                return true;
            }
        }

        Log("Game is up to date");
        return false;
    }
}
