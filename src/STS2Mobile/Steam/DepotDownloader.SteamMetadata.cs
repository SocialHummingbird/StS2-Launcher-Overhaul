using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SteamKit2;
using PICSProductInfo = SteamKit2.SteamApps.PICSProductInfoCallback.PICSProductInfo;
using System.Collections.Concurrent;

namespace STS2Mobile.Steam;
internal sealed partial class DepotDownloader
{
    private readonly ProductInfoAppCache _productInfoAppCache = new();
    private BranchAvailabilityReport _lastBranchAvailability;
    private async Task<List<DepotManifestReference>> PrepareAndGetMainAppDepotsAsync(bool requireAny)
    {
        _stateStore.Prepare();
        var depots = await GetMainAppDepotsAsync();
        if (depots.Count == 0)
        {
            if (requireAny || !IsPublicBranch)
                throw new Exception(NoAccessibleDepotsMessage());
        }

        return depots;
    }

    private async Task<List<DepotManifestReference>> GetMainAppDepotsAsync()
    {
        var depotSection = await ProductInfoApp.GetMainDepotsSectionAsync(this);
        _lastBranchAvailability = BranchAvailabilityReport.FromDepots(depotSection, _branch);
        Log(_lastBranchAvailability.LogSummary());
        _lastBranchAvailability.WriteMarker(_dataDir);
        return await ParseDepotsAsync(depotSection);
    }

    private bool IsPublicBranch => string.Equals(_branch, SteamGameBranch.Public, StringComparison.OrdinalIgnoreCase);

    private string NoAccessibleDepotsMessage() => IsPublicBranch ? "No downloadable depots found" : $"No downloadable depots found for Steam branch '{_branch}'. " + "The branch may not exist, may be unavailable to this Steam account, " + "or may require a Steam beta password. Beta password entry is not implemented yet. " + (_lastBranchAvailability?.FailureSummary() ?? "Visible branch information was unavailable.");
    private readonly partial struct ProductInfoApp
    {
        private readonly DepotDownloader _owner;
        private readonly struct ProductInfoDepots
        {
            private ProductInfoDepots(KeyValue depots)
            {
                Depots = depots;
            }

            private KeyValue Depots { get; }

            internal static KeyValue RequiredSection(ProductInfoApp app, PICSProductInfo? appInfo) => Required(app, appInfo).Depots;
            internal static KeyValue TryGetManifestSection(ProductInfoApp app, PICSProductInfo? appInfo, uint depotId)
            {
                if (appInfo == null)
                    return KeyValue.Invalid;
                return Required(app, appInfo).ManifestSection(depotId);
            }

            private static ProductInfoDepots Required(ProductInfoApp app, PICSProductInfo? appInfo)
            {
                if (appInfo == null)
                    throw new System.Exception(app.AppInfoUnavailable());
                var depots = appInfo.KeyValues?["depots"];
                if (depots == null || depots == KeyValue.Invalid)
                    throw new System.InvalidOperationException(app.MissingDepotsSection());
                return new ProductInfoDepots(depots);
            }

            private KeyValue ManifestSection(uint depotId)
            {
                var depot = Depots[depotId.ToString()];
                return depot != KeyValue.Invalid ? depot["manifests"] : KeyValue.Invalid;
            }
        }

        private ProductInfoApp(DepotDownloader owner, uint appId)
        {
            _owner = owner;
            AppId = appId;
        }

        private uint AppId { get; }

        private static ProductInfoApp Main(DepotDownloader owner) => new(owner, SteamGameApp.AppId);
        private static ProductInfoApp Referenced(DepotDownloader owner, uint appId) => new(owner, appId);
        internal static Task<KeyValue> GetMainDepotsSectionAsync(DepotDownloader owner) => Main(owner).GetRequiredDepotsSectionAsync();
        internal static Task<KeyValue> TryGetReferencedManifestSectionAsync(DepotDownloader owner, uint appId, uint depotId) => Referenced(owner, appId).TryGetManifestSectionAsync(depotId);
        private async Task<KeyValue> GetRequiredDepotsSectionAsync()
        {
            var appInfo = await GetInfoAsync();
            return ProductInfoDepots.RequiredSection(this, appInfo);
        }

        private async Task<KeyValue> TryGetManifestSectionAsync(uint depotId)
        {
            var appInfo = await GetInfoAsync();
            return ProductInfoDepots.TryGetManifestSection(this, appInfo, depotId);
        }

        private readonly struct ProductInfoFetchRequest
        {
            internal ProductInfoFetchRequest(ProductInfoApp app)
            {
                App = app;
            }

            private ProductInfoApp App { get; }

            internal async Task<PICSProductInfo?> FetchAsync() => await App._owner._connection.GetAppInfoAsync(App.AppId, await GetAccessTokenAsync());
            private async Task<ulong> GetAccessTokenAsync()
            {
                var token = await App._owner._connection.GetAppAccessTokenOrPublicAsync(App.AppId, App.AccessTokenDenied());
                if (token == 0)
                    App._owner.Log(App.PublicAccessTokenFallback());
                return token;
            }
        }

        private async Task<PICSProductInfo?> GetInfoAsync()
        {
            var request = new ProductInfoFetchRequest(this);
            return await _owner._productInfoAppCache.GetOrFetchAsync(AppId, request.FetchAsync);
        }

        private bool IsMainApp => AppId == SteamGameApp.AppId;
        private string Name => IsMainApp ? $"{SteamGameApp.Name} ({SteamGameApp.AppId})" : $"referenced app {AppId}";
        private string OwnershipHint => IsMainApp ? "; ownership/session may be invalid" : "";

        private string AccessTokenDenied() => $"Steam denied app access token for {Name}{OwnershipHint}";
        private string PublicAccessTokenFallback() => $"Steam returned no app access token for {Name}; " + "continuing with public token 0";
        private string AppInfoUnavailable() => $"Failed to get app info from Steam for {Name}{OwnershipHint}";
        private string MissingDepotsSection() => $"Steam app info for {Name} has no depots section";
    }

