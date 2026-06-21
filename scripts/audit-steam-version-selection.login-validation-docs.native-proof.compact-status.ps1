function Add-SteamVersionSelectionLoginValidationDocsNativeProofCompactStatusChecks {
    Add-Check `
        "docs\android-steam-login-validation.md" `
        "keeps compact status, quick-start, and workflow proof wording focused" `
        @(
            "The compact status card stays readable while using low-profile spacing so more current task content remains visible.",
            "The compact status card stacks the phase chip and guided next action so neither is squeezed on narrow screens.",
            "Compact status details keep normal progress text to one stable line while attention/failure guidance can expand.",
            "Compact status uses short mobile detail copy and expands full raw status when tapped.",
            "Compact status exposes a visible `Details` / `Hide` cue in a touch-safe row.",
            "Compact sign-in status says `Sign in with Steam to continue` instead of exposing the raw credential prompt.",
            "Compact download-needed status says `Download this game version to play` and the next action reads `Install Game`.",
            "Compact ready status says `Ready to play this version` and the next action reads `Start Game`.",
            "Compact quick-start guidance starts collapsed behind a touch-safe two-line toggle that says `Quick Start` and `Get saves first`.",
            "Compact quick-start toggle renders Quick Start / Get saves first as structured title/detail labels",
            "Compact expanded quick-start guide renders each step inside a bounded row card.",
            "Compact sign-in, Steam Guard, and download task screens suppress the quick-start drawer so the current primary controls stay higher in the viewport.",
            "The compact brand subtitle remains readable at phone scale and uses plain app copy instead of pipe-separated command-line-style labels.",
            "The compact workflow strip stays in one dense row on narrow compact viewports instead of taking a second fixed header row.",
            "Tapping compact workflow step labels scrolls directly to the visible matching task section or the current safe fallback task."
        )
}
