param(
    [string]$Repo = "SocialHummingbird/StS2-Launcher-Overhaul",
    [string]$ReleaseTag = "latest",
    [string]$AssetName = "",
    [switch]$AllowMultipleApks,
    [switch]$SkipReleaseBodyChecks
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

function Resolve-Release {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Repository,
        [Parameter(Mandatory = $true)]
        [string]$Tag
    )

    if ($Tag -and $Tag -ne "latest") {
        return Invoke-GhJson -Arguments @(
            "release",
            "view",
            $Tag,
            "--repo",
            $Repository,
            "--json",
            "tagName,name,isDraft,isPrerelease,publishedAt,assets,body"
        )
    }

    $releases = Invoke-GhJson -Arguments @(
        "api",
        "repos/$Repository/releases?per_page=100"
    )
    $latest = @($releases | Where-Object { -not $_.draft } | Select-Object -First 1)
    if (-not $latest) {
        throw "No non-draft GitHub release found for $Repository"
    }

    return Invoke-GhJson -Arguments @(
        "release",
        "view",
        $latest.tag_name,
        "--repo",
        $Repository,
        "--json",
        "tagName,name,isDraft,isPrerelease,publishedAt,assets,body"
    )
}

function Require-Condition {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

function Get-AssetByName {
    param(
        [Parameter(Mandatory = $true)]
        $Release,
        [Parameter(Mandatory = $true)]
        [string]$Name
    )

    return @($Release.assets | Where-Object { $_.name -eq $Name } | Select-Object -First 1)
}

function Read-ReleaseAssetText {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Repository,
        [Parameter(Mandatory = $true)]
        [string]$Tag,
        [Parameter(Mandatory = $true)]
        [string]$Name
    )

    $tempDir = Join-Path ([System.IO.Path]::GetTempPath()) ("sts2-release-hygiene-" + [System.Guid]::NewGuid().ToString("N"))
    New-Item -ItemType Directory -Force $tempDir | Out-Null
    try {
        $downloadOutput = & gh release download $Tag --repo $Repository --pattern $Name --dir $tempDir --clobber 2>&1
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to download release asset $Tag/$Name from $Repository.`n$($downloadOutput -join "`n")"
        }

        $path = Join-Path $tempDir $Name
        if (-not (Test-Path -LiteralPath $path)) {
            throw "Downloaded release asset was not found on disk: $path"
        }

        return Get-Content -Raw -LiteralPath $path
    } finally {
        if (Test-Path -LiteralPath $tempDir) {
            Remove-Item -LiteralPath $tempDir -Recurse -Force
        }
    }
}

function Normalize-AssetDigest {
    param([string]$Digest)

    if (-not $Digest) {
        return ""
    }

    if ($Digest.StartsWith("sha256:", [System.StringComparison]::OrdinalIgnoreCase)) {
        return $Digest.Substring("sha256:".Length).ToLowerInvariant()
    }

    return $Digest.ToLowerInvariant()
}

function Get-VersionNameFromAsset {
    param([string]$Name)

    $match = [regex]::Match($Name, "^StS2Launcher-v(.+)-(arm64-v8a|x86_64|universal)\.apk$")
    if (-not $match.Success) {
        return ""
    }

    return $match.Groups[1].Value
}

$release = Resolve-Release -Repository $Repo -Tag $ReleaseTag
Require-Condition (-not $release.isDraft) "Release is still a draft: $($release.tagName)"

$apkAssets = @($release.assets | Where-Object { $_.name -match "^StS2Launcher-v.*\.apk$" })
if ($AssetName) {
    $apkAssets = @($apkAssets | Where-Object { $_.name -eq $AssetName })
    Require-Condition ($apkAssets.Count -eq 1) "Requested APK asset was not found: $AssetName"
} else {
    Require-Condition ($apkAssets.Count -ge 1) "No StS2Launcher-v*.apk assets found on release $($release.tagName)"
}

if (-not $AllowMultipleApks) {
    Require-Condition ($apkAssets.Count -eq 1) "Expected exactly one APK asset. Found $($apkAssets.Count). Pass -AllowMultipleApks for multi-ABI releases."
}

Write-Host "Checking GitHub release hygiene for $Repo $($release.tagName)"
Write-Host "Release name: $($release.name)"
Write-Host "Published: $($release.publishedAt)"
Write-Host "Prerelease: $($release.isPrerelease)"

