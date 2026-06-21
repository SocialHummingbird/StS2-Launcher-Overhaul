function Add-SteamVersionSelectionCloudSafetyBranchSwitchMarkerChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherBranchSwitchSafety.cs" `
        "keeps branch-switch safety marker identity isolated" `
        @(
            "internal static partial class LauncherBranchSwitchSafety",
            "last_game_branch_switch\.txt",
            "internal const string MarkerFileName",
            "MarkerPath",
            "HasMarker"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherBranchSwitchSafety.Fields.cs" `
        "centralizes branch-switch safety marker field prefixes" `
        @(
            "UtcPrefix = ""UTC:""",
            "PreviousBranchPrefix = ""Previous branch:""",
            "SelectedBranchPrefix = ""Selected branch:""",
            "SelectedBranchSelectionKindPrefix = ""Selected branch selection kind:""",
            "SelectorModePrefix = ""Steam branch selector mode:""",
            "SelectedVersionPrefix = ""Selected version:""",
            "SelectedVersionSlotKindPrefix = ""Selected version slot kind:""",
            "SelectedVersionSlotDirectoryPrefix = ""Selected version slot directory:""",
            "SelectedBranchNotePrefix = ""Selected branch note:""",
            "LocalBackupForcedPrefix = ""Local backup forced on:""",
            "ManualPushRequiresBackupStoragePrefix = ""Manual Push requires backup storage:""",
            "WarningAcknowledgedPrefix = ""Warning acknowledged:""",
            "NonPublicBranchWarningAcknowledgedPrefix = ""Non-public branch warning acknowledged:"""
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherBranchSwitchSafety.Read.cs" `
        "reads and parses branch-switch safety marker evidence" `
        @(
            "MarkerUtc",
            "MarkerUtcParseable",
            "PreviousBranch",
            "SelectedBranch",
            "SelectedBranchSelectionKind",
            "SelectorMode",
            "SelectedVersion",
            "SelectedVersionSlotKind",
            "SelectedVersionSlotDirectory",
            "SelectedBranchNote",
            "LocalBackupForced",
            "ManualPushRequiresBackupStorage",
            "WarningAcknowledged",
            "NonPublicBranchWarningAcknowledged",
            "ReadMarkerValue",
            "LauncherMarkerFile\.ReadValue",
            "LauncherMarkerFile\.HasConcreteValue",
            "ReadMarkerBool",
            "TryReadMarkerUtc",
            "DateTimeStyles\.AdjustToUniversal"
        )
}
