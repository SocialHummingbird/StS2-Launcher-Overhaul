param(
    [string]$Repo = "SocialHummingbird/StS2-Launcher-Overhaul",
    [int]$Limit = 40,
    [string]$OutputPath = ""
)

$ErrorActionPreference = "Stop"

if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    throw "GitHub CLI not found: gh"
}

function Invoke-GhJson {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    $output = & gh @Arguments 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "gh $($Arguments -join ' ') failed.`n$($output -join "`n")"
    }

    return ($output -join "`n") | ConvertFrom-Json
}

function Normalize-Digest {
    param([string]$Digest)

    if (-not $Digest) {
        return ""
    }
    if ($Digest.StartsWith("sha256:", [System.StringComparison]::OrdinalIgnoreCase)) {
        return $Digest.Substring("sha256:".Length).ToLowerInvariant()
    }
    return $Digest.ToLowerInvariant()
}

function Get-ReleaseClass {
    param(
        $Release,
        [bool]$IsNewestApkRelease,
        [bool]$IsGitHubLatestNonPrerelease
    )

    if ($IsNewestApkRelease) {
        return "current-prerelease"
    }
    if ($IsGitHubLatestNonPrerelease) {
        return "github-latest-non-prerelease"
    }
    if ($Release.prerelease -and $Release.tag_name -match "(?i)(local|audit|evidence|debug|fix|smoke|test)") {
        return "historical-test-prerelease"
    }
    if ($Release.prerelease) {
        return "historical-prerelease"
    }
    return "historical-release"
}

function Get-AssetNames {
    param($Release)

    return @($Release.assets | ForEach-Object { $_.name })
}

$releasePayload = Invoke-GhJson -Arguments @(
    "api",
    "repos/$Repo/releases?per_page=$Limit"
)
$releases = @($releasePayload)
if ($releases.Count -eq 1 -and $releases[0] -is [System.Array]) {
    $releases = @($releases[0])
}

if ($releases.Count -eq 0) {
    throw "No GitHub releases found for $Repo"
}

$apkReleases = @($releases | Where-Object { @($_.assets | Where-Object { $_.name -match "^StS2Launcher-v.*\.apk$" }).Count -gt 0 })
if ($apkReleases.Count -eq 0) {
    throw "No APK releases found for $Repo"
}

$newestApkRelease = $apkReleases[0]
$githubLatestNonPrerelease = $apkReleases | Where-Object { -not $_.prerelease } | Select-Object -First 1

