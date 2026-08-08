param(
    [string]$Repo = "SocialHummingbird/StS2-Launcher-Overhaul",
    [string]$AndroidHome = "",
    [string]$JavaHome = ""
)

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "android-signing-utils.ps1")

$ExpectedPackageName = "com.sts2launcher.overhaul.fork.local"
$UpdateBaselineTag = "v0.2.416-startup-recovery-ime"
$UpdateBaselineAssetName = "StS2Launcher-v0.2.416-startup-recovery-ime-local-arm64-v8a.apk"
$UpdateBaselineApkSha256 = "fdf2dcfcf2352d0e1a370da76922fb5b70cee3654d98c5fe9afbbd39554fc17b"
$SigningEnvironment = "android-local-signing"
$RequiredReleaseBranch = "main"

$ghCommand = Get-Command gh -ErrorAction SilentlyContinue
if (-not $ghCommand) {
    throw "GitHub CLI not found: gh"
}

function Invoke-GitHubRead {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    $previousErrorActionPreference = $ErrorActionPreference
    try {
        # Windows PowerShell promotes native stderr to ErrorRecord objects.
        # Keep it capturable so the caller can report the actual failed read.
        $ErrorActionPreference = "Continue"
        $output = @(& $ghCommand.Source @Arguments 2>&1)
        $exitCode = $LASTEXITCODE
    } finally {
        $ErrorActionPreference = $previousErrorActionPreference
    }

    return [pscustomobject]@{
        ExitCode = $exitCode
        Output = @($output)
    }
}

$requiredSecrets = @(
    "ANDROID_LOCAL_UPDATE_KEYSTORE_BASE64",
    "ANDROID_LOCAL_UPDATE_KEYSTORE_PASSWORD",
    "ANDROID_LOCAL_UPDATE_KEY_ALIAS"
)
$requiredVariables = @(
    "ANDROID_LOCAL_UPDATE_SIGNER_SHA256"
)

$failed = $false

Write-Host "Checking Android release readiness for $Repo"

$environmentRead = Invoke-GitHubRead -Arguments @(
    'api', '--method', 'GET', "repos/$Repo/environments/$SigningEnvironment"
)
if ($environmentRead.ExitCode -ne 0) {
    throw "Required GitHub signing environment '$SigningEnvironment' is missing or inaccessible for $Repo. No signing credentials were read.`n$($environmentRead.Output -join "`n")"
}
try {
    $environmentMetadata = ($environmentRead.Output -join "`n") | ConvertFrom-Json
} catch {
    throw "GitHub returned invalid metadata for required signing environment '$SigningEnvironment'. No signing credentials were read."
}
if ([string]$environmentMetadata.name -cne $SigningEnvironment) {
    throw "GitHub returned the wrong signing environment. Expected '$SigningEnvironment', got '$([string]$environmentMetadata.name)'. No signing credentials were read."
}
$deploymentPolicy = $environmentMetadata.deployment_branch_policy
if (-not $deploymentPolicy -or
    $deploymentPolicy.protected_branches -or
    -not $deploymentPolicy.custom_branch_policies) {
    throw "GitHub environment '$SigningEnvironment' must use a custom deployment policy restricted to '$RequiredReleaseBranch'. No signing credentials were read."
}

$branchPolicyRead = Invoke-GitHubRead -Arguments @(
    'api', '--method', 'GET',
    "repos/$Repo/environments/$SigningEnvironment/deployment-branch-policies?per_page=100"
)
if ($branchPolicyRead.ExitCode -ne 0) {
    throw "Could not verify deployment branch policies for GitHub environment '$SigningEnvironment'. No signing credentials were read.`n$($branchPolicyRead.Output -join "`n")"
}
try {
    $branchPolicyMetadata = ($branchPolicyRead.Output -join "`n") | ConvertFrom-Json
} catch {
    throw "GitHub returned invalid deployment-policy metadata for environment '$SigningEnvironment'. No signing credentials were read."
}
$branchPolicies = @($branchPolicyMetadata.branch_policies)
if ($branchPolicyMetadata.total_count -ne 1 -or
    $branchPolicies.Count -ne 1 -or
    $branchPolicies[0].name -cne $RequiredReleaseBranch -or
    $branchPolicies[0].type -cne "branch") {
    throw "GitHub environment '$SigningEnvironment' must allow exactly the '$RequiredReleaseBranch' branch and no tag or wildcard policy. No signing credentials were read."
}
Write-Host "OK main-only GitHub signing environment: $SigningEnvironment (branch=$RequiredReleaseBranch)"

