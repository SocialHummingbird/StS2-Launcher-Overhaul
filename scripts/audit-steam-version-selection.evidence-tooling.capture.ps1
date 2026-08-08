function Add-SteamVersionSelectionEvidenceToolingCaptureChecks {
    Add-Check `
        "scripts\android-adb-utils.ps1" `
        "resolves adb from explicit path, PATH, or common Android SDK roots" `
        @(
            "Resolve-AndroidAdbPath",
            "ANDROID_HOME",
            "ANDROID_SDK_ROOT",
            "platform-tools",
            "\.w40k-android-toolchain",
            "Pass -AdbPath"
        )

    Add-Check `
        "scripts\capture-steam-version-selection-evidence.ps1" `
        "captures branch evidence with resolved adb and bounded backup scans" `
        @(
            "Resolve-AndroidAdbPath",
            "android-shell-utils\.ps1",
            "evidence-path-utils\.ps1",
            "evidence-redaction-utils\.ps1",
            "AdbPath",
            "Resolve-EvidenceRepoPath",
            "ConvertTo-AndroidShellSingleQuoted",
            "ConvertTo-AndroidShellPathSingleQuoted",
            "ConvertTo-RedactedLogLine",
            "quotedCommand",
            'run-as \$PackageName sh -c',
            "Invoke-DeviceShell",
            'sh -c \$quotedCommand',
            "echo local-pre-push:",
            "echo cloud-pre-push:",
            "last_game_version_cache_cleanup\.txt",
            "last_game_version_redownload\.txt",
            "last_steam_branch_availability\.txt",
            "marker-evidence-status\.txt",
            "Marker evidence status",
            "<missing marker>",
            "<empty marker>",
            "launcher-diagnostics-index\.txt",
            'Android/data/\$PackageName/files/diagnostics',
            'Invoke-DeviceShell -Command "find /storage/emulated/0/Android/data/\$PackageName/files/diagnostics',
            "timeout 10 find",
            "StS2Launcher/Saves -maxdepth 6",
            "pre-push-backup-counts\.txt"
        )

    Add-ForbiddenCheck `
        "scripts\capture-steam-version-selection-evidence.ps1" `
        "does not let external diagnostics find run from device root" `
        @(
            'Invoke-AdbText\s+-Arguments\s+@\("shell",\s*"sh",\s*"-c",\s*"find /storage/emulated/0/Android/data/\$PackageName/files/diagnostics'
        )
}
