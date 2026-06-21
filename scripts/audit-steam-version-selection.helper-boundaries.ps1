function Add-SteamVersionSelectionHelperBoundaryChecks {
    Add-Check `
        "scripts\audit-steam-version-selection.helper-boundaries.ps1" `
        "keeps shared helper-boundary audit contracts in a focused module" `
        @(
            "function Add-SteamVersionSelectionHelperBoundaryChecks",
            "static-audit-utils.ps1",
            "evidence-marker-utils.ps1",
            "LauncherMarkerFile.Read.cs",
            "LauncherGameFiles.Markers.cs",
            "audit-steam-version-selection.launcher-shell.ps1",
            "audit-steam-version-selection.branch-selector.ps1",
            "audit-steam-version-selection.branch-runtime.ps1",
            "audit-steam-version-selection.branch-availability.ps1",
            "audit-steam-version-selection.download-workflows.ps1",
            "audit-steam-version-selection.session-auth.ps1"
        )

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
        "scripts\audit-steam-version-selection.launcher-shell.ps1" `
        "keeps launcher shell audit contracts in a focused module" `
        @(
            "function Add-SteamVersionSelectionLauncherShellChecks",
            "LauncherUI.cs",
            "LauncherUI.Lifecycle.cs",
            "LauncherUI.MainThread.cs",
            "LauncherUI.Viewport.cs",
            "LauncherUI.AutoLaunch.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.branch-selector.ps1" `
        "keeps Steam branch selector audit contracts in a focused module" `
        @(
            "function Add-SteamVersionSelectionBranchSelectorChecks",
            "SteamGameBranch.cs",
            "LauncherBranchCatalog.Option.cs",
            "LauncherBranchDropdown.cs",
            "DownloadSection.Branches.cs",
            "ActionSection.Branches.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.branch-runtime.ps1" `
        "keeps Steam branch runtime and cache audit contracts in a focused module" `
        @(
            "function Add-SteamVersionSelectionBranchRuntimeChecks",
            "DepotDownloader.DepotManifestReference.cs",
            "LauncherBranchMarkerFields.cs",
            "LauncherAndroidAppPrivatePath.cs",
            "LauncherGameFiles.Redownload.cs",
            "LauncherGameFiles.CacheCleanup.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.download-workflows.ps1" `
        "keeps download, update, and branch-refresh workflow audit contracts in a focused module" `
        @(
            "function Add-SteamVersionSelectionDownloadWorkflowChecks",
            "LauncherModel.Downloads.cs",
            "LauncherModel.Downloads.Action.cs",
            "LauncherController.Downloads.Actions.cs",
            "LauncherController.UpdateChecks.cs",
            "LauncherController.UpdateChecks.ViewUpdate.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.session-auth.ps1" `
        "keeps Steam session authentication audit contracts in a focused module" `
        @(
            "function Add-SteamVersionSelectionSessionAuthChecks",
            "LauncherModel.SessionAuth.cs",
            "LauncherModel.SessionAuth.Attempt.cs",
            "LauncherSteamSession.Connection.SavedCredentials.cs",
            "LauncherSteamSession.Connection.Ensure.cs",
            "LauncherSteamSession.Connection.Adoption.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.branch-availability.ps1" `
        "keeps Steam branch availability audit contracts in a focused module" `
        @(
            "function Add-SteamVersionSelectionBranchAvailabilityChecks",
            "SteamBranchAvailabilityMarkerFields",
            "SteamBranchAvailabilityMarkerFile",
            "SteamBranchAvailabilityMarkerRow",
            "LauncherBranchAvailabilityStatus.Read.cs",
            "DepotDownloader.BranchAvailability.Marker.cs"
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

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherMarkerFile.cs" `
        "declares shared marker-file sentinel values" `
        @(
            "internal static partial class LauncherMarkerFile",
            "MissingFileValue = ""<none>""",
            "MissingLineValue = ""<missing>""",
            "ReadFailedValue = ""<read failed>"""
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherMarkerFile.Read.cs" `
        "centralizes scalar marker-file value parsing" `
        @(
            "ReadValue",
            "ReadOptionalValue",
            "File\.ReadLines",
            "StringComparison\.OrdinalIgnoreCase"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherMarkerFile.Typed.cs" `
        "centralizes typed marker-file parsing" `
        @(
            "ReadInt",
            "NumberStyles\.Integer",
            "CultureInfo\.InvariantCulture",
            "ReadUtc",
            "UtcParseable",
            "DateTimeStyles\.AdjustToUniversal",
            "ReadBoolFlag"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherMarkerFile.Values.cs" `
        "centralizes marker-file repeated-value reads" `
        @(
            "ReadJoinedValues",
            "ReadValues",
            "File\.ReadLines",
            "valueFormatter",
            "maxValues"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherMarkerFile.Predicates.cs" `
        "centralizes marker-file predicates and counts" `
        @(
            "CountLines",
            "HasLine",
            "HasConcreteValue"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherGameFiles.Markers.cs" `
        "keeps game-file marker evidence readers as thin shared-helper wrappers" `
        @(
            "ReadMarkerValue",
            "LauncherMarkerFile\.ReadValue",
            "ReadMarkerInt",
            "LauncherMarkerFile\.ReadInt",
            "MarkerUtcParseable",
            "LauncherMarkerFile\.UtcParseable",
            "MarkerHasLine",
            "LauncherMarkerFile\.HasLine"
        )
}
