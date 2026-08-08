[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$OutputPath
)

$ErrorActionPreference = "Stop"

function Add-Evidence {
    param(
        [Parameter(Mandatory = $true)]
        [AllowEmptyCollection()]
        [Collections.Generic.List[object]]$Items,
        [Parameter(Mandatory = $true)][string]$Id,
        [Parameter(Mandatory = $true)]
        [ValidateSet("android-manifest", "steam-manifest", "collector")]
        [string]$Kind,
        [int]$Row = 0,
        [string]$Phase = ""
    )

    $item = [ordered]@{
        id = $Id
        kind = $Kind
        path = "evidence/$Id.json"
        sha256 = "<sha256>"
    }
    if ($Kind -eq "collector") {
        $item.row = $Row
        $item.phase = $Phase
        $item.inventory = [ordered]@{
            path = "evidence/$Id-inventory.json"
            sha256 = "<sha256>"
        }
    }
    $Items.Add([pscustomobject]$item)
}

function New-Check {
    param(
        [Parameter(Mandatory = $true)][string]$Id,
        [Parameter(Mandatory = $true)][string]$Type,
        [Parameter(Mandatory = $true)]$References
    )

    return [ordered]@{
        id = $Id
        type = $Type
        refs = $References
    }
}

$resolvedOutput = [IO.Path]::GetFullPath($OutputPath)
if (Test-Path -LiteralPath $resolvedOutput) {
    throw "Refusing to overwrite an existing Stage 5 matrix template: $resolvedOutput"
}

$evidence = [Collections.Generic.List[object]]::new()
$descriptors = @(
    @('r1-steam-before', 'steam-manifest'),
    @('r1-android-after', 'android-manifest'),
    @('r1-steam-after', 'steam-manifest'),
    @('r1-after-quit-sync', 'collector', 1, 'after-quit-sync'),

    @('r2-android-before', 'android-manifest'),
    @('r2-steam-before', 'steam-manifest'),
    @('r2-android-after', 'android-manifest'),
    @('r2-steam-after', 'steam-manifest'),
    @('r2-before-play-reconcile', 'collector', 2, 'before-play-reconcile'),

    @('r3-baseline-android', 'android-manifest'),
    @('r3-baseline-steam', 'steam-manifest'),
    @('r3-local-before', 'android-manifest'),
    @('r3-remote-before', 'steam-manifest'),
    @('r3-local-after', 'android-manifest'),
    @('r3-remote-after', 'steam-manifest'),
    @('r3-divergence-conflict', 'collector', 3, 'divergence-conflict'),

    @('r4-steam-before', 'steam-manifest'),
    @('r4-android-after', 'android-manifest'),
    @('r4-steam-after', 'steam-manifest'),
    @('r4-after-modded-sync', 'collector', 4, 'after-modded-sync'),

    @('r5-local-before', 'android-manifest'),
    @('r5-steam-before', 'steam-manifest'),
    @('r5-local-after', 'android-manifest'),
    @('r5-steam-after', 'steam-manifest'),
    @('r5-changed-mod-set-blocked', 'collector', 5, 'changed-mod-set-blocked'),

    @('r6-public-before', 'android-manifest'),
    @('r6-beta-before', 'android-manifest'),
    @('r6-beta-after', 'android-manifest'),
    @('r6-beta-steam-after', 'steam-manifest'),
    @('r6-public-after', 'android-manifest'),
    @('r6-public-steam-after', 'steam-manifest'),
    @('r6-after-switch-to-beta', 'collector', 6, 'after-switch-to-beta'),
    @('r6-after-switch-to-public', 'collector', 6, 'after-switch-to-public'),

    @('r7-baseline-android', 'android-manifest'),
    @('r7-baseline-steam', 'steam-manifest'),
    @('r7-local-offline', 'android-manifest'),
    @('r7-remote-offline', 'steam-manifest'),
    @('r7-local-after-retry', 'android-manifest'),
    @('r7-remote-after-retry', 'steam-manifest'),
    @('r7-offline-pending', 'collector', 7, 'offline-pending'),
    @('r7-retry-complete', 'collector', 7, 'retry-complete'),

    @('r8-baseline-android', 'android-manifest'),
    @('r8-baseline-steam', 'steam-manifest'),
    @('r8-local-before-crash', 'android-manifest'),
    @('r8-remote-before-restart', 'steam-manifest'),
    @('r8-local-after-restart', 'android-manifest'),
    @('r8-remote-after-restart', 'steam-manifest'),
    @('r8-pending-before-force-stop', 'collector', 8, 'pending-before-force-stop'),
    @('r8-after-restart', 'collector', 8, 'after-restart'),

    @('r9-commit-local-before', 'android-manifest'),
    @('r9-commit-steam-before', 'steam-manifest'),
    @('r9-commit-local-after', 'android-manifest'),
    @('r9-commit-steam-after', 'steam-manifest'),
    @('r9-commit-failure', 'collector', 9, 'commit-failure'),
    @('r9-readback-local-before', 'android-manifest'),
    @('r9-readback-steam-before', 'steam-manifest'),
    @('r9-readback-local-after', 'android-manifest'),
    @('r9-readback-steam-after', 'steam-manifest'),
    @('r9-readback-failure', 'collector', 9, 'readback-failure'),

    @('r10-android-original', 'android-manifest'),
    @('r10-android-restored', 'android-manifest'),
    @('r10-android-undone', 'android-manifest'),
    @('r10-steam-before', 'steam-manifest'),
    @('r10-steam-after-restore', 'steam-manifest'),
    @('r10-steam-after-undo', 'steam-manifest'),
    @('r10-after-restore', 'collector', 10, 'after-restore'),
    @('r10-after-undo', 'collector', 10, 'after-undo')
)
foreach ($descriptor in $descriptors) {
    if ($descriptor.Count -eq 4) {
        Add-Evidence `
            -Items $evidence `
            -Id ([string]$descriptor[0]) `
            -Kind ([string]$descriptor[1]) `
            -Row ([int]$descriptor[2]) `
            -Phase ([string]$descriptor[3])
    } else {
        Add-Evidence `
            -Items $evidence `
            -Id ([string]$descriptor[0]) `
            -Kind ([string]$descriptor[1])
    }
}

