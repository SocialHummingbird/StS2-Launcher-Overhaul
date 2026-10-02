using System;
using System.Threading.Tasks;
using SteamKit2;

namespace STS2Mobile.Steam;
internal sealed partial class DepotDownloader
{
    private readonly struct ManifestDownloadRequest
    {
        internal ManifestDownloadRequest(uint depotId, ulong manifestId, ulong requestCode, byte[] depotKey)
        {
            DepotId = depotId;
            ManifestId = manifestId;
            RequestCode = requestCode;
            DepotKey = depotKey;
        }

        internal uint DepotId { get; }
        private ulong ManifestId { get; }
        private ulong RequestCode { get; }
        private byte[] DepotKey { get; }

        internal string DownloadLogMessage() => $"Downloading manifest for depot {DepotId}...";
        internal string FailureMessage() => $"Failed to download manifest for depot {DepotId} after {MaxRetries} attempts";
        internal Task<DepotManifest> DownloadAsync(DepotDownloader owner, CdnServerAttempt attempt, string? cdnAuthToken = null) => attempt.DownloadManifestAsync(owner, DepotId, ManifestId, RequestCode, DepotKey, cdnAuthToken);
    }

    private Task<DepotManifest> DownloadManifestWithRetriesAsync(ManifestDownloadRequest request)
    {
        Log(request.DownloadLogMessage());
        return RunCdnDownloadWithRetriesAsync(CreateManifestDownloadOperation(request));
    }

    private CdnDownloadOperation<DepotManifest> CreateManifestDownloadOperation(ManifestDownloadRequest request) => CdnDownloadOperation<DepotManifest>.AcrossServersWithAuthRetry(CdnManifestDownloadOperation, attempt => CdnDownloadResult<DepotManifest>.FromAsync(() => request.DownloadAsync(this, attempt)), attempt => RunCdnAuthRetryAsync(new CdnAuthRetry<DepotManifest>(request.DepotId, attempt, CdnManifestAuthRetryOperation, token => CdnDownloadResult<DepotManifest>.FromAsync(() => request.DownloadAsync(this, attempt, token)))), () => new Exception(request.FailureMessage()));
    private static readonly TimeSpan ManifestRequestCodeTtl = TimeSpan.FromMinutes(5);
    private readonly ExpiringCache<ManifestRequestKey, ulong> _manifestRequestCodes = new();
    private async Task<ulong> GetManifestRequestCodeAsync(uint depotId, ulong manifestId, string branch)
    {
        var request = ManifestRequestKey.ForBranch(depotId, manifestId, branch);
        var code = await _manifestRequestCodes.GetOrAddAsync(request, ManifestRequestCodeTtl, () => request.FetchCodeAsync(_connection), code => code != 0);
        request.ThrowIfDenied(code);
        return code;
    }

    private readonly struct ManifestRequestKey : IEquatable<ManifestRequestKey>
    {
        private ManifestRequestKey(uint depotId, ulong manifestId, string branch)
        {
            DepotId = depotId;
            ManifestId = manifestId;
            Branch = branch;
        }

        private uint DepotId { get; }
        private ulong ManifestId { get; }
        private string Branch { get; }

        internal static ManifestRequestKey ForBranch(uint depotId, ulong manifestId, string branch) => new(depotId, manifestId, SteamGameBranch.Normalize(branch));
        internal Task<ulong> FetchCodeAsync(SteamConnection connection) => connection.GetManifestRequestCodeAsync(DepotId, ManifestId, Branch);
        internal void ThrowIfDenied(ulong code)
        {
            if (code != 0)
                return;
            throw new Exception($"Failed to get manifest request code for depot {DepotId}, " + $"manifest {ManifestId}, branch '{Branch}'. " + "Ensure the account owns this app.");
        }

        public bool Equals(ManifestRequestKey other) => DepotId == other.DepotId && ManifestId == other.ManifestId && Branch == other.Branch;
        public override bool Equals(object obj) => obj is ManifestRequestKey other && Equals(other);
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = hash * 31 + DepotId.GetHashCode();
                hash = hash * 31 + ManifestId.GetHashCode();
                hash = hash * 31 + Branch.GetHashCode();
                return hash;
            }
        }
    }
}
