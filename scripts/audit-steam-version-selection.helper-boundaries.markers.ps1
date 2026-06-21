function Add-SteamVersionSelectionMarkerBoundaryChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherMarkerFile.cs" `
        "declares shared marker-file sentinel values" `
        @(
            "internal static partial class LauncherMarkerFile",
            "MissingFileValue = ""<none>""",
            "MissingLineValue = ""<missing>""",
            "ReadFailedValue = ""<read failed>"""
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherMarkerFile.Read.cs" `
        "centralizes scalar marker-file value parsing" `
        @(
            "ReadValue",
            "ReadOptionalValue",
            "File\.ReadLines",
            "StringComparison\.OrdinalIgnoreCase"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherMarkerFile.Typed.cs" `
        "centralizes typed marker-file parsing" `
        @(
            "ReadInt",
            "NumberStyles\.Integer",
            "CultureInfo\.InvariantCulture",
            "ReadUtc",
            "UtcParseable",
            "DateTimeStyles\.AdjustToUniversal",
            "ReadBoolFlag"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherMarkerFile.Values.cs" `
        "centralizes marker-file repeated-value reads" `
        @(
            "ReadJoinedValues",
            "ReadValues",
            "File\.ReadLines",
            "valueFormatter",
            "maxValues"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherMarkerFile.Predicates.cs" `
        "centralizes marker-file predicates and counts" `
        @(
            "CountLines",
            "HasLine",
            "HasConcreteValue"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherGameFiles.Markers.cs" `
        "keeps game-file marker evidence readers as thin shared-helper wrappers" `
        @(
            "ReadMarkerValue",
            "LauncherMarkerFile\.ReadValue",
            "ReadMarkerInt",
            "LauncherMarkerFile\.ReadInt",
            "MarkerUtcParseable",
            "LauncherMarkerFile\.UtcParseable",
            "MarkerHasLine",
            "LauncherMarkerFile\.HasLine"
        )
}