$rows = @(
    [ordered]@{
        row = 1
        checks = @(
            New-Check -Id 'r1-verified-upload' -Type 'verified-upload' -References ([ordered]@{
                context = 'vanilla-public'
                steamBefore = 'r1-steam-before'
                androidAfter = 'r1-android-after'
                steamAfter = 'r1-steam-after'
                collector = 'r1-after-quit-sync'
            })
        )
    },
    [ordered]@{
        row = 2
        checks = @(
            New-Check -Id 'r2-safe-download' -Type 'safe-download-with-backup' -References ([ordered]@{
                context = 'vanilla-public'
                androidBefore = 'r2-android-before'
                steamBefore = 'r2-steam-before'
                androidAfter = 'r2-android-after'
                destinationBackupTreeSha256 = '<verified-recovery-tree-sha256>'
                steamAfter = 'r2-steam-after'
                collector = 'r2-before-play-reconcile'
            })
        )
    },
    [ordered]@{
        row = 3
        checks = @(
            New-Check -Id 'r3-divergence-preserved' -Type 'divergence-preserved' -References ([ordered]@{
                context = 'vanilla-public'
                baselineAndroid = 'r3-baseline-android'
                baselineSteam = 'r3-baseline-steam'
                localBefore = 'r3-local-before'
                remoteBefore = 'r3-remote-before'
                localAfter = 'r3-local-after'
                remoteAfter = 'r3-remote-after'
                collector = 'r3-divergence-conflict'
            })
        )
    },
    [ordered]@{
        row = 4
        checks = @(
            New-Check -Id 'r4-modded-namespace-isolation' -Type 'modded-namespace-isolation' -References ([ordered]@{
                context = 'modded-exact'
                steamBefore = 'r4-steam-before'
                androidAfter = 'r4-android-after'
                steamAfter = 'r4-steam-after'
                collector = 'r4-after-modded-sync'
            })
        )
    },
    [ordered]@{
        row = 5
        checks = @(
            New-Check -Id 'r5-changed-mod-set-blocked' -Type 'changed-mod-set-blocked' -References ([ordered]@{
                localContext = 'modded-changed'
                remoteContext = 'modded-exact'
                localBefore = 'r5-local-before'
                steamBefore = 'r5-steam-before'
                localAfter = 'r5-local-after'
                steamAfter = 'r5-steam-after'
                collector = 'r5-changed-mod-set-blocked'
            })
        )
    },
    [ordered]@{
        row = 6
        checks = @(
            New-Check -Id 'r6-two-way-branch-isolation' -Type 'two-way-branch-isolation' -References ([ordered]@{
                publicContext = 'vanilla-public'
                betaContext = 'vanilla-public-beta'
                publicBefore = 'r6-public-before'
                betaBefore = 'r6-beta-before'
                betaAfter = 'r6-beta-after'
                betaSteamAfter = 'r6-beta-steam-after'
                publicAfter = 'r6-public-after'
                publicSteamAfter = 'r6-public-steam-after'
                betaCollector = 'r6-after-switch-to-beta'
                publicCollector = 'r6-after-switch-to-public'
            })
        )
    },
    [ordered]@{
        row = 7
        checks = @(
            New-Check -Id 'r7-offline-retry' -Type 'offline-retry' -References ([ordered]@{
                context = 'vanilla-public'
                baselineAndroid = 'r7-baseline-android'
                baselineSteam = 'r7-baseline-steam'
                localOffline = 'r7-local-offline'
                remoteOffline = 'r7-remote-offline'
                localAfterRetry = 'r7-local-after-retry'
                remoteAfterRetry = 'r7-remote-after-retry'
                offlineCollector = 'r7-offline-pending'
                retryCollector = 'r7-retry-complete'
            })
        )
    },
    [ordered]@{
        row = 8
        checks = @(
            New-Check -Id 'r8-crash-resume' -Type 'crash-resume' -References ([ordered]@{
                context = 'vanilla-public'
                baselineAndroid = 'r8-baseline-android'
                baselineSteam = 'r8-baseline-steam'
                localBeforeCrash = 'r8-local-before-crash'
                remoteBeforeRestart = 'r8-remote-before-restart'
                localAfterRestart = 'r8-local-after-restart'
                remoteAfterRestart = 'r8-remote-after-restart'
                beforeCollector = 'r8-pending-before-force-stop'
                afterCollector = 'r8-after-restart'
            })
        )
    },
    [ordered]@{
        row = 9
        checks = @(
            $(New-Check -Id 'r9-commit-failure' -Type 'commit-failure-no-success' -References ([ordered]@{
                context = 'vanilla-public'
                localBefore = 'r9-commit-local-before'
                steamBefore = 'r9-commit-steam-before'
                localAfter = 'r9-commit-local-after'
                steamAfter = 'r9-commit-steam-after'
                collector = 'r9-commit-failure'
            })),
            $(New-Check -Id 'r9-readback-failure' -Type 'readback-failure-no-success' -References ([ordered]@{
                context = 'vanilla-public'
                localBefore = 'r9-readback-local-before'
                steamBefore = 'r9-readback-steam-before'
                localAfter = 'r9-readback-local-after'
                steamAfter = 'r9-readback-steam-after'
                collector = 'r9-readback-failure'
            }))
        )
    },
    [ordered]@{
        row = 10
        checks = @(
            New-Check -Id 'r10-restore-undo-exact' -Type 'restore-undo-exact' -References ([ordered]@{
                context = 'vanilla-public'
                androidOriginal = 'r10-android-original'
                androidRestored = 'r10-android-restored'
                androidUndone = 'r10-android-undone'
                steamBefore = 'r10-steam-before'
                steamAfterRestore = 'r10-steam-after-restore'
                steamAfterUndo = 'r10-steam-after-undo'
                restoreCollector = 'r10-after-restore'
                undoCollector = 'r10-after-undo'
            })
        )
    }
)

