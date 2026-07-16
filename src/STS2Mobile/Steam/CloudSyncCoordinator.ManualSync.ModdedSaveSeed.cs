using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private sealed class ModdedSaveSeedEvidence
    {
        public int SchemaVersion { get; init; } = 1;
        public string Policy { get; init; } = "upstream-v0.108-first-modded-copy-set";
        public string StartedAtUtc { get; init; } = DateTime.UtcNow.ToString("O");
        public string CompletedAtUtc { get; set; } = "";
        public bool SteamCloudPushPerformed { get; init; }
        public List<string> CloudAuthoritativeNamespaces { get; init; } = new();
        public List<string> ExpectedDirectCloudModdedPaths { get; init; } = new();
        public List<string> DownloadedCloudPaths { get; init; } = new();
        public List<ModdedSaveSeedCopyEvidence> SeededCopies { get; init; } = new();
        public List<string> PrivateBackupPaths { get; init; } = new();
        public List<string> SkippedSeedSources { get; init; } = new();
        public List<string> Errors { get; init; } = new();
    }

    private sealed class ModdedSaveSeedCopyEvidence
    {
        public string SourcePath { get; init; } = "";
        public string TargetPath { get; init; } = "";
        public string SaveNamespace { get; init; } = "";
        public int Characters { get; init; }
    }

    private sealed class ModdedSaveSeedSession
    {
        private static readonly JsonSerializerOptions EvidenceJsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
        };

        private ModdedSaveSeedSession(
            ModdedSaveSeedPlan plan,
            ModdedSaveSeedEvidence evidence
        )
        {
            Plan = plan;
            Evidence = evidence;
        }

        private ModdedSaveSeedPlan Plan { get; }
        private ModdedSaveSeedEvidence Evidence { get; }

        internal static async Task<ModdedSaveSeedSession> PrepareAsync(
            ManualSyncContext sync,
            IEnumerable<string> cloudPaths
        )
        {
            var plan = ModdedSaveSeedPlan.Create(
                cloudPaths.Where(sync.CloudFileExists)
            );
            var evidence = new ModdedSaveSeedEvidence
            {
                CloudAuthoritativeNamespaces = plan.CloudAuthoritativeNamespaces.ToList(),
                ExpectedDirectCloudModdedPaths = plan.DirectCloudModdedTargets.ToList(),
            };
            var session = new ModdedSaveSeedSession(plan, evidence);

            try
            {
                await session.BackUpLocalOverwriteTargetsAsync(sync);
                return session;
            }
            catch (Exception ex)
            {
                evidence.Errors.Add($"backup-preflight: {ex.Message}");
                await session.WriteEvidenceAsync(sync);
                throw new InvalidOperationException(
                    "Manual Pull blocked before downloads because an existing local modded save could not be backed up.",
                    ex
                );
            }
        }

        internal async Task<string> CompleteAsync(
            ManualSyncContext sync,
            IReadOnlyDictionary<string, string> downloadedCloudContent
        )
        {
            Evidence.DownloadedCloudPaths.AddRange(
                downloadedCloudContent.Keys.OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            );

            try
            {
                foreach (var mapping in Plan.SeedMappings)
                {
                    if (!downloadedCloudContent.TryGetValue(mapping.SourcePath, out var content))
                    {
                        Evidence.SkippedSeedSources.Add(mapping.SourcePath);
                        continue;
                    }

                    await WriteAndVerifyLocalAsync(sync, mapping.TargetPath, content);
                    Evidence.SeededCopies.Add(new ModdedSaveSeedCopyEvidence
                    {
                        SourcePath = mapping.SourcePath,
                        TargetPath = mapping.TargetPath,
                        SaveNamespace = mapping.SaveNamespace,
                        Characters = content.Length,
                    });
                    PatchHelper.Log(
                        $"[Cloud] Manual Pull seeded modded save {mapping.TargetPath} from freshly downloaded {mapping.SourcePath} ({content.Length} characters)"
                    );
                }
            }
            catch (Exception ex)
            {
                Evidence.Errors.Add($"seed-write: {ex.Message}");
                await WriteEvidenceAsync(sync);
                throw;
            }

            await WriteEvidenceAsync(sync);
            return $"Modded save handling: {Evidence.SeededCopies.Count} file(s) seeded from fresh vanilla cloud data, "
                + $"{Evidence.CloudAuthoritativeNamespaces.Count} cloud modded namespace(s) preferred, "
                + $"{Evidence.PrivateBackupPaths.Count} existing local modded file(s) backed up privately. "
                + "Steam Cloud Push was not run.";
        }

        private async Task BackUpLocalOverwriteTargetsAsync(ManualSyncContext sync)
        {
            var backupStamp = DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffZ");
            foreach (var targetPath in Plan.LocalOverwriteTargets)
            {
                var content = sync.ReadLocalFile(targetPath);
                if (content == null)
                    continue;

                var backupPath = $"{AppPaths.ManualPullPrivateBackupRelativeDirectory}/{backupStamp}/{targetPath}";
                await WriteAndVerifyLocalAsync(sync, backupPath, content);
                Evidence.PrivateBackupPaths.Add(backupPath);
                PatchHelper.Log(
                    $"[Cloud] Manual Pull private backup: {targetPath} -> {backupPath} ({content.Length} characters)"
                );
            }
        }

        private async Task WriteEvidenceAsync(ManualSyncContext sync)
        {
            Evidence.CompletedAtUtc = DateTime.UtcNow.ToString("O");
            var json = JsonSerializer.Serialize(Evidence, EvidenceJsonOptions);
            await WriteAndVerifyLocalAsync(
                sync,
                AppPaths.ManualPullModdedSaveSeedEvidenceRelativePath,
                json
            );
            PatchHelper.Log(
                $"[Cloud] Manual Pull modded-save provenance written: {AppPaths.ManualPullModdedSaveSeedEvidenceRelativePath}"
            );
        }

        private static async Task WriteAndVerifyLocalAsync(
            ManualSyncContext sync,
            string path,
            string content
        )
        {
            await sync.WriteLocalContentAsync(path, content);
            var verified = sync.ReadLocalFile(path);
            if (!string.Equals(content, verified, StringComparison.Ordinal))
                throw new InvalidOperationException($"Local write verification failed for {path}.");
        }
    }
}
