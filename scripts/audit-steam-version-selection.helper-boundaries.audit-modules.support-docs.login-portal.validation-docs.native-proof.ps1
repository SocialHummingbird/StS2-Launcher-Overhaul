function Add-SteamVersionSelectionSupportDocsLoginValidationNativeProofBoundaryChecks {
    Add-Check `
        "scripts\audit-steam-version-selection.login-validation-docs.native-proof.ps1" `
        "keeps Android native login proof-contract documentation checks behind a focused orchestrator" `
        @(
            "function Add-SteamVersionSelectionLoginValidationDocsNativeProofChecks",
            "audit-steam-version-selection.login-validation-docs.native-proof.core.ps1",
            "audit-steam-version-selection.login-validation-docs.native-proof.compact-status.ps1",
            "audit-steam-version-selection.login-validation-docs.native-proof.compact-actions.ps1",
            "audit-steam-version-selection.login-validation-docs.native-proof.task-header.ps1",
            "audit-steam-version-selection.login-validation-docs.native-proof.evidence-matrix.ps1",
            "Add-SteamVersionSelectionLoginValidationDocsNativeProofCoreChecks",
            "Add-SteamVersionSelectionLoginValidationDocsNativeProofCompactStatusChecks",
            "Add-SteamVersionSelectionLoginValidationDocsNativeProofCompactActionChecks",
            "Add-SteamVersionSelectionLoginValidationDocsNativeProofTaskHeaderChecks",
            "Add-SteamVersionSelectionLoginValidationDocsNativeProofEvidenceMatrixChecks"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.login-validation-docs.native-proof.core.ps1" `
        "keeps Android native login proof core documentation checks focused" `
        @(
            "function Add-SteamVersionSelectionLoginValidationDocsNativeProofCoreChecks",
            "integrated in-app native Steam credential panel",
            "Native fallback keeps verbose diagnostics collapsed"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.login-validation-docs.native-proof.compact-status.ps1" `
        "keeps compact status and quick-start native-proof documentation checks focused" `
        @(
            "function Add-SteamVersionSelectionLoginValidationDocsNativeProofCompactStatusChecks",
            "Compact sign-in status says",
            "Compact quick-start guidance"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.login-validation-docs.native-proof.compact-actions.ps1" `
        "keeps compact action and cloud native-proof documentation checks focused" `
        @(
            "function Add-SteamVersionSelectionLoginValidationDocsNativeProofCompactActionChecks",
            "Compact Pull action",
            "Compact armed Push warning"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.login-validation-docs.native-proof.task-header.ps1" `
        "keeps compact task-header and section native-proof documentation checks focused" `
        @(
            "function Add-SteamVersionSelectionLoginValidationDocsNativeProofTaskHeaderChecks",
            "compact current-task bar",
            "compact sticky task header"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.login-validation-docs.native-proof.evidence-matrix.ps1" `
        "keeps Android native login evidence-matrix documentation checks focused" `
        @(
            "function Add-SteamVersionSelectionLoginValidationDocsNativeProofEvidenceMatrixChecks",
            "Native credential panel requests both Autofill fields",
            "Password-manager suggestions device validated"
        )
}