foreach ($apk in $apkAssets) {
    $apkName = $apk.name
    $versionName = Get-VersionNameFromAsset $apkName
    Require-Condition (-not [string]::IsNullOrWhiteSpace($versionName)) "APK name must include an explicit ABI suffix: $apkName"

    $shaAssetName = "$apkName.sha256"
    $jsonAssetName = "$apkName.json"
    $buildInfoAssetName = "$apkName.build-info.txt"
    $shaAsset = Get-AssetByName -Release $release -Name $shaAssetName
    $jsonAsset = Get-AssetByName -Release $release -Name $jsonAssetName
    $buildInfoAsset = Get-AssetByName -Release $release -Name $buildInfoAssetName
    $digest = Normalize-AssetDigest $apk.digest

    Require-Condition ($null -ne $shaAsset) "Missing checksum sidecar asset: $shaAssetName"
    Require-Condition (($null -ne $jsonAsset) -or ($null -ne $buildInfoAsset)) "Missing metadata sidecar asset: expected $jsonAssetName or $buildInfoAssetName"
    Require-Condition (-not [string]::IsNullOrWhiteSpace($digest)) "GitHub did not expose a SHA-256 digest for APK asset: $apkName"

    $shaText = Read-ReleaseAssetText -Repository $Repo -Tag $release.tagName -Name $shaAssetName
    $shaLine = @($shaText -split "`r?`n" | Where-Object { $_.Trim() } | Select-Object -First 1)
    Require-Condition (-not [string]::IsNullOrWhiteSpace($shaLine)) "Checksum sidecar is empty: $shaAssetName"
    $shaMatch = [regex]::Match($shaLine, "^(?<sha>[a-fA-F0-9]{64})\s+\*?(?<file>\S+)\s*$")
    Require-Condition $shaMatch.Success "Checksum sidecar must be sha256sum-compatible: $shaAssetName"
    $sidecarSha = $shaMatch.Groups["sha"].Value.ToLowerInvariant()
    $sidecarFile = $shaMatch.Groups["file"].Value
    Require-Condition ($sidecarFile -eq $apkName) "Checksum sidecar filename mismatch. Expected $apkName, got $sidecarFile"
    Require-Condition ($sidecarSha -eq $digest) "Checksum sidecar SHA mismatch for $apkName. Asset digest is $digest, sidecar is $sidecarSha"

    if ($jsonAsset) {
        $jsonText = Read-ReleaseAssetText -Repository $Repo -Tag $release.tagName -Name $jsonAssetName
        $metadata = $jsonText | ConvertFrom-Json
        Require-Condition ($metadata.apk -eq $apkName) "Metadata apk mismatch. Expected $apkName, got $($metadata.apk)"
        Require-Condition ($metadata.sha256 -eq $digest) "Metadata sha256 mismatch for $apkName"
        Require-Condition ($metadata.versionName -eq $versionName) "Metadata versionName mismatch. Asset implies $versionName, metadata has $($metadata.versionName)"
        Require-Condition (($metadata.versionCode -as [int]) -gt 0) "Metadata versionCode is missing or invalid for $apkName"
        Require-Condition (-not [string]::IsNullOrWhiteSpace($metadata.packageName)) "Metadata packageName is missing for $apkName"
        Require-Condition (-not [string]::IsNullOrWhiteSpace($metadata.signingChannel)) "Metadata signingChannel is missing for $apkName"
        Require-Condition ($metadata.abi -match "^(arm64-v8a|x86_64|universal)$") "Metadata abi is missing or invalid for $apkName"
        Write-Host "OK metadata: package=$($metadata.packageName) versionCode=$($metadata.versionCode) signing=$($metadata.signingChannel) abi=$($metadata.abi)"
    }

    if ($buildInfoAsset) {
        $buildInfoText = Read-ReleaseAssetText -Repository $Repo -Tag $release.tagName -Name $buildInfoAssetName
        $buildInfo = @{}
        foreach ($line in ($buildInfoText -split "`r?`n")) {
            $index = $line.IndexOf("=")
            if ($index -le 0) {
                continue
            }
            $key = $line.Substring(0, $index).Trim()
            $value = $line.Substring($index + 1).Trim()
            if ($key) {
                $buildInfo[$key] = $value
            }
        }

        Require-Condition ($buildInfo["version_name"] -eq $versionName) "Build info version_name mismatch. Asset implies $versionName, build info has $($buildInfo["version_name"])"
        Require-Condition (($buildInfo["version_code"] -as [int]) -gt 0) "Build info version_code is missing or invalid for $apkName"
        Require-Condition (-not [string]::IsNullOrWhiteSpace($buildInfo["package_name"])) "Build info package_name is missing for $apkName"
        Require-Condition (-not [string]::IsNullOrWhiteSpace($buildInfo["signing_channel"])) "Build info signing_channel is missing for $apkName"
        Write-Host "OK build info: package=$($buildInfo["package_name"]) versionCode=$($buildInfo["version_code"]) signing=$($buildInfo["signing_channel"])"
    }

    if (-not $SkipReleaseBodyChecks) {
        $body = [string]$release.body
        Require-Condition ($body.Contains($apkName)) "Release body does not name APK asset: $apkName"
        Require-Condition ($body.Contains($digest)) "Release body does not include APK SHA-256: $digest"
        if ($jsonAsset -and $metadata.packageName) {
            Require-Condition ($body.Contains($metadata.packageName)) "Release body does not include package name: $($metadata.packageName)"
        }
        if ($buildInfoAsset -and $buildInfo["package_name"]) {
            Require-Condition ($body.Contains($buildInfo["package_name"])) "Release body does not include package name: $($buildInfo["package_name"])"
        }
    }

    Write-Host "OK APK hygiene: $apkName sha256=$digest"
}

Write-Host "GitHub release hygiene check passed: $Repo $($release.tagName)"
