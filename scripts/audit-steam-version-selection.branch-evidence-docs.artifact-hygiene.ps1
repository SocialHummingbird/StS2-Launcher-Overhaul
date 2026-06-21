function Add-SteamVersionSelectionBranchEvidenceArtifactHygieneChecks {
    Add-Check `
        "docs\steam-version-selection-evidence-template.md" `
        "captures public artifact hygiene and redaction evidence" `
        @(
            "Artifact hygiene",
            "Steam credentials",
            "refresh tokens",
            "shared preferences",
            "device identifiers",
            "Raw full logcat was omitted by default",
            "IncludeRawLogcat",
            "sts2_steamkit_debug_logs",
            "logcat-steam-version-focused-redacted\.txt",
            "best-effort redacted file was manually reviewed before posting",
            "Redacted focused logcat includes its best-effort/manual-review warning header",
            "ARTIFACT_HYGIENE\.txt",
            "raw logs are treated as local-only",
            "PUBLIC_SHARE_MANIFEST\.txt",
            "preferred public artifacts",
            "logcat-redaction-summary\.txt",
            "focused-line and changed-line counts",
            "launcher-diagnostics-index\.txt",
            "full launcher diagnostics report attached publicly was manually reviewed/redacted",
            "Full launcher diagnostics and startup-recovery diagnostics reports include a public-sharing warning"
        )
}
