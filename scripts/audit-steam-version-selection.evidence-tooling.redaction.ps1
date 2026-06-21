function Add-SteamVersionSelectionEvidenceToolingRedactionChecks {
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
}
