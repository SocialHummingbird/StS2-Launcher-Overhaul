function Add-SteamVersionSelectionPortalActionReadyActionSupportBoundaryChecks {
    Add-Check `
        "scripts\audit-steam-version-selection.action-support.ps1" `
        "keeps ready-state support-tool audit contracts behind a focused orchestrator" `
        @(
            "function Add-SteamVersionSelectionActionSupportChecks",
            "audit-steam-version-selection.action-support.foundation.ps1",
            "audit-steam-version-selection.action-support.construction.ps1",
            "audit-steam-version-selection.action-support.labels.ps1",
            "audit-steam-version-selection.action-support.diagnostics.ps1",
            "Add-SteamVersionSelectionActionSupportFoundationChecks",
            "Add-SteamVersionSelectionActionSupportConstructionChecks",
            "Add-SteamVersionSelectionActionSupportLabelChecks"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.action-support.foundation.ps1" `
        "keeps ready-state support foundation and typed-control audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionActionSupportFoundationChecks",
            "ActionSection.Construction.Support.Foundation.cs",
            "ActionSection.Construction.Support.Types.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.action-support.construction.ps1" `
        "keeps ready-state support construction and tool-button audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionActionSupportConstructionChecks",
            "ActionSection.Construction.Support.cs",
            "ActionSection.Construction.Support.Tools.cs",
            "ActionSection.Construction.Support.DiagnosticsTools.cs",
            "ActionSection.Construction.Primary.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.action-support.labels.ps1" `
        "keeps ready-state support label, drawer, and compact-button audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionActionSupportLabelChecks",
            "ActionSection.Support.cs",
            "ActionSection.CompactActionButton.cs",
            "ActionSection.Toggles.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.action-support.diagnostics.ps1" `
        "keeps ready-state support diagnostics sharing audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionActionSupportDiagnosticsChecks",
            "ActionSection.Construction.Support.DiagnosticsTools.cs",
            "Create Help Report"
        )
}
