function Add-SteamVersionSelectionNativeRoutingFallbackChecks {
    Add-Check `
        "android\src\com\game\sts2launcher\NativeFallbackActivity.java" `
        "shows branch marker readiness in native fallback diagnostics" `
        @(
            "steam_branch\.txt",
            "Selected Steam branch note",
            "SteamBranchInfo\.selectorHelpText",
            "SteamBranchInfo\.gameDirectory",
            "branch marker",
            "hasInstallSlotProvenance",
            "Steam branch marker install slot kind",
            "Steam branch marker expected install slot kind",
            "Steam branch marker install slot directory",
            "Steam branch marker expected install slot directory",
            "Steam branch marker has matching install slot provenance",
            "depotManifestCount"
        )

    Add-Check `
        "android\src\com\game\sts2launcher\NativeFallbackActivity.java" `
        "keeps native fallback recovery controls visible before verbose diagnostics" `
        @(
            "boolean landscape",
            "boolean compactActionRows",
            "LinearLayout actions",
            "actions\.setOrientation\(compactActionRows \? LinearLayout\.VERTICAL : \(landscape \? LinearLayout\.HORIZONTAL : LinearLayout\.VERTICAL\)\)",
            "createFallbackActionRow",
            "addFallbackActionRow",
            "useCompactFallbackActionRows",
            "width < dp\(900\)",
            "Copy diagnostics",
            "Restart launcher",
            "Clear files",
            "Show diagnostics",
            "Hide diagnostics",
            "styleActionButton",
            "setMinHeight\(dp\(48\)\)",
            "GradientDrawable",
            "addActionButton",
            "root\.addView\(actions, actionsParams\)",
            "createDiagnosticsView",
            "diagnosticsView\.setVisibility\(View\.GONE\)",
            "diagnosticsView\.setVisibility\(show \? View\.VISIBLE : View\.GONE\)",
            "copyDiagnostics\(diagnosticsText\)",
            "root\.addView\(diagnosticsView"
        )
}
