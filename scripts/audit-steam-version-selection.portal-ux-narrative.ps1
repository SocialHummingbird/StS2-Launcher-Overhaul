function Add-SteamVersionSelectionPortalUxNarrativeChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherPortalUxSupport.cs" `
        "keeps the reporter-facing portal narrative aligned with the destination architecture and validation boundary" `
        @(
            "Task-led launcher with five stable destinations",
            "Home keeps readiness, Start Game, Safe Start, and retry actions together",
            "Saves keeps Pull before Push",
            "locked reveal, arm, and explicit overwrite confirmation gates",
            "Versions owns branch selection, update, repair, refresh, and cached-version cleanup controls",
            "Mods owns play mode and Workshop controls",
            "Help owns recovery, diagnostics, error, report, and review-before-sharing log controls",
            "Phone layouts use persistent bottom navigation",
            "wide and foldable layouts use top navigation",
            "stable container",
            "without deferred task re-anchoring or ready-path reparenting",
            "display safe-area insets",
            "composition refresh",
            "Steam Cloud Push gates remain unchanged",
            "Exact final-build cover, inner-display, rotation, and real-game handoff evidence still requires an unlocked ARM64 device",
            "Steam Cloud Push is outside this UI validation"
        )
}