    private readonly struct DepotManifestReference
    {
        internal DepotManifestReference(uint depotId, ulong manifestId, string branch, ulong? selectedBranchManifestId, ulong? publicManifestId, string manifestSource, string manifestRequestBranch)
        {
            DepotId = depotId;
            ManifestId = manifestId;
            Branch = SteamGameBranch.Normalize(branch);
            SelectedBranchManifestId = selectedBranchManifestId;
            PublicManifestId = publicManifestId;
            ManifestSource = string.IsNullOrWhiteSpace(manifestSource) ? "unknown" : manifestSource;
            ManifestRequestBranch = SteamGameBranch.Normalize(manifestRequestBranch);
        }

        internal uint DepotId { get; }
        internal ulong ManifestId { get; }
        internal string Branch { get; }
        internal ulong? SelectedBranchManifestId { get; }
        internal ulong? PublicManifestId { get; }
        internal string ManifestSource { get; }
        internal string ManifestRequestBranch { get; }
        internal bool HasSelectedBranchManifest => SelectedBranchManifestId.HasValue;
        internal bool HasPublicManifest => PublicManifestId.HasValue;
        internal bool EffectiveMatchesPublicManifest => PublicManifestId.HasValue && PublicManifestId.Value == ManifestId;
        internal bool SelectedBranchManifestMatchesPublic => SelectedBranchManifestId.HasValue && PublicManifestId.HasValue && SelectedBranchManifestId.Value == PublicManifestId.Value;
        internal bool InheritedFromPublic => string.Equals(ManifestSource, "public-inherited", System.StringComparison.OrdinalIgnoreCase);
    }

    private readonly struct DepotManifestEvidence
    {
        internal DepotManifestEvidence(ulong? selectedManifestId, ulong? publicManifestId)
        {
            SelectedManifestId = selectedManifestId;
            PublicManifestId = publicManifestId;
        }

        internal ulong? SelectedManifestId { get; }
        internal ulong? PublicManifestId { get; }
    }

    private readonly struct DepotManifestSource
    {
        private DepotManifestSource(DepotDownloader owner, KeyValue depot, uint depotId)
        {
            Owner = owner;
            Depot = depot;
            DepotId = depotId;
        }

        private DepotDownloader Owner { get; }
        private KeyValue Depot { get; }
        private uint DepotId { get; }

        internal static Task<ulong?> GetSelectedManifestIdAsync(DepotDownloader owner, KeyValue depot, uint depotId) => new DepotManifestSource(owner, depot, depotId).GetSelectedManifestIdAsync();
        internal static Task<DepotManifestEvidence> GetManifestEvidenceAsync(DepotDownloader owner, KeyValue depot, uint depotId) => new DepotManifestSource(owner, depot, depotId).GetManifestEvidenceAsync();
        private async Task<ulong?> GetSelectedManifestIdAsync() => (await GetManifestEvidenceAsync()).SelectedManifestId;
        private async Task<DepotManifestEvidence> GetManifestEvidenceAsync()
        {
            var manifests = await GetManifestSectionAsync();
            if (manifests == KeyValue.Invalid)
                return new DepotManifestEvidence(null, null);
            var manifestId = DepotDownloader.ReadKeyValueUInt64(manifests[Owner._branch]["gid"]);
            var publicManifestId = DepotDownloader.ReadKeyValueUInt64(manifests[SteamGameBranch.Public]["gid"]);
            if (!manifestId.HasValue)
                Owner.Log($"Depot {DepotId} has no manifest for branch '{Owner._branch}'");
            return new DepotManifestEvidence(manifestId, publicManifestId);
        }

        private async Task<KeyValue> GetManifestSectionAsync()
        {
            var manifests = Depot["manifests"];
            if (manifests != KeyValue.Invalid)
                return manifests;
            var otherAppId = DepotDownloader.ReadKeyValueUInt32(Depot["depotfromapp"]);
            return otherAppId.HasValue ? await GetReferencedManifestSectionAsync(otherAppId.Value) : KeyValue.Invalid;
        }

        private async Task<KeyValue> GetReferencedManifestSectionAsync(uint appId)
        {
            Owner.Log($"Depot {DepotId} references app {appId}, fetching...");
            return await ProductInfoApp.TryGetReferencedManifestSectionAsync(Owner, appId, DepotId);
        }
    }

