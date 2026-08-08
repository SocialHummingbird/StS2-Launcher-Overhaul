function Add-SteamVersionSelectionPortalBehaviorDebugLogChecks {
    Add-Check `
        "src\STS2Mobile\Steam\SteamConnectionConfigurationFactory.cs" `
        "sanitizes SteamKit debug logs before writing launcher diagnostics" `
        @(
            "SanitizeSteamKitDebugMessage",
            "SteamKitDebugLogsSanitized\s*=\s*true",
            "SteamKitDebugLogsOptInEnabled",
            "STS2_STEAMKIT_DEBUG_LOGS",
            "disabled by default",
            "SensitiveJsonValueRegex",
            "SensitiveKeyValueRegex",
            "BearerTokenRegex",
            "<redacted>",
            "PatchHelper\.Log",
            "SteamKit"
        )

    Add-Check `
        "android\src\com\game\sts2launcher\GodotApp.java" `
        "keeps SteamKit debug logging opt-in at the Android boundary" `
        @(
            "ENV_STEAMKIT_DEBUG_LOGS",
            "sts2_steamkit_debug_logs",
            "setSteamKitDebugLogMode",
            "Sanitized SteamKit debug logging enabled",
            "STS2_STEAMKIT_DEBUG_LOGS"
        )
}
