function Add-MultiVersionRuntimeHelperChecks {
    Add-Check `
        "scripts\audit-multi-version-runtime.helpers.ps1" `
        "keeps multi-version runtime audit helper and module-boundary contracts in a focused module" `
        @(
            "function Add-MultiVersionRuntimeHelperChecks",
            "audit-multi-version-runtime.helpers.ps1",
            "audit-multi-version-runtime.runtime-slot.ps1",
            "audit-multi-version-runtime.runtime-pack.ps1",
            "audit-multi-version-runtime.patch-compatibility.ps1",
            "audit-multi-version-runtime.native-cache.ps1",
            "audit-multi-version-runtime.startup-patches.ps1",
            "audit-multi-version-runtime.save-safety.ps1",
            "audit-multi-version-runtime.diagnostics.ps1",
            "audit-multi-version-runtime.evidence-tooling.ps1"
        )

    Add-Check `
        "scripts\static-audit-utils.ps1" `
        "keeps shared static audit harness isolated from runtime-contract checks" `
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
        "centralizes marker-text parsing for runtime evidence scripts" `
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
        "scripts\evidence-report-utils.ps1" `
        "centralizes Markdown table row formatting for runtime evidence reports" `
        @(
            "Format-Cell",
            "Add-ValidationRow",
            "Add-HypothesisRow",
            "RequiredNextAction",
            "NextProof",
            '\$Lines\.Add',
            '\| \$\(Format-Cell'
        )

    Add-Check `
        "scripts\android-shell-utils.ps1" `
        "centralizes Android shell quoting for runtime evidence capture" `
        @(
            "ConvertTo-AndroidShellSingleQuoted",
            "ConvertTo-AndroidShellPathSingleQuoted",
            "Unsupported single quote in device path",
            "-split",
            "-join",
            "return ConvertTo-AndroidShellSingleQuoted"
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
}