    private readonly struct DepotReferenceCandidate
    {
        private DepotReferenceCandidate(DepotDownloader owner, KeyValue depot, uint depotId)
        {
            Owner = owner;
            Depot = depot;
            DepotId = depotId;
        }

        private DepotDownloader Owner { get; }
        private KeyValue Depot { get; }
        private uint DepotId { get; }

        internal static DepotReferenceCandidate? TryCreate(DepotDownloader owner, KeyValue depot) => uint.TryParse(depot.Name, out var depotId) ? new DepotReferenceCandidate(owner, depot, depotId) : null;
        internal async Task<DepotManifestReference?> TryCreateReferenceAsync()
        {
            if (ShouldSkip())
                return null;
            var manifest = await DepotManifestSource.GetManifestEvidenceAsync(Owner, Depot, DepotId);
            var effectiveManifestId = manifest.SelectedManifestId;
            var manifestSource = "selected";
            var manifestRequestBranch = Owner._branch;
            if (!effectiveManifestId.HasValue && !string.Equals(Owner._branch, SteamGameBranch.Public, System.StringComparison.OrdinalIgnoreCase) && manifest.PublicManifestId.HasValue)
            {
                effectiveManifestId = manifest.PublicManifestId;
                manifestSource = "public-inherited";
                manifestRequestBranch = SteamGameBranch.Public;
                Owner.Log($"Depot {DepotId} branch '{Owner._branch}' has no explicit branch manifest; inheriting public manifest {effectiveManifestId.Value}");
            }

            if (!effectiveManifestId.HasValue)
                return null;
            Owner.Log($"Found depot {DepotId} manifest {effectiveManifestId.Value} for branch '{Owner._branch}' source={manifestSource} requestBranch='{manifestRequestBranch}'");
            if (!string.Equals(Owner._branch, SteamGameBranch.Public, System.StringComparison.OrdinalIgnoreCase))
            {
                if (!manifest.PublicManifestId.HasValue)
                {
                    Owner.Log($"Depot {DepotId} has no public manifest to compare with branch '{Owner._branch}'");
                }
                else if (manifest.PublicManifestId.Value == effectiveManifestId.Value)
                {
                    Owner.Log($"Depot {DepotId} branch '{Owner._branch}' uses the same effective manifest as public ({effectiveManifestId.Value}) source={manifestSource}");
                }
                else
                {
                    Owner.Log($"Depot {DepotId} branch '{Owner._branch}' differs from public: effective={effectiveManifestId.Value} public={manifest.PublicManifestId.Value} source={manifestSource}");
                }
            }

            return new DepotManifestReference(DepotId, effectiveManifestId.Value, Owner._branch, manifest.SelectedManifestId, manifest.PublicManifestId, manifestSource, manifestRequestBranch);
        }

        private bool ShouldSkip()
        {
            var config = Depot["config"];
            if (config == KeyValue.Invalid)
                return false;
            var oslist = config["oslist"]?.Value;
            if (string.IsNullOrEmpty(oslist) || oslist.Contains("windows"))
                return false;
            Owner.Log($"Skipping depot {DepotId} (OS: {oslist})");
            return true;
        }
    }

    private async Task<List<DepotManifestReference>> ParseDepotsAsync(KeyValue depotSection)
    {
        var result = new List<DepotManifestReference>();
        foreach (var depot in depotSection.Children)
        {
            var candidate = DepotReferenceCandidate.TryCreate(this, depot);
            if (!candidate.HasValue)
                continue;
            var reference = await candidate.Value.TryCreateReferenceAsync();
            if (reference.HasValue)
                result.Add(reference.Value);
        }

        return result;
    }

    private static uint? ReadKeyValueUInt32(KeyValue value) => value != KeyValue.Invalid && value.Value != null && uint.TryParse(value.Value, out var parsed) ? parsed : null;
    private static ulong? ReadKeyValueUInt64(KeyValue value) => value != KeyValue.Invalid && value.Value != null && ulong.TryParse(value.Value, out var parsed) ? parsed : null;
    private sealed class ProductInfoAppCache
    {
        private readonly ConcurrentDictionary<uint, PICSProductInfo> _cache = new();
        internal async Task<PICSProductInfo?> GetOrFetchAsync(uint appId, Func<Task<PICSProductInfo?>> fetchAsync)
        {
            if (_cache.TryGetValue(appId, out var cached))
                return cached;
            var appInfo = await fetchAsync();
            if (appInfo != null)
                _cache[appId] = appInfo;
            return appInfo;
        }
    }
}
