using System;
using System.Globalization;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal enum LauncherSaveSyncState
{
    UpToDate,
    NotSyncedYet,
    Syncing,
    SyncFailed,
    Offline,
    Conflict,
    ChangesQueued,
    RetryRequired,
    SignInRequired,
    StatusUnknown,
}

internal readonly record struct LauncherSaveSyncPresentation(
    LauncherSaveSyncState State,
    string Summary,
    string HomeState,
    string LocalState,
    string SteamState
)
{
    internal bool ShowEndpointDetails
        => State is LauncherSaveSyncState.SyncFailed
            or LauncherSaveSyncState.Offline
            or LauncherSaveSyncState.Conflict;

    internal static LauncherSaveSyncPresentation FromStatus(
        SaveSyncService.StatusSnapshot status
    )
    {
        if (!status.HasCredentials)
            return SignInRequired();

        if (status.LastFailureKind == SaveSyncService.SyncFailureKind.Authentication)
            return SignInRequired();

        if (status.Availability == SaveSyncService.SyncAvailability.Unavailable)
            return Offline(status);

        if (status.LastFailureKind != SaveSyncService.SyncFailureKind.None)
            return SyncFailed(status);

        if (status.ChangesQueued)
            return ChangesQueued(status);

        if (status.RetryRequired)
            return RetryRequired(status);

        if (status.SyncStatusUnknown)
            return StatusUnknown();

        if (status.HasSuccessfulSync)
            return UpToDate(status);

        return NotSyncedYet();
    }

    internal static LauncherSaveSyncPresentation UpToDate(
        SaveSyncService.StatusSnapshot status
    )
        => new(
            LauncherSaveSyncState.UpToDate,
            $"Up to date · Last synced {LastSuccess(status)}",
            "Synced",
            "Up to date",
            "Up to date"
        );

    internal static LauncherSaveSyncPresentation NotSyncedYet()
        => new(
            LauncherSaveSyncState.NotSyncedYet,
            "Not synced yet",
            "Not synced yet",
            "Saved locally",
            "Not checked yet"
        );

    internal static LauncherSaveSyncPresentation Syncing()
        => new(
            LauncherSaveSyncState.Syncing,
            "Syncing…",
            "Syncing",
            "Checking",
            "Checking"
        );

    internal static LauncherSaveSyncPresentation Conflict(
        SaveSyncService.StatusSnapshot status
    )
        => new(
            LauncherSaveSyncState.Conflict,
            "Conflict · Choose which copy to keep",
            "Conflict",
            "Different copy",
            "Different copy"
        );

    internal static LauncherSaveSyncPresentation SyncFailed(
        SaveSyncService.StatusSnapshot status
    )
        => new(
            LauncherSaveSyncState.SyncFailed,
            "Sync failed",
            "Sync failed",
            status.ChangesQueued ? "Changes queued" : "Saved locally",
            "Sync did not complete"
        );

    internal static LauncherSaveSyncPresentation Offline(
        SaveSyncService.StatusSnapshot status
    )
        => new(
            LauncherSaveSyncState.Offline,
            "Offline",
            "Offline",
            status.ChangesQueued
                ? "Changes queued"
                : status.RetryRequired ? "Update queued" : "Saved locally",
            "Network or authentication unavailable"
        );

    internal static LauncherSaveSyncPresentation ChangesQueued(
        SaveSyncService.StatusSnapshot status
    )
        => new(
            LauncherSaveSyncState.ChangesQueued,
            "Changes queued",
            "Changes queued",
            "Changes queued",
            "Waiting to sync"
        );

    internal static LauncherSaveSyncPresentation RetryRequired(
        SaveSyncService.StatusSnapshot status
    )
        => new(
            LauncherSaveSyncState.RetryRequired,
            "Retry required",
            "Retry required",
            "Saved locally",
            "Update queued"
        );

    internal static LauncherSaveSyncPresentation SignInRequired()
        => new(
            LauncherSaveSyncState.SignInRequired,
            "Sign in required",
            "Sign in required",
            "Saved locally",
            "Sign in to sync"
        );

    internal static LauncherSaveSyncPresentation StatusUnknown()
        => new(
            LauncherSaveSyncState.StatusUnknown,
            "Sync status unknown",
            "Sync required",
            "Not checked",
            "Not checked"
        );

    private static string LastSuccess(SaveSyncService.StatusSnapshot status)
    {
        if (status.LastSuccessfulSyncUtc is { } completedAt)
        {
            return completedAt.ToLocalTime().ToString(
                "g",
                CultureInfo.CurrentCulture
            );
        }

        return "earlier";
    }
}
