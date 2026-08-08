function Add-SteamVersionSelectionSharedUtilityBoundaryChecks {
    Add-Check `
        "scripts\static-audit-utils.ps1" `
        "keeps shared static audit harness isolated from version-selection contracts" `
        @(
            "Initialize-StaticAudit",
            "Resolve-RepoPath",
            "Read-RepoFile",
            "Add-Check",
            "Add-ForbiddenCheck",
            "Complete-StaticAudit",
            "StaticAuditFailures",
            "StaticAuditPasses",
            "StaticAuditQuiet",
            "ThrowOnFailure"
        )

    Add-Check `
        "scripts\evidence-marker-utils.ps1" `
        "centralizes marker-text parsing for release evidence scripts" `
        @(
            "Read-MarkerValueFromText",
            "Read-MarkerIntFromText",
            "Read-MarkerRowsFromText",
            "Read-BranchFromMarkerText",
            "MissingValue",
            "OrdinalIgnoreCase",
            "\[int\]::TryParse",
            'return @\(\$MissingValue\)'
        )

    Add-Check `
        "scripts\android-shell-utils.ps1" `
        "centralizes Android shell quoting for run-as evidence capture" `
        @(
            "ConvertTo-AndroidShellSingleQuoted",
            "ConvertTo-AndroidShellPathSingleQuoted",
            "Unsupported single quote in device path",
            "-split",
            "-join",
            "return ConvertTo-AndroidShellSingleQuoted"
        )

    Add-Check `
        "scripts\evidence-path-utils.ps1" `
        "centralizes repo-relative and evidence-relative path helpers" `
        @(
            "Resolve-EvidenceRepoPath",
            "Get-EvidenceRelativePath",
            "ConvertTo-EvidenceSafeFileName",
            "IsPathRooted",
            "MakeRelativeUri",
            "UnescapeDataString",
            "DirectorySeparatorChar",
            "empty",
            "\[\^A-Za-z0-9\._-\]"
        )

    Add-Check `
        "scripts\evidence-redaction-utils.ps1" `
        "centralizes public evidence and focused log redaction patterns" `
        @(
            "ConvertTo-RedactedEvidenceText",
            "ConvertTo-RedactedLogLine",
            "Get-EvidenceTextFileExtensions",
            "Get-EvidenceImageFileExtensions",
            "Get-EvidenceLocalOnlyPathPatterns",
            "Test-EvidenceLocalOnlyPath",
            "Get-EvidenceSensitiveTextChecks",
            "Get-PublicEvidenceRedactionReviewFields",
            "Format-PublicEvidenceRedactionReviewFields",
            "Screenshots manually reviewed",
            "Credential suggestions absent",
            "Only sanitized diagnostics selected for public sharing",
            "redacted-local-path",
            "android-app-private",
            "redacted-device-serial",
            "redacted-email",
            "Bearer <redacted>",
            "logs\\\\logcat-full",
            "logs\\\\logcat-steam-version-focused",
            "logcat-\(\?!steam-version-focused-redacted\)",
            "startup-routing-focused",
            "credential/token assignment",
            "Android package-private data path",
            "known connected device serial",
            "saveData",
            "profileContent"
        )
}
