using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace STS2Mobile.Patches
{
    internal static class PatchHelper
    {
        internal static readonly ConcurrentQueue<string> Messages = new();

        internal static void Log(string message)
            => Messages.Enqueue(message);
    }
}

namespace STS2Mobile.Steam
{
    internal static class SteamGameBranch
    {
        internal static string Normalize(string branch)
            => string.IsNullOrWhiteSpace(branch)
                ? "public"
                : branch.Trim();

        internal static string DisplayName(string branch)
            => string.IsNullOrWhiteSpace(branch) ? "public" : branch;
    }
}

namespace STS2Mobile
{
    internal static class AppPaths
    {
        internal static bool StoragePermissionAvailable = true;
        internal static int EnsureExternalDirectoriesCalls;
        internal static int RequestStoragePermissionCalls;

        internal static bool HasStoragePermission()
            => StoragePermissionAvailable;

        internal static void EnsureExternalDirectories()
            => EnsureExternalDirectoriesCalls++;

        internal static void RequestStoragePermission()
            => RequestStoragePermissionCalls++;

        internal static void Reset()
        {
            StoragePermissionAvailable = true;
            EnsureExternalDirectoriesCalls = 0;
            RequestStoragePermissionCalls = 0;
        }
    }
}

namespace STS2Mobile.Launcher
{
    internal sealed class LauncherModel
    {
        internal LauncherModel(string dataDir)
        {
            DataDir = dataDir;
        }

        internal string DataDir { get; }
        internal int RefreshCloudSaveCredentialsCalls { get; private set; }

        internal void RefreshCloudSaveCredentials()
            => RefreshCloudSaveCredentialsCalls++;
    }

    internal sealed class LauncherView
    {
        private readonly object _stateLock = new();
        private bool _pushPullDisabled;

        internal readonly ConcurrentQueue<string> StatusMessages = new();
        internal readonly ConcurrentQueue<string> LogMessages = new();
        internal readonly ConcurrentQueue<bool> PushPullDisabledChanges = new();
        internal readonly ConcurrentQueue<string> UiEvents = new();
        internal readonly ConcurrentQueue<
            CloudPostOperationSnapshot
        > PostOperationSnapshots = new();
        internal readonly ConcurrentQueue<
            STS2Mobile.Steam.CloudOperationState
        > CloudOperationStates = new();

        internal int ConfirmationCount { get; private set; }
        internal Action? PendingConfirmation { get; private set; }

        internal bool PushPullDisabled
        {
            get
            {
                lock (_stateLock)
                    return _pushPullDisabled;
            }
        }

        internal void SetPushPullDisabled(bool disabled)
        {
            lock (_stateLock)
                _pushPullDisabled = disabled;
            PushPullDisabledChanges.Enqueue(disabled);
            UiEvents.Enqueue($"disabled:{disabled}");
        }

        internal void RefreshCloudPushEligibility()
        {
        }

        internal void SetCloudOperationState(
            STS2Mobile.Steam.CloudOperationState state
        )
            => CloudOperationStates.Enqueue(state);

        internal void ClearCloudOperationState()
        {
        }

        internal void ApplyCloudPostOperationSnapshot(
            CloudPostOperationSnapshot snapshot
        )
        {
            PostOperationSnapshots.Enqueue(snapshot);
            UiEvents.Enqueue(
                $"snapshot:eligible={snapshot.UploadEligibility.IsEligible}"
            );
        }

        internal void SetStatus(string message)
            => StatusMessages.Enqueue(message);

        internal void AppendLog(string message)
            => LogMessages.Enqueue(message);

        internal void ShowConfirmation(
            string message,
            Action confirm,
            string confirmText,
            string cancelText
        )
        {
            ConfirmationCount++;
            PendingConfirmation = confirm;
        }

        internal void ConfirmPending()
        {
            var confirm = PendingConfirmation
                ?? throw new InvalidOperationException(
                    "No confirmation callback is pending."
                );
            PendingConfirmation = null;
            confirm();
        }

        internal void SetActionPreferences(LauncherActionPreferences preferences)
        {
        }
    }

