function Add-SteamVersionSelectionEvidenceToolingChecks {
    Add-Check `
        "scripts\android-adb-utils.ps1" `
        "resolves adb from explicit path, PATH, or common Android SDK roots" `
        @(
            "Resolve-AndroidAdbPath",
            "ANDROID_HOME",
            "ANDROID_SDK_ROOT",
            "platform-tools",
            "\.w40k-android-toolchain",
            "Pass -AdbPath"
        )

    Add-Check `
        "scripts\capture-steam-version-selection-evidence.ps1" `
        "captures branch evidence with resolved adb and bounded backup scans" `
        @(
            "Resolve-AndroidAdbPath",
            "android-shell-utils\.ps1",
            "evidence-path-utils\.ps1",
            "evidence-redaction-utils\.ps1",
            "AdbPath",
            "Resolve-EvidenceRepoPath",
            "ConvertTo-AndroidShellSingleQuoted",
            "ConvertTo-AndroidShellPathSingleQuoted",
            "ConvertTo-RedactedLogLine",
            "quotedCommand",
            'run-as \$PackageName sh -c',
            "Invoke-DeviceShell",
            'sh -c \$quotedCommand',
            "echo local-pre-push:",
            "echo cloud-pre-push:",
            "last_game_version_cache_cleanup\.txt",
            "last_game_version_redownload\.txt",
            "last_steam_branch_availability\.txt",
            "marker-evidence-status\.txt",
            "Marker evidence status",
            "<missing marker>",
            "<empty marker>",
            "launcher-diagnostics-index\.txt",
            'Android/data/\$PackageName/files/diagnostics',
            'Invoke-DeviceShell -Command "find /storage/emulated/0/Android/data/\$PackageName/files/diagnostics',
            "timeout 10 find",
            "StS2Launcher/Saves -maxdepth 6",
            "pre-push-backup-counts\.txt"
        )

    Add-ForbiddenCheck `
        "scripts\capture-steam-version-selection-evidence.ps1" `
        "does not let external diagnostics find run from device root" `
        @(
            'Invoke-AdbText\s+-Arguments\s+@\("shell",\s*"sh",\s*"-c",\s*"find /storage/emulated/0/Android/data/\$PackageName/files/diagnostics'
        )

    Add-Check `
        "scripts\new-steam-version-selection-evidence.ps1" `
        "creates a structured artifact folder for ARM64 validation evidence" `
        @(
            "steam-version-selection",
            "branch-markers",
            "backup-evidence",
            "evidence-path-utils\.ps1",
            "evidence-redaction-utils\.ps1",
            "Resolve-EvidenceRepoPath",
            "steam-version-selection-evidence-template\.md",
            "steam-version-selection-release-readiness\.md",
            "ARTIFACT_HYGIENE\.txt",
            "PUBLIC_SHARE_MANIFEST\.txt",
            "PUBLIC_EVIDENCE_REDACTION_REVIEW\.txt",
            "review-public-evidence-redaction\.ps1",
            "Preferred public artifacts",
            "Local-only or manual-review artifacts",
            "Manual Push evidence marker filename",
            "Do not run manual Push"
        )

    Add-Check `
        "scripts\new-steam-version-selection-evidence.ps1" `
        "scaffolds non-secret device evidence review for version-selection validation" `
        @(
            "ARTIFACT_HYGIENE\.txt",
            "Raw logs and full launcher diagnostics are local-only",
            "PUBLIC_SHARE_MANIFEST\.txt",
            "PUBLIC_EVIDENCE_REDACTION_REVIEW\.txt",
            "Format-PublicEvidenceRedactionReviewFields",
            "preferred public artifacts",
            "steam_branch\.txt",
            "last_game_branch_switch\.txt",
            "last_manual_cloud_pull\.txt",
            "last_manual_cloud_push\.txt",
            "last_manual_cloud_push_blocked\.txt",
            "last_steam_branch_availability\.txt",
            "last_game_version_cache_cleanup\.txt",
            "last_game_version_redownload\.txt",
            "backup-evidence",
            "Resolve-EvidenceRepoPath",
            "branch-markers"
        )

    Add-Check `
        "scripts\audit-steam-branch-guidance-parity.ps1" `
        "checks managed/native selected-branch guidance phrase parity" `
        @(
            "SteamGameBranch\.cs",
            "SteamBranchInfo\.java",
            "Default/public Steam branch",
            "Failed downloads do not change Steam Cloud saves",
            "Save compatibility is unproven"
        )

    Add-Check `
        "scripts\export-public-evidence-redaction.ps1" `
        "exports a sanitized public-share evidence candidate without mutating raw evidence" `
        @(
            "SourceEvidenceDir",
            "evidence-path-utils\.ps1",
            "evidence-redaction-utils\.ps1",
            "PUBLIC_EVIDENCE_REDACTION_REVIEW\.txt",
            "PUBLIC_SHARE_MANIFEST\.txt",
            "Resolve-EvidenceRepoPath",
            "Get-EvidenceRelativePath",
            "ConvertTo-RedactedEvidenceText",
            "Format-PublicEvidenceRedactionReviewFields",
            "Get-EvidenceTextFileExtensions",
            "Get-EvidenceImageFileExtensions",
            "Test-EvidenceLocalOnlyPath",
            "Raw evidence remains local",
            "IncludeImages"
        )

    Add-Check `
        "scripts\review-public-evidence-redaction.ps1" `
        "fails public-share candidates without completed redaction review or with local-only artifacts" `
        @(
            "evidence-path-utils\.ps1",
            "evidence-redaction-utils\.ps1",
            "PUBLIC_EVIDENCE_REDACTION_REVIEW\.txt",
            "Get-PublicEvidenceRedactionReviewFields",
            "\[regex\]::Escape",
            "Get-EvidenceTextFileExtensions",
            "Get-EvidenceImageFileExtensions",
            "Get-EvidenceSensitiveTextChecks",
            "Test-EvidenceLocalOnlyPath",
            "Get-EvidenceRelativePath",
            "Screenshot/image requires completed"
        )

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
