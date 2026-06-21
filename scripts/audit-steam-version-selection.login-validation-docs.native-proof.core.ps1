function Add-SteamVersionSelectionLoginValidationDocsNativeProofCoreChecks {
    Add-Check `
        "docs\android-steam-login-validation.md" `
        "keeps Android native login proof-contract documentation checks focused" `
        @(
            "android-login-portal-evidence-template\.md",
            "integrated in-app native Steam credential panel",
            "real Android username and password fields",
            "no user-facing native credential handoff popup",
            "does not store or inject Steam passwords",
            "real `EditText` fields",
            "Android Autofill hints",
            "Steam web-domain metadata",
            "launcher portal uses explicit status and titled state sections",
            "Diagnostics are hidden behind the Help & Reports drawer",
            "Android warmup/loading uses a mobile-width compact panel with readable styled percentage progress.",
            "Android post-launch startup status uses a framed mobile-width readable card after the launcher closes.",
            "Native fallback keeps verbose diagnostics collapsed until explicitly requested",
            "Native fallback recovery actions split into two touch-friendly rows on narrow landscape screens.",
            "Startup recovery compact actions render as structured user-facing controls: .*Restart App.*Open launcher.*Safe Start.*Cloud off.*Help Report.*Share details.*Copy Log.*Review first.*Hide Help.*Keep waiting",
            "The compact diagnostics toggle uses a touch-safe two-line detail label without exposing diagnostics by default.",
            "Compact diagnostics lives inside the scroll body rather than consuming fixed phone viewport chrome, and explicit diagnostics actions scroll to it."
        )
}