$template = [ordered]@{
    schemaVersion = 1
    kind = 'stage5-physical-save-matrix'
    binding = [ordered]@{
        candidate = [ordered]@{
            apkPath = '<path-to-unchanged-candidate-apk>'
            apkSha256 = '<sha256>'
            unsignedApkSha256 = '<sha256-from-candidate-build-info>'
            buildInfoPath = '<path-to-candidate-build-info.txt>'
            buildInfoSha256 = '<sha256>'
            sourceCommit = '<40-character-clean-source-commit>'
            candidateRunId = '<github-run-id>'
            candidateRunAttempt = '<github-run-attempt>'
            abi = 'arm64-v8a'
            signerSha256 = 'fd0e3d5acf435c1d23bfc5c426e99aa9eb5808619ff1fc214ffca99cfac7e57a'
            packageName = 'com.sts2launcher.overhaul.fork.local'
            versionName = '<version-name>'
            versionCode = '417003'
            releaseTag = '<candidate-release-tag>'
            signingChannel = 'release'
            updateBaselineTag = 'v0.2.416-startup-recovery-ime'
            updateBaselineAssetName = 'StS2Launcher-v0.2.416-startup-recovery-ime-local-arm64-v8a.apk'
            updateBaselineApkSha256 = 'fdf2dcfcf2352d0e1a370da76922fb5b70cee3654d98c5fe9afbbd39554fc17b'
        }
        steamId64 = '<steam-id-64>'
        device = [ordered]@{
            serialSha256 = '<sha256-of-adb-serial>'
            manufacturer = '<manufacturer>'
            model = '<model>'
            androidApi = '<api-level>'
            abiList = '<abi-list>'
            buildFingerprint = '<build-fingerprint>'
        }
    }
    contexts = @(
        [ordered]@{
            id = 'vanilla-public'
            steamId64 = '<steam-id-64>'
            saveNamespace = 'vanilla'
            runtimeIdentity = 'public'
            modSetFingerprint = ''
        },
        [ordered]@{
            id = 'vanilla-public-beta'
            steamId64 = '<steam-id-64>'
            saveNamespace = 'vanilla'
            runtimeIdentity = 'public-beta'
            modSetFingerprint = ''
        },
        [ordered]@{
            id = 'modded-exact'
            steamId64 = '<steam-id-64>'
            saveNamespace = 'modded'
            runtimeIdentity = 'public'
            modSetFingerprint = '<exact-mod-set-fingerprint>'
        },
        [ordered]@{
            id = 'modded-changed'
            steamId64 = '<steam-id-64>'
            saveNamespace = 'modded'
            runtimeIdentity = 'public'
            modSetFingerprint = '<different-mod-set-fingerprint>'
        }
    )
    evidence = @($evidence)
    rows = $rows
}

$parent = Split-Path -Parent $resolvedOutput
if ($parent) {
    New-Item -ItemType Directory -Force -Path $parent | Out-Null
}
[IO.File]::WriteAllText(
    $resolvedOutput,
    ($template | ConvertTo-Json -Depth 16),
    [Text.UTF8Encoding]::new($false)
)
Write-Host "Stage 5 physical matrix template: $resolvedOutput"
