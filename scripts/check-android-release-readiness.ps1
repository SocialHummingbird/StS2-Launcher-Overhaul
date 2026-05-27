param(
    [string]$Repo = "SocialHummingbird/StS2-Launcher-Overhaul",
    [string]$ExpectedPackageName = "com.sts2launcher.overhaul.fork.dev"
)

$ErrorActionPreference = "Stop"

if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    throw "GitHub CLI not found: gh"
}

$requiredSecrets = @(
    "ANDROID_RELEASE_KEYSTORE_BASE64",
    "ANDROID_RELEASE_KEYSTORE_PASSWORD",
    "ANDROID_RELEASE_KEY_ALIAS"
)
$requiredVariables = @(
    "ANDROID_RELEASE_SIGNER_SHA256"
)

$failed = $false

Write-Host "Checking Android release readiness for $Repo"

$secretOutput = gh secret list --repo $Repo 2>&1
if ($LASTEXITCODE -ne 0) {
    throw "Failed to list GitHub secrets for $Repo.`n$($secretOutput -join "`n")"
}

$secretNames = @($secretOutput | ForEach-Object { ($_ -split "\s+")[0] } | Where-Object { $_ })
foreach ($secret in $requiredSecrets) {
    if ($secretNames -contains $secret) {
        Write-Host "OK secret present: $secret"
    } else {
        Write-Host "MISSING secret: $secret"
        $failed = $true
    }
}

$variableOutput = gh variable list --repo $Repo 2>&1
if ($LASTEXITCODE -ne 0) {
    throw "Failed to list GitHub variables for $Repo.`n$($variableOutput -join "`n")"
}

$variables = @{}
foreach ($line in $variableOutput) {
    $parts = $line -split "\s+", 2
    if ($parts.Length -ge 2 -and $parts[0]) {
        $variables[$parts[0]] = $parts[1].Trim()
    }
}

foreach ($variable in $requiredVariables) {
    if ($variables.ContainsKey($variable) -and $variables[$variable]) {
        Write-Host "OK variable present: $variable=$($variables[$variable])"
    } else {
        Write-Host "MISSING variable: $variable"
        $failed = $true
    }
}

$releaseJson = gh api "repos/$Repo/releases?per_page=100" 2>&1
if ($LASTEXITCODE -ne 0) {
    throw "Failed to list GitHub releases for $Repo.`n$($releaseJson -join "`n")"
}

$parsedReleases = ($releaseJson -join "`n") | ConvertFrom-Json
$releases = @($parsedReleases)
$baselineFound = $false
foreach ($release in $releases) {
    if ($release.draft) {
        continue
    }

    $assets = @($release.assets | Where-Object { $_.name -match "^StS2Launcher-v.*\.apk$" })
    if ($assets.Count -gt 0) {
        $baselineFound = $true
        Write-Host "OK previous APK baseline found: $($release.tag_name)/$($assets[0].name)"
        break
    }
}

if (-not $baselineFound) {
    Write-Host "MISSING previous APK baseline release. First stable-key release must use allow_update_baseline_reset=true."
    $failed = $true
}

Write-Host "Expected package name for GitHub releases: $ExpectedPackageName"

if ($failed) {
    throw "Android release path is not ready to guarantee direct updates."
}

Write-Host "Android release path is ready for update-compatible GitHub releases."