$rows = New-Object System.Collections.Generic.List[object]
foreach ($release in $apkReleases) {
    $assetNames = Get-AssetNames $release
    $apkAssets = @($release.assets | Where-Object { $_.name -match "^StS2Launcher-v.*\.apk$" })
    foreach ($apk in $apkAssets) {
        $apkName = $apk.name
        $digest = Normalize-Digest $apk.digest
        $hasChecksum = $assetNames -contains "$apkName.sha256"
        $hasJson = $assetNames -contains "$apkName.json"
        $hasBuildInfo = $assetNames -contains "$apkName.build-info.txt"
        $body = [string]$release.body
        $bodyNamesApk = $body.Contains($apkName)
        $bodyNamesSha = $digest -and $body.Contains($digest)
        $class = Get-ReleaseClass `
            -Release $release `
            -IsNewestApkRelease:($release.id -eq $newestApkRelease.id) `
            -IsGitHubLatestNonPrerelease:($githubLatestNonPrerelease -and $release.id -eq $githubLatestNonPrerelease.id)

        $issues = New-Object System.Collections.Generic.List[string]
        if (-not $hasChecksum) {
            $issues.Add("missing checksum")
        }
        if (-not ($hasJson -or $hasBuildInfo)) {
            $issues.Add("missing metadata")
        }
        if (-not $bodyNamesApk) {
            $issues.Add("body missing APK")
        }
        if (-not $bodyNamesSha) {
            $issues.Add("body missing SHA")
        }
        if ($class -eq "github-latest-non-prerelease" -and $release.tag_name -ne $newestApkRelease.tag_name) {
            $issues.Add("GitHub latest is not newest APK")
        }

        $rows.Add([pscustomobject]@{
            Published = ([DateTime]$release.published_at).ToString("yyyy-MM-dd")
            Tag = $release.tag_name
            Name = $release.name
            Class = $class
            Prerelease = [bool]$release.prerelease
            Apk = $apkName
            Sha256 = $digest
            Checksum = if ($hasChecksum) { "yes" } else { "no" }
            Metadata = if ($hasJson) { "json" } elseif ($hasBuildInfo) { "build-info" } else { "no" }
            BodyNamesApk = if ($bodyNamesApk) { "yes" } else { "no" }
            BodyNamesSha = if ($bodyNamesSha) { "yes" } else { "no" }
            Issues = if ($issues.Count -eq 0) { "ok" } else { $issues -join "; " }
        })
    }
}

$recommended = $rows[0]
Write-Host "Release inventory: $Repo"
Write-Host "Recommended newest APK: $($recommended.Tag) / $($recommended.Apk)"
Write-Host "GitHub latest non-prerelease: $($githubLatestNonPrerelease.tag_name)"
if ($githubLatestNonPrerelease -and $githubLatestNonPrerelease.tag_name -ne $newestApkRelease.tag_name) {
    Write-Host "WARNING: GitHub /releases/latest points at $($githubLatestNonPrerelease.tag_name), not newest APK $($newestApkRelease.tag_name)."
}

$rows |
    Select-Object Published, Tag, Class, Checksum, Metadata, BodyNamesApk, BodyNamesSha, Issues |
    Format-Table -AutoSize

if ($OutputPath) {
    $resolvedOutputPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputPath)
    $outputDir = Split-Path -Parent $resolvedOutputPath
    if ($outputDir) {
        New-Item -ItemType Directory -Force $outputDir | Out-Null
    }

    $lines = New-Object System.Collections.Generic.List[string]
    $lines.Add("# GitHub Release Inventory")
    $lines.Add("")
    $lines.Add(('Generated from `{0}` with `scripts/audit-github-release-inventory.ps1`.' -f $Repo))
    $lines.Add("")
    $lines.Add("## Current Download")
    $lines.Add("")
    $lines.Add(('- Recommended newest APK release: `{0}`' -f $recommended.Tag))
    $lines.Add(('- APK: `{0}`' -f $recommended.Apk))
    $lines.Add(('- SHA-256: `{0}`' -f $recommended.Sha256))
    $lines.Add(('- GitHub `/releases/latest` non-prerelease target: `{0}`' -f $githubLatestNonPrerelease.tag_name))
    if ($githubLatestNonPrerelease -and $githubLatestNonPrerelease.tag_name -ne $newestApkRelease.tag_name) {
        $lines.Add('- Warning: GitHub `/releases/latest` does not point at the newest APK because newer APKs are prereleases.')
    }
    $lines.Add("")
    $lines.Add("## Release Classes")
    $lines.Add("")
    $lines.Add('- `current-prerelease`: newest APK release and the current recommended tester download.')
    $lines.Add('- `github-latest-non-prerelease`: what GitHub marks as Latest when prereleases are excluded; may be older than the recommended APK.')
    $lines.Add('- `historical-test-prerelease`: older local/debug/evidence/audit build kept for traceability.')
    $lines.Add('- `historical-prerelease`: older prerelease kept for traceability.')
    $lines.Add('- `historical-release`: older non-prerelease APK kept for traceability or upgrade-baseline context.')
    $lines.Add("")
    $lines.Add("## Inventory")
    $lines.Add("")
    $lines.Add("| Published | Release | Class | APK | Checksum | Metadata | Body APK/SHA | Issues |")
    $lines.Add("| --- | --- | --- | --- | --- | --- | --- | --- |")
    foreach ($row in $rows) {
        $bodyState = "$($row.BodyNamesApk)/$($row.BodyNamesSha)"
        $lines.Add(('| {0} | `{1}` | `{2}` | `{3}` | {4} | {5} | {6} | {7} |' -f $row.Published, $row.Tag, $row.Class, $row.Apk, $row.Checksum, $row.Metadata, $bodyState, $row.Issues))
    }
    $lines.Add("")
    $lines.Add("## Required Hygiene For New Releases")
    $lines.Add("")
    $lines.Add('- Use explicit fork repo arguments: `--repo SocialHummingbird/StS2-Launcher-Overhaul`.')
    $lines.Add('- Attach exactly named APK assets, for example `StS2Launcher-v<version>-arm64-v8a.apk`.')
    $lines.Add('- Attach a sha256sum-compatible sidecar named `<apk>.sha256` containing the APK filename, not a runner absolute path.')
    $lines.Add('- Attach metadata as `<apk>.json` for local builds or `<apk>.build-info.txt` for GitHub Actions builds.')
    $lines.Add("- Include APK name, SHA-256, package name, versionName, versionCode, signing channel, validation, and limitations in the release body.")
    $lines.Add('- Run `scripts/check-github-release-hygiene.ps1` before public announcements.')

    $lines | Set-Content -LiteralPath $resolvedOutputPath -Encoding UTF8
    Write-Host "Wrote release inventory: $resolvedOutputPath"
}
