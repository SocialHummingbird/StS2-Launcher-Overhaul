function Add-SteamVersionSelectionEvidenceToolingDocsChecks {
    Add-Check `
        "docs\steam-version-selection-tooling.md" `
        "documents static audit and evidence capture helper usage" `
        @(
            "audit-steam-version-selection\.ps1",
            "audit-steam-branch-guidance-parity\.ps1",
            "new-steam-version-selection-evidence\.ps1",
            "export-public-evidence-redaction\.ps1",
            "review-public-evidence-redaction\.ps1",
            "capture-steam-version-selection-evidence\.ps1",
            "steam-version-selection-release-readiness\.md",
            "ARTIFACT_HYGIENE\.txt",
            "local-only/raw-log",
            "PUBLIC_SHARE_MANIFEST\.txt",
            "PUBLIC_EVIDENCE_REDACTION_REVIEW\.txt",
            "Review public evidence redaction",
            "Export a sanitized public evidence candidate",
            "Raw evidence remains local",
            "Does not mutate artifacts and does not replace manual review",
            "safer public-sharing defaults",
            "logcat-steam-version-focused-redacted\.txt",
            "IncludeRawLogcat",
            "omitted by default",
            "normalize local path separators",
            "steamkit-debug-log-setting\.txt",
            "logcat-redaction-summary\.txt",
            "launcher-diagnostics-index\.txt",
            "AdbPath",
            "ANDROID_HOME",
            "ANDROID_SDK_ROOT",
            "\.w40k-android-toolchain",
            "last_game_branch_switch\.txt",
            "last_manual_cloud_push\.txt",
            "last_manual_cloud_push_blocked\.txt",
            "pre-push-backup-counts\.txt",
            "Artifact hygiene",
            "Do not store Steam credentials",
            "sts2_steamkit_debug_logs",
            "disabled by default",
            "Credential providers versus local credential handoff",
            "developer-only automation aids"
        )
}
