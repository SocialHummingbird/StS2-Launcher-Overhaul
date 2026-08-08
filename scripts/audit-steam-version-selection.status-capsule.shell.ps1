function Add-SteamVersionSelectionStatusCapsuleShellChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Layout.StatusCapsule.cs" `
        "wraps launcher status in a readable portal status capsule" `
        @(
            "BuildStatusCapsule",
            "BuildCompactStatusCapsule",
            "new PanelContainer",
            "BuildStatusStyle\(scale, compact: false\)",
            "BuildStatusPhaseStyle\(scale, compact: false\)",
            "statusAccent\.CustomMinimumSize",
            "statusLabel\.SizeFlagsHorizontal = Control\.SizeFlags\.ExpandFill"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Layout.StatusCapsule.Styles.cs" `
        "keeps status capsule visual styles in focused helpers" `
        @(
            "BuildStatusStyle",
            "BuildStatusPhaseStyle",
            "BuildCompactStatusDetailButtonStyle",
            "SetBorderWidthAll"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Layout.StatusCapsule.cs" `
        "centralizes compact status capsule sizing constants" `
        @(
            "CompactStatusBodySeparation = 5",
            "CompactStatusAccentHeight = 3",
            "CompactStatusHeadlineSeparation = 3",
            "CompactStatusHeadlineInlineSeparation = 6",
            "CompactStatusPhaseInlineWidth = 112",
            "CompactStatusPhaseHorizontalMargin = 7",
            "CompactStatusPhaseVerticalMargin = 3",
            "CompactStatusActionMinHeight = 24",
            "CompactStatusDetailHeight = 44",
            "CompactStatusDetailCueWidth = 62",
            "CompactStatusDetailCueFontSize = LauncherSectionMetrics\.CompactDetailLabelFontSize",
            "CompactStatusDetailHorizontalMargin = 8",
            "CompactStatusDetailVerticalMargin = 5",
            "CompactStatusDetailRowGap = 6",
            "CompactStatusDetailRadius = 7"
        )
}
