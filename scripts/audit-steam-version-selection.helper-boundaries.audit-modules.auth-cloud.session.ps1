function Add-SteamVersionSelectionAuthCloudSessionBoundaryChecks {
    Add-Check `
        "scripts\audit-steam-version-selection.session-auth.ps1" `
        "keeps Steam session authentication audit contracts behind a focused orchestrator" `
        @(
            "function Add-SteamVersionSelectionSessionAuthChecks",
            "audit-steam-version-selection.session-auth.model.ps1",
            "audit-steam-version-selection.session-auth.connection.ps1",
            "Add-SteamVersionSelectionSessionAuthModelChecks",
            "Add-SteamVersionSelectionSessionAuthConnectionChecks"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.session-auth.model.ps1" `
        "keeps launcher model session-auth attempt and result audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionSessionAuthModelChecks",
            "LauncherModel.SessionAuth.cs",
            "LauncherModel.SessionAuth.Attempt.cs",
            "LauncherModel.SessionAuth.Result.cs",
            "LauncherModel.SessionAuth.Connection.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.session-auth.connection.ps1" `
        "keeps Steam session connection reuse and saved-credential adoption audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionSessionAuthConnectionChecks",
            "LauncherSteamSession.Connection.SavedCredentials.cs",
            "LauncherSteamSession.Connection.Ensure.cs",
            "LauncherSteamSession.Connection.Adoption.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.automation.ps1" `
        "keeps launcher automation audit contracts in a focused module" `
        @(
            "function Add-SteamVersionSelectionAutomationChecks",
            "LauncherAutomationCoordinator.cs",
            "LauncherAutomationCoordinator.Request.cs",
            "LauncherAutomationCoordinator.Run.cs",
            "LauncherAutomationCoordinator.Marker.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.local-login.ps1" `
        "keeps local Steam credential handoff audit contracts in a focused module" `
        @(
            "function Add-SteamVersionSelectionLocalLoginChecks",
            "LauncherSessionCoordinator.cs",
            "LauncherSessionCoordinator.LocalLogin.cs",
            "LocalSteamCredentials.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.confirmations.ps1" `
        "keeps confirmation dialog and contextual-action audit contracts in a focused module" `
        @(
            "function Add-SteamVersionSelectionConfirmationChecks",
            "LauncherView.Dialog.Buttons.cs",
            "LauncherView.Dialog.Message.cs",
            "LauncherView.Behavior.Confirmation.cs",
            "LauncherBranchSwitchCoordinator.cs",
            "LauncherCloudSyncCoordinator.Request.Factory.cs"
        )
}