    internal readonly record struct LauncherActionPreferences(bool LocalBackupEnabled);

    internal static class LauncherPreferences
    {
        internal static string SelectedBranch = "public";
        internal static bool LocalBackupEnabled;
        internal static bool CloudSyncEnabled;

        internal static string ReadGameBranch()
            => SelectedBranch;

        internal static bool ReadLocalBackupEnabled()
            => LocalBackupEnabled;

        internal static void SaveLocalBackupEnabled(bool enabled)
            => LocalBackupEnabled = enabled;

        internal static void SaveCloudSyncEnabled(bool enabled)
            => CloudSyncEnabled = enabled;

        internal static LauncherActionPreferences ReadActionPreferences()
            => new(LocalBackupEnabled);

        internal static void Reset()
        {
            SelectedBranch = "public";
            LocalBackupEnabled = false;
            CloudSyncEnabled = false;
        }
    }

    internal static class LauncherCloudSyncEvidence
    {
        internal static bool PullCompletionRecorded = true;
        internal static bool PullMatchesSelectedBranch = true;
        internal static bool PullAfterBranchSwitch = true;
        internal static readonly ConcurrentQueue<string> BlockedReasons = new();

        internal static bool LastManualPullCompletionRecorded(string dataDir)
            => PullCompletionRecorded;

        internal static bool LastManualPullMatchesSelectedBranch(
            string dataDir,
            string selectedBranch
        )
            => PullMatchesSelectedBranch;

        internal static bool HasManualPullAfterBranchSwitch(
            string dataDir,
            string selectedBranch
        )
            => PullAfterBranchSwitch;

        internal static void WriteManualPushBlockedMarker(
            string dataDir,
            string selectedBranch,
            string reason
        )
            => BlockedReasons.Enqueue(reason);

        internal static void Reset()
        {
            PullCompletionRecorded = true;
            PullMatchesSelectedBranch = true;
            PullAfterBranchSwitch = true;
            while (BlockedReasons.TryDequeue(out _))
            {
            }
        }
    }

    internal static class LauncherLocalSaveEvidence
    {
        internal static bool ImportantSaveEvidenceAvailable = true;

        internal static bool HasImportantSaveEvidence(string dataDir)
            => ImportantSaveEvidenceAvailable;

        internal static int CountImportantSaveEvidence(string dataDir)
            => ImportantSaveEvidenceAvailable ? 1 : 0;
    }

    internal static class LauncherBackupEvidence
    {
        internal static int CurrentMirrorCount;

        internal static int CurrentMirrorSaveCount()
            => CurrentMirrorCount;
    }

    internal static class LauncherSaveOriginEvidence
    {
        internal static bool MatchesSelectedRuntime = true;

        internal static bool CurrentLocalSavesMatchSelectedRuntime(
            string dataDir,
            string selectedBranch
        )
            => MatchesSelectedRuntime;
    }

    internal static class LauncherBranchSwitchSafety
    {
        internal static bool MarkerPresent;
        internal static bool RequiredEvidenceAvailable = true;

        internal static bool HasMarker(string dataDir)
            => MarkerPresent;

        internal static bool HasRequiredEvidence(
            string dataDir,
            string selectedBranch
        )
            => RequiredEvidenceAvailable;
    }

    internal static class CloudBehaviorGateState
    {
        internal static void Reset()
        {
            LauncherPreferences.Reset();
            LauncherModSelectionState.IsModdedMode = false;
            CloudSyncCoordinator.IncompletePullMarkerPresent = false;
            LauncherCloudSyncEvidence.Reset();
            LauncherLocalSaveEvidence.ImportantSaveEvidenceAvailable = true;
            LauncherBackupEvidence.CurrentMirrorCount = 0;
            LauncherSaveOriginEvidence.MatchesSelectedRuntime = true;
            LauncherBranchSwitchSafety.MarkerPresent = false;
            LauncherBranchSwitchSafety.RequiredEvidenceAvailable = true;
            STS2Mobile.AppPaths.Reset();
        }
    }
}