$secretRead = Invoke-GitHubRead -Arguments @(
    'secret', 'list', '--env', $SigningEnvironment, '--repo', $Repo
)
if ($secretRead.ExitCode -ne 0) {
    throw "Failed to list secret names in GitHub environment '$SigningEnvironment' for $Repo. No secret values were read.`n$($secretRead.Output -join "`n")"
}

$secretNames = @($secretRead.Output | ForEach-Object { ($_ -split "\s+")[0] } | Where-Object { $_ })
foreach ($secret in $requiredSecrets) {
    if ($secretNames -contains $secret) {
        Write-Host "OK secret present: $secret"
    } else {
        Write-Host "MISSING secret: $secret"
        $failed = $true
    }
}

$variableRead = Invoke-GitHubRead -Arguments @(
    'variable', 'list', '--env', $SigningEnvironment, '--repo', $Repo
)
if ($variableRead.ExitCode -ne 0) {
    throw "Failed to list GitHub variables in environment '$SigningEnvironment' for $Repo.`n$($variableRead.Output -join "`n")"
}

$variables = @{}
foreach ($line in $variableRead.Output) {
    $parts = @($line -split "\s+")
    if ($parts.Count -ge 2 -and $parts[0]) {
        $variables[$parts[0]] = $parts[1]
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

$expectedSignerSha256 = ""
if ($variables.ContainsKey("ANDROID_LOCAL_UPDATE_SIGNER_SHA256")) {
    $expectedSignerSha256 = Normalize-Sha256 $variables["ANDROID_LOCAL_UPDATE_SIGNER_SHA256"]
    if ($expectedSignerSha256 -notmatch "^[A-F0-9]{64}$") {
        Write-Host "INVALID variable: ANDROID_LOCAL_UPDATE_SIGNER_SHA256 must contain one SHA-256 fingerprint."
        $failed = $true
        $expectedSignerSha256 = ""
    }
}

if (-not $UpdateBaselineTag -or -not $UpdateBaselineAssetName) {
    throw "UpdateBaselineTag and UpdateBaselineAssetName must identify one explicit published .local APK baseline."
}

$expectedBaselineSha256 = Normalize-Sha256 $UpdateBaselineApkSha256
if ($expectedBaselineSha256 -notmatch "^[A-F0-9]{64}$") {
    throw "UpdateBaselineApkSha256 must be a 64-character SHA-256 digest."
}

$releaseOutput = gh release view $UpdateBaselineTag --repo $Repo --json tagName,assets 2>&1
if ($LASTEXITCODE -ne 0) {
    throw "Failed to inspect update baseline $UpdateBaselineTag in $Repo.`n$($releaseOutput -join "`n")"
}

$release = ($releaseOutput -join "`n") | ConvertFrom-Json
$baselineAssets = @($release.assets | Where-Object { $_.name -eq $UpdateBaselineAssetName })
if ($baselineAssets.Count -ne 1) {
    throw "Expected exactly one update-baseline asset named '$UpdateBaselineAssetName' in $UpdateBaselineTag; found $($baselineAssets.Count)."
}

$reportedDigest = [string]$baselineAssets[0].digest
if ($reportedDigest -match "^sha256:([A-Fa-f0-9]{64})$") {
    $reportedSha256 = Normalize-Sha256 $Matches[1]
    if ($reportedSha256 -ne $expectedBaselineSha256) {
        throw "GitHub reports SHA-256 $reportedSha256 for $UpdateBaselineTag/$UpdateBaselineAssetName, not configured digest $expectedBaselineSha256."
    }
}

$temporaryParent = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$baselineDirectory = [IO.Path]::GetFullPath(
    (Join-Path $temporaryParent ("sts2-android-readiness-{0}" -f [Guid]::NewGuid().ToString("N")))
)
$temporaryPrefix = $temporaryParent.TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
if (-not $baselineDirectory.StartsWith($temporaryPrefix, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Refusing to use temporary baseline directory outside the system temporary root: $baselineDirectory"
}

try {
    $null = New-Item -ItemType Directory -Path $baselineDirectory
    $downloadOutput = gh release download $UpdateBaselineTag `
        --repo $Repo `
        --pattern $UpdateBaselineAssetName `
        --dir $baselineDirectory 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to download update baseline $UpdateBaselineTag/$UpdateBaselineAssetName.`n$($downloadOutput -join "`n")"
    }

    $baselineApkPath = Join-Path $baselineDirectory $UpdateBaselineAssetName
    if (-not (Test-Path -LiteralPath $baselineApkPath -PathType Leaf)) {
        throw "Downloaded update-baseline APK is missing: $baselineApkPath"
    }

    $actualBaselineSha256 = (Get-FileHash -LiteralPath $baselineApkPath -Algorithm SHA256).Hash.ToUpperInvariant()
    if ($actualBaselineSha256 -ne $expectedBaselineSha256) {
        throw "Downloaded update-baseline APK SHA-256 mismatch. Expected $expectedBaselineSha256, got $actualBaselineSha256."
    }

    $userHome = if ($env:USERPROFILE) { $env:USERPROFILE } else { $HOME }
    $androidCandidates = if ($AndroidHome) {
        @($AndroidHome)
    } else {
        @(
            $env:ANDROID_HOME,
            $env:ANDROID_SDK_ROOT,
            (Join-Path $userHome ".w40k-android-toolchain\android-sdk"),
            (Join-Path $userHome "AppData\Local\Android\Sdk"),
            (Join-Path $userHome "Android/Sdk")
        )
    }
    $resolvedAndroidHome = $androidCandidates |
        Where-Object { $_ -and (Test-Path -LiteralPath (Join-Path $_ "build-tools") -PathType Container) } |
        ForEach-Object { (Resolve-Path -LiteralPath $_).Path } |
        Select-Object -First 1
    if (-not $resolvedAndroidHome) {
        throw "Android SDK build-tools not found. Pass -AndroidHome or install the repo toolchain under .w40k-android-toolchain."
    }

    $runningOnWindows = $env:OS -eq "Windows_NT"
    $javaExecutableName = if ($runningOnWindows) { "java.exe" } else { "java" }
    $javaCandidates = if ($JavaHome) {
        @($JavaHome)
    } else {
        @(
            $env:JAVA_HOME,
            (Join-Path $userHome ".w40k-android-toolchain\jdk-17"),
            (Join-Path $userHome ".w40k-android-toolchain\jdk-21")
        )
    }
    $resolvedJavaHome = $javaCandidates |
        Where-Object { $_ -and (Test-Path -LiteralPath (Join-Path $_ "bin\$javaExecutableName") -PathType Leaf) } |
        ForEach-Object { (Resolve-Path -LiteralPath $_).Path } |
        Select-Object -First 1
    if (-not $resolvedJavaHome) {
        throw "JDK not found. Pass -JavaHome or install the repo JDK under .w40k-android-toolchain."
    }

    $androidSdkRootWasPresent = Test-Path Env:ANDROID_SDK_ROOT
    $javaHomeWasPresent = Test-Path Env:JAVA_HOME
    $previousAndroidSdkRoot = $env:ANDROID_SDK_ROOT
    $previousJavaHome = $env:JAVA_HOME
    try {
        $env:ANDROID_SDK_ROOT = $resolvedAndroidHome
        $env:JAVA_HOME = $resolvedJavaHome
        $aaptName = if ($runningOnWindows) { "aapt.exe" } else { "aapt" }
        $apkSignerName = if ($runningOnWindows) { "apksigner.bat" } else { "apksigner" }
        $aaptPath = Resolve-AndroidBuildTool $aaptName
        $apkSignerPath = Resolve-AndroidBuildTool $apkSignerName
        $baselineIdentity = Get-AndroidApkIdentity `
            -Path $baselineApkPath `
            -Aapt $aaptPath `
            -ApkSigner $apkSignerPath
    } finally {
        if ($androidSdkRootWasPresent) {
            $env:ANDROID_SDK_ROOT = $previousAndroidSdkRoot
        } else {
            Remove-Item Env:ANDROID_SDK_ROOT -ErrorAction SilentlyContinue
        }
        if ($javaHomeWasPresent) {
            $env:JAVA_HOME = $previousJavaHome
        } else {
            Remove-Item Env:JAVA_HOME -ErrorAction SilentlyContinue
        }
    }
    if ($baselineIdentity.packageName -ne $ExpectedPackageName) {
        Write-Host "INVALID update baseline package: expected $ExpectedPackageName, got $($baselineIdentity.packageName)"
        $failed = $true
    } else {
        Write-Host "OK update baseline package: $($baselineIdentity.packageName)"
    }

    if (-not $expectedSignerSha256) {
        Write-Host "UNVERIFIED update baseline signer: ANDROID_LOCAL_UPDATE_SIGNER_SHA256 is unavailable or invalid."
        $failed = $true
    } elseif ($baselineIdentity.signerSha256 -ne $expectedSignerSha256) {
        Write-Host "INVALID update baseline signer: expected $expectedSignerSha256, got $($baselineIdentity.signerSha256)"
        $failed = $true
    } else {
        Write-Host "OK update baseline signer: $($baselineIdentity.signerSha256)"
    }

    Write-Host "OK update baseline bytes: $UpdateBaselineTag/$UpdateBaselineAssetName sha256=$actualBaselineSha256"
} finally {
    if (Test-Path -LiteralPath $baselineDirectory) {
        Remove-Item -LiteralPath $baselineDirectory -Recurse -Force
    }
}

Write-Host "Expected package name for candidate and update baseline: $ExpectedPackageName"

if ($failed) {
    throw "Android release path is not ready to guarantee a v0.2.416 .local update-compatible candidate."
}

Write-Host "Android release path is ready for a signed v0.2.416 .local update-compatible candidate."
