function Add-SteamVersionSelectionBranchEvidenceCloudPushChecks {
    Add-Check `
        "docs\steam-version-selection-evidence-template.md" `
        "captures the shared Pull/Upload transfer contract and failure evidence" `
        @(
            "last_manual_cloud_push_blocked\.txt",
            "Upload is eligible with transferable allowlisted local saves and no preceding Pull",
            "Upload is blocked when no transferable local saves exist",
            "Upload is blocked while an interrupted Pull marker exists",
            "explicit separate allowlists",
            "Device settings remain local",
            "Transfer context records authenticated SteamID64",
            "Account, namespace, runtime, branch, and mod-set mismatches fail without transfer success",
            "immutable snapshot before destination mutation",
            "Every destination overwrite or deletion is backed up before mutation",
            "destination tombstones",
            "file_committed=false",
            "read back and every transferred hash is verified before success",
            "No failure or hash mismatch reports .synced.",
            "same direction-parameterized transfer implementation",
            "fails closed rather than silently adopting legacy data",
            "history only, not Upload prerequisites",
            "Manual Pull outcome",
            "Incomplete Pull marker present",
            "Selected save namespace",
            "Runtime compatibility / branch identity",
            "Mod-set fingerprint",
            "Manual Push blocked evidence reason",
            "Manual Push blocked before upload evidence recorded"
        )
}
