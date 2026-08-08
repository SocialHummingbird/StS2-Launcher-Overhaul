function Add-SteamVersionSelectionCloudSafetyLocalSaveEvidenceChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherLocalSaveEvidence.cs" `
        "exposes bounded local save evidence counts for branch-switch Push safety" `
        @(
            "internal static partial class LauncherLocalSaveEvidence",
            "MaxFilesToInspect = 1000",
            "MaxDirectoriesToInspect = 250",
            "IgnoredDirectoryNames",
            "HasImportantSaveEvidence",
            "CountImportantSaveEvidence"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherLocalSaveEvidence.Classify.cs" `
        "classifies non-empty Android save artifacts as important local save evidence" `
        @(
            "IsImportantSaveEvidence",
            "Path\.GetRelativePath",
            "ToLowerInvariant",
            "FileHasContent",
            "\.save",
            "\.save\.backup",
            "\.run",
            "\.bak",
            "prefs",
            "prefs\.save",
            "prefs\.backup",
            "prefs\.save\.backup",
            "new FileInfo\(file\)\.Length > 0"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherLocalSaveEvidence.Enumeration.cs" `
        "walks local save directories with runtime-directory exclusions and scan limits" `
        @(
            "EnumerateFilesSafely",
            "new Stack<string>",
            "MaxFilesToInspect",
            "MaxDirectoriesToInspect",
            "IsIgnoredRuntimeDirectory",
            "IgnoredDirectoryNames",
            "StringComparison\.OrdinalIgnoreCase",
            "SafeFiles",
            "SafeDirectories"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherLocalSaveEvidence.FileSystem.cs" `
        "swallows filesystem enumeration failures when scanning local save evidence" `
        @(
            "SafeFiles",
            "Directory\.GetFiles",
            "SafeDirectories",
            "Directory\.GetDirectories",
            "Array\.Empty<string>\(\)"
        )
}
