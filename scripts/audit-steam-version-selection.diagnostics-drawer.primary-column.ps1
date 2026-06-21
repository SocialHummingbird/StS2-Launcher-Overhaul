function Add-SteamVersionSelectionDiagnosticsDrawerPrimaryColumnChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Layout.PrimaryColumn.Result.cs" `
        "uses a typed primary-column layout result instead of a large positional tuple" `
        @(
            "LauncherViewPrimaryColumn",
            "CompactStatusDetailsButton",
            "WorkflowStepNumberLabels",
            "CompactStickyTaskHeader",
            "PrimaryScroll",
            "CompactDiagnosticsHost"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Layout.PrimaryColumn.StatusResult.cs" `
        "keeps primary status result fields isolated from full column layout result fields" `
        @(
            "LauncherViewPrimaryStatus",
            "StyledLabel Phase",
            "StyledLabel Action",
            "StyledLabel Message",
            "ColorRect Accent",
            "Control Capsule",
            "CompactDetailButton",
            "CompactHeadline",
            "CompactPhasePanel"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Layout.PrimaryColumn.BodyResult.cs" `
        "keeps primary body scroll result isolated from full column layout result fields" `
        @(
            "LauncherViewPrimaryBody",
            "ScrollContainer PrimaryScroll",
            "VBoxContainer Body"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Layout.PrimaryColumn.cs" `
        "hosts compact diagnostics inside the primary scroll body instead of fixed root chrome" `
        @(
            "VBoxContainer compactDiagnosticsHost",
            "compactDiagnosticsHost = new VBoxContainer",
            "compactDiagnosticsHost\.SizeFlagsHorizontal = Control\.SizeFlags\.ExpandFill",
            "left\.AddChild\(compactDiagnosticsHost\)",
            "compactDiagnosticsHost"
        )
}
