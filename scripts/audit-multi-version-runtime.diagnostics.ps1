function Add-MultiVersionRuntimeDiagnosticsChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherDiagnostics.ReportRuntimeSlot.cs" `
        "coordinates runtime slot, runtime pack, patch validation, cache, and pairing diagnostics" `
        @(
            "AppendGameRuntimeSlot",
            "GameRuntimeSlot\.Inspect\(dataDir, branch\)",
            "AppendRuntimeSlotSummary",
            "AppendRuntimeSlotSelectedFiles",
            "AppendRuntimePackEvidence",
            "AppendPatchCompatibilityEvidence",
            "AppendRuntimePatchValidationEvidence",
            "AppendRuntimeCacheEvidence",
            "AppendRuntimePairingSummary"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherDiagnostics.ReportRuntimeSlot.Slot.cs" `
        "surfaces selected runtime slot identity and selected file evidence" `
        @(
            "AppendRuntimeSlotSummary",
            "Runtime slot evidence marker present",
            "Runtime slot evidence selected branch matches current runtime",
            "Runtime slot evidence runtime slot ID matches current runtime",
            "Runtime slot evidence selected PCK matches current runtime",
            "Runtime slot evidence selected source sts2\.dll matches current runtime",
            "AppendRuntimeSlotSelectedFiles",
            "Selected runtime PCK SHA256",
            "Selected runtime release version",
            "Selected runtime depot manifest fingerprint",
            "Selected runtime identity summary",
            "Selected runtime slot ID",
            "Selected runtime source sts2\.dll SHA256",
            "Selected runtime active Android sts2\.dll SHA256"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherDiagnostics.ReportRuntimeSlot.RuntimePack.cs" `
        "surfaces selected runtime-pack manifest, hash, support assembly, and patch validation evidence" `
        @(
            "AppendRuntimePackEvidence",
            "Selected runtime pack status",
            "Selected runtime pack usability status",
            "Selected runtime pack usable",
            "Selected runtime pack source runtime slot ID",
            "Selected runtime pack source runtime slot ID matches selected runtime",
            "Selected runtime pack Android sts2\.dll SHA256",
            "Selected runtime pack Android sts2\.dll hash matches manifest",
            "Selected runtime pack patch validation status",
            "Selected runtime pack generated from clean directory",
            "Selected runtime pack support assemblies declared",
            "Selected runtime pack support assembly hashes declared",
            "Selected runtime pack missing symbol count"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherDiagnostics.ReportRuntimeSlot.PatchCompatibility.cs" `
        "surfaces selected patch compatibility and runtime patch validation marker evidence" `
        @(
            "AppendPatchCompatibilityEvidence",
            "Selected patch compatibility status",
            "Selected patch compatibility validation surface version",
            "Selected patch compatibility checked symbol count",
            "Selected patch compatibility validated PCK SHA256",
            "Selected patch compatibility source assembly matches selected",
            "AppendRuntimePatchValidationEvidence",
            "Runtime patch validation marker filename",
            "Runtime patch validation status",
            "Runtime patch validation selected PCK SHA256",
            "Runtime patch validation active Android sts2\.dll SHA256",
            "Runtime patch validation runtime pack status"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherDiagnostics.ReportRuntimeSlot.RuntimeCache.cs" `
        "surfaces selected runtime cache marker and selected-runtime match evidence" `
        @(
            "AppendRuntimeCacheEvidence",
            "Runtime cache marker filename",
            "Runtime cache marker package",
            "Runtime cache marker selected branch requires runtime pack",
            "Runtime cache marker runtime pack directory",
            "Runtime cache marker selected PCK SHA256",
            "Runtime cache marker selected source sts2\.dll SHA256",
            "Runtime cache marker publish cache active sts2\.dll SHA256",
            "Runtime cache marker selected PCK matches selected runtime",
            "Runtime cache prepared for selected runtime",
            "CachePreparedForSelectedRuntime\(dataDir, branch\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherDiagnostics.ReportRuntimeSlot.Pairing.cs" `
        "surfaces selected runtime pairing and final playability gate evidence" `
        @(
            "AppendRuntimePairingSummary",
            "Selected runtime pairing status",
            "Selected runtime requires usable runtime pack",
            "Selected runtime branch-matched Android runtime prepared",
            "Selected runtime compatible",
            "Selected runtime playable"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherDiagnostics.ReportBranchSwitchSafety.SaveOrigin.cs" `
        "surfaces save-origin evidence used by branch-switch Push safety" `
        @(
            "AppendSaveOriginEvidence",
            "Android save-origin marker present",
            "Android save-origin selected runtime slot ID matches current runtime",
            "Android save-origin current selected runtime is playable",
            "Android local saves verified for selected branch",
            "Android local saves verified for selected runtime",
            "Android save-origin selected PCK matches current runtime",
            "Android save-origin selected source sts2\.dll matches current runtime"
        )
}
