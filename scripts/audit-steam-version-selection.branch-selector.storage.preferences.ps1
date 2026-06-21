function Add-SteamVersionSelectionBranchSelectorStoragePreferenceChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherPreferences.cs" `
        "declares launcher preference keys and typed preference storage roots" `
        @(
            "LocalBackupPreferenceKey",
            "CloudSyncPreferenceKey",
            "game_branch",
            "GameBranchPreference",
            "BooleanPreference",
            "OperatingSystem\.IsAndroid"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherPreferences.GameBranch.cs" `
        "persists selected Steam game branch through normalized storage" `
        @(
            "ReadGameBranch",
            "SaveGameBranch",
            "GameBranchPreferenceExists",
            "SteamGameBranch\.Normalize",
            "SteamGameBranch\.Public",
            "GameBranchPreference\.WriteText"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherPreferences.ActionPreferences.cs" `
        "keeps aggregate launcher action preferences typed" `
        @(
            "internal readonly struct ActionPreferences",
            "LocalBackupEnabled",
            "CloudSyncEnabled",
            "GameBranch",
            "ReadActionPreferences",
            "LoadAndApplyActionPreferences"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherPreferences.BooleanPreference.cs" `
        "isolates boolean preference read apply and save behavior" `
        @(
            "internal BooleanPreference",
            "Func<bool> defaultValue",
            "Action<bool> apply",
            "Action<bool>\? beforeSave = null",
            "LoadAndApply",
            "BeforeSave\?\.Invoke",
            "Storage\.WriteBoolean"
        )
}
