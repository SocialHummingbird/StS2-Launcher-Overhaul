param()

$ErrorActionPreference = "Stop"

$root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$workflowPath = Join-Path $root ".github\workflows\android-release.yml"
$readinessPath = Join-Path $root "scripts\check-android-release-readiness.ps1"
$configuratorPath = Join-Path $root "scripts\configure-android-release-signing.ps1"
$collectorPath = Join-Path $root "scripts\collect-android-save-validation.ps1"
$matrixReviewerPath = Join-Path $root "scripts\review-stage5-physical-matrix.ps1"
$matrixTemplatePath = Join-Path $root "scripts\new-stage5-physical-matrix-template.ps1"

if (-not (Test-Path -LiteralPath $workflowPath -PathType Leaf)) {
    throw "Android release-candidate workflow is missing: $workflowPath"
}
if (-not (Test-Path -LiteralPath $readinessPath -PathType Leaf)) {
    throw "Android release-readiness check is missing: $readinessPath"
}
if (-not (Test-Path -LiteralPath $configuratorPath -PathType Leaf)) {
    throw "Android release-signing configurator is missing: $configuratorPath"
}
if (-not (Test-Path -LiteralPath $collectorPath -PathType Leaf)) {
    throw "Android Stage 5 evidence collector is missing: $collectorPath"
}
if (-not (Test-Path -LiteralPath $matrixReviewerPath -PathType Leaf)) {
    throw "Android Stage 5 physical matrix reviewer is missing: $matrixReviewerPath"
}
if (-not (Test-Path -LiteralPath $matrixTemplatePath -PathType Leaf)) {
    throw "Android Stage 5 physical matrix template is missing: $matrixTemplatePath"
}

$workflow = Get-Content -LiteralPath $workflowPath -Raw
$readiness = Get-Content -LiteralPath $readinessPath -Raw
$configurator = Get-Content -LiteralPath $configuratorPath -Raw
$matrixReviewer = Get-Content -LiteralPath $matrixReviewerPath -Raw
$matrixTemplate = Get-Content -LiteralPath $matrixTemplatePath -Raw

function Require-Pattern {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Description,

        [Parameter(Mandatory = $true)]
        [string]$Pattern
    )

    if ($workflow -notmatch $Pattern) {
        throw "Stage 5 release gate is missing: $Description"
    }
}

function Reject-Pattern {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Description,

        [Parameter(Mandatory = $true)]
        [string]$Pattern
    )

    if ($workflow -match $Pattern) {
        throw "Stage 5 release gate was bypassed: $Description"
    }
}

function Require-ReadinessPattern {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Description,

        [Parameter(Mandatory = $true)]
        [string]$Pattern
    )

    if ($readiness -notmatch $Pattern) {
        throw "Stage 5 release readiness is missing: $Description"
    }
}

function Reject-ReadinessPattern {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Description,

        [Parameter(Mandatory = $true)]
        [string]$Pattern
    )

    if ($readiness -match $Pattern) {
        throw "Stage 5 release readiness was bypassed: $Description"
    }
}

function Require-ConfiguratorPattern {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Description,

        [Parameter(Mandatory = $true)]
        [string]$Pattern
    )

    if ($configurator -notmatch $Pattern) {
        throw "Stage 5 signing configurator is missing: $Description"
    }
}

function Reject-ConfiguratorPattern {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Description,

        [Parameter(Mandatory = $true)]
        [string]$Pattern
    )

    if ($configurator -match $Pattern) {
        throw "Stage 5 signing configurator targets the wrong lineage: $Description"
    }
}

function Require-StaticPattern {
    param(
        [Parameter(Mandatory = $true)][string]$Content,
        [Parameter(Mandatory = $true)][string]$Subject,
        [Parameter(Mandatory = $true)][string]$Description,
        [Parameter(Mandatory = $true)][string]$Pattern
    )

    if ($Content -notmatch $Pattern) {
        throw "Stage 5 $Subject is missing: $Description"
    }
}

Require-Pattern `
    -Description "candidate-only workflow identity" `
    -Pattern '(?m)^name:\s*Android Release Candidate\s*$'
Require-Pattern `
    -Description "candidate source commit binding" `
    -Pattern 'source_commit=\$\{GITHUB_SHA\}'
Require-Pattern `
    -Description "candidate workflow-run binding" `
    -Pattern 'candidate_run_id=\$\{GITHUB_RUN_ID\}'
Require-Pattern `
    -Description "candidate workflow-run attempt binding" `
    -Pattern 'candidate_run_attempt=\$\{GITHUB_RUN_ATTEMPT\}'
Require-Pattern `
    -Description "candidate ABI binding" `
    -Pattern 'echo "abi=arm64-v8a"'
Require-Pattern `
    -Description "candidate APK byte binding" `
    -Pattern 'apk_sha256=\$\{APK_SHA\}'
Require-Pattern `
    -Description "candidate actual signer binding" `
    -Pattern 'signer_sha256=\$\{ACTUAL_SIGNER_SHA256\}'
Require-Pattern `
    -Description "actual candidate signer verification" `
    -Pattern 'ACTUAL_SIGNER_SHA256" != "\$EXPECTED_SIGNER_NORMALIZED'
Require-Pattern `
    -Description "fixed published .local package" `
    -Pattern 'PACKAGE_NAME="com\.sts2launcher\.overhaul\.fork\.local"'
Require-Pattern `
    -Description "default versionCode newer than the pinned v0.2.416 baseline" `
    -Pattern '(?ms)version_code:\s*.*?required:\s*true\s*.*?default:\s*"417003"'
Require-Pattern `
    -Description "early pinned-baseline versionCode rejection" `
    -Pattern 'VERSION_CODE <= 416001'
Require-Pattern `
    -Description "fixed v0.2.416 update-baseline tag" `
    -Pattern 'UPDATE_BASELINE_TAG:\s*v0\.2\.416-startup-recovery-ime'
Require-Pattern `
    -Description "fixed v0.2.416 update-baseline asset" `
    -Pattern 'UPDATE_BASELINE_ASSET_NAME:\s*StS2Launcher-v0\.2\.416-startup-recovery-ime-local-arm64-v8a\.apk'
Require-Pattern `
    -Description "fixed v0.2.416 update-baseline bytes" `
    -Pattern 'UPDATE_BASELINE_APK_SHA256:\s*fdf2dcfcf2352d0e1a370da76922fb5b70cee3654d98c5fe9afbbd39554fc17b'
Require-Pattern `
    -Description "dedicated local-update keystore secret" `
    -Pattern 'secrets\.ANDROID_LOCAL_UPDATE_KEYSTORE_BASE64'
Require-Pattern `
    -Description "dedicated local-update password and alias secrets" `
    -Pattern '(?s)secrets\.ANDROID_LOCAL_UPDATE_KEYSTORE_PASSWORD.*?secrets\.ANDROID_LOCAL_UPDATE_KEY_ALIAS'
Require-Pattern `
    -Description "dedicated local-update signer variable" `
    -Pattern 'vars\.ANDROID_LOCAL_UPDATE_SIGNER_SHA256'
Require-Pattern `
    -Description "quoted workflow-dispatch metadata input environment" `
    -Pattern '(?ms)RELEASE_TAG_INPUT:\s*"\$\{\{\s*github\.event\.inputs\.release_tag\s*\}\}".*?VERSION_NAME_INPUT:\s*"\$\{\{\s*github\.event\.inputs\.version_name\s*\}\}".*?VERSION_CODE_INPUT:\s*"\$\{\{\s*github\.event\.inputs\.version_code\s*\}\}".*?RELEASE_TAG="\$\{RELEASE_TAG_INPUT\}".*?VERSION_NAME="\$\{VERSION_NAME_INPUT\}".*?VERSION_CODE="\$\{VERSION_CODE_INPUT\}"'
Require-Pattern `
    -Description "quoted workflow-dispatch dependency input environment" `
    -Pattern '(?ms)UPSTREAM_TAG_INPUT:\s*"\$\{\{\s*github\.event\.inputs\.upstream_release_tag\s*\}\}".*?UPSTREAM_APK_SHA256_INPUT:\s*"\$\{\{\s*github\.event\.inputs\.upstream_release_apk_sha256\s*\}\}".*?STS2_SOURCE_INPUT:\s*"\$\{\{\s*github\.event\.inputs\.sts2_dll_source_url\s*\}\}".*?STS2_SOURCE_SHA256_INPUT:\s*"\$\{\{\s*github\.event\.inputs\.sts2_dll_source_sha256\s*\}\}".*?UPSTREAM_TAG="\$\{UPSTREAM_TAG_INPUT\}".*?UPSTREAM_APK_SHA256="\$\{UPSTREAM_APK_SHA256_INPUT\}".*?STS2_SOURCE="\$\{STS2_SOURCE_INPUT\}".*?STS2_SOURCE_SHA256="\$\{STS2_SOURCE_SHA256_INPUT\}"'
Require-Pattern `
    -Description "signing secrets forwarded through the signing-step environment" `
    -Pattern '(?ms)KEYSTORE_BASE64:\s*"\$\{\{\s*secrets\.ANDROID_LOCAL_UPDATE_KEYSTORE_BASE64\s*\}\}".*?KEYSTORE_PASSWORD:\s*"\$\{\{\s*secrets\.ANDROID_LOCAL_UPDATE_KEYSTORE_PASSWORD\s*\}\}".*?KEYSTORE_ALIAS:\s*"\$\{\{\s*secrets\.ANDROID_LOCAL_UPDATE_KEY_ALIAS\s*\}\}".*?if \[\[ -n "\$\{KEYSTORE_BASE64\}" && -n "\$\{KEYSTORE_PASSWORD\}" && -n "\$\{KEYSTORE_ALIAS\}" \]\]'
Require-Pattern `
    -Description "signing password and alias passed directly to the build-step environment" `
    -Pattern '(?ms)Build release APK.*?KEYSTORE_PASSWORD:\s*"\$\{\{\s*secrets\.ANDROID_LOCAL_UPDATE_KEYSTORE_PASSWORD\s*\}\}".*?KEYSTORE_ALIAS:\s*"\$\{\{\s*secrets\.ANDROID_LOCAL_UPDATE_KEY_ALIAS\s*\}\}".*?run:\s*\|'
Require-Pattern `
    -Description "pinned upstream 0.2.0 APK bytes" `
    -Pattern '6dddbd6716c3830ec802e2672be2402bd06d4270dcf1bcd72c27e46ca05acf41'
Require-Pattern `
    -Description "pinned v0.2.416 sts2 source APK bytes" `
    -Pattern 'fdf2dcfcf2352d0e1a370da76922fb5b70cee3654d98c5fe9afbbd39554fc17b'
Require-Pattern `
    -Description "explicit v0.2.416 sts2 source APK" `
    -Pattern 'releases/download/v0\.2\.416-startup-recovery-ime/StS2Launcher-v0\.2\.416-startup-recovery-ime-local-arm64-v8a\.apk'
Require-Pattern `
    -Description "downloaded upstream APK hash verification" `
    -Pattern 'ACTUAL_UPSTREAM_APK_SHA256.*UPSTREAM_APK_SHA256'
Require-Pattern `
    -Description "downloaded sts2 source hash verification" `
    -Pattern 'ACTUAL_STS2_SOURCE_SHA256.*STS2_SOURCE_SHA256'
Require-Pattern `
    -Description "downloaded update-baseline hash verification" `
    -Pattern 'ACTUAL_BASELINE_APK_SHA256.*UPDATE_BASELINE_APK_SHA256'
Require-Pattern `
    -Description "retained candidate APK artifact" `
    -Pattern 'actions/upload-artifact@v4'
Require-Pattern `
    -Description "full save-sync and hard-restart gate before candidate build" `
    -Pattern '(?ms)Verify save safety before building candidate.*?test-cloud-sync-production-path\.ps1.*?Build patched Godot Android runtime'
Require-Pattern `
    -Description "byte-evidence tooling gate before candidate build" `
    -Pattern '(?ms)Verify save safety before building candidate.*?test-stage5-save-evidence-manifests\.ps1.*?Build patched Godot Android runtime'
Require-Pattern `
    -Description "read-only repository permission" `
    -Pattern '(?ms)^permissions:\s*\r?\n\s{2}contents:\s*read\s*$'

Reject-Pattern `
    -Description "candidate builds must be manually dispatched only" `
    -Pattern '(?m)^\s{2}(push|pull_request|schedule|workflow_run|repository_dispatch):\s*$'
Reject-Pattern `
    -Description "candidate workflow must not contain a publish job" `
    -Pattern '(?im)^\s{2}[^\r\n]*publish[^\r\n]*:\s*$'
Reject-Pattern `
    -Description "candidate workflow must not invoke GitHub release publication" `
    -Pattern '(?i)(action-gh-release|actions/create-release|release-action|gh\s+release\s+(create|edit|upload|delete))'
Reject-Pattern `
    -Description "candidate workflow must not expose a create-release switch" `
    -Pattern '(?i)create_release'
Reject-Pattern `
    -Description "candidate workflow must not request any write permission" `
    -Pattern '(?m)(^\s+[A-Za-z-]+:\s*write\s*$|^permissions:\s*write-all\s*$)'
Reject-Pattern `
    -Description "candidate workflow must not guess a baseline from a release listing" `
    -Pattern '(?i)releases\?per_page'
Reject-Pattern `
    -Description "candidate workflow must not expose a selectable package" `
    -Pattern '(?ms)^    inputs:\s*\r?\n(?:(?!^permissions:).)*?^\s{6}package_name:|github\.event\.inputs\.package_name'
Reject-Pattern `
    -Description "candidate workflow must not expose baseline overrides" `
    -Pattern '(?ms)^    inputs:\s*\r?\n(?:(?!^permissions:).)*?^\s{6}update_baseline_(tag|asset_name|apk_sha256):|github\.event\.inputs\.update_baseline_'
Reject-Pattern `
    -Description "candidate workflow must not contain a baseline-reset bypass" `
    -Pattern '(?i)allow_update_baseline_reset|update_baseline=reset'
Reject-Pattern `
    -Description "candidate workflow must not use the unrelated release-signing credentials" `
    -Pattern 'ANDROID_RELEASE_'
Reject-Pattern `
    -Description "workflow-dispatch inputs or secrets must not be interpolated into run blocks" `
    -Pattern '(?ms)^[ ]{8}run:[ ]*\|[ ]*\r?\n(?:(?!^[ ]{6}-[ ]).)*\$\{\{[ ]*(?:github\.event\.inputs\.|inputs\.|secrets\.)'
Reject-Pattern `
    -Description "signing password or alias must never be written to GITHUB_OUTPUT" `
    -Pattern '(?im)^(?=[^\r\n]*GITHUB_OUTPUT)(?=[^\r\n]*(?:password|alias))[^\r\n]*$'
Reject-Pattern `
    -Description "signing password or alias must not transit through step outputs" `
    -Pattern 'steps\.signing\.outputs\.(?:password|alias)'

Require-ReadinessPattern `
    -Description "fixed published .local package expectation" `
    -Pattern '\$ExpectedPackageName\s*=\s*"com\.sts2launcher\.overhaul\.fork\.local"'
Require-ReadinessPattern `
    -Description "fixed v0.2.416 baseline selection" `
    -Pattern '\$UpdateBaselineTag\s*=\s*"v0\.2\.416-startup-recovery-ime"'
Require-ReadinessPattern `
    -Description "fixed v0.2.416 baseline asset" `
    -Pattern '\$UpdateBaselineAssetName\s*=\s*"StS2Launcher-v0\.2\.416-startup-recovery-ime-local-arm64-v8a\.apk"'
Require-ReadinessPattern `
    -Description "fixed v0.2.416 baseline bytes" `
    -Pattern '\$UpdateBaselineApkSha256\s*=\s*"fdf2dcfcf2352d0e1a370da76922fb5b70cee3654d98c5fe9afbbd39554fc17b"'
Require-ReadinessPattern `
    -Description "dedicated local-update secrets" `
    -Pattern '(?s)ANDROID_LOCAL_UPDATE_KEYSTORE_BASE64.*?ANDROID_LOCAL_UPDATE_KEYSTORE_PASSWORD.*?ANDROID_LOCAL_UPDATE_KEY_ALIAS'
Require-ReadinessPattern `
    -Description "dedicated local-update signer variable" `
    -Pattern 'ANDROID_LOCAL_UPDATE_SIGNER_SHA256'
Require-ReadinessPattern `
    -Description "actual downloaded baseline identity inspection" `
    -Pattern '(?s)\$baselineIdentity\s*=\s*Get-AndroidApkIdentity.*?-Path\s+\$baselineApkPath'
Require-ReadinessPattern `
    -Description "actual baseline package comparison" `
    -Pattern '\$baselineIdentity\.packageName\s+-ne\s+\$ExpectedPackageName'
Require-ReadinessPattern `
    -Description "actual baseline signer comparison" `
    -Pattern '\$baselineIdentity\.signerSha256\s+-ne\s+\$expectedSignerSha256'
Require-ReadinessPattern `
    -Description "baseline byte comparison" `
    -Pattern '\$actualBaselineSha256\s+-ne\s+\$expectedBaselineSha256'
Require-ReadinessPattern `
    -Description "repo Android SDK fallback" `
    -Pattern '\.w40k-android-toolchain\\android-sdk'
Require-ReadinessPattern `
    -Description "repo JDK fallback" `
    -Pattern '\.w40k-android-toolchain\\jdk-17'
Require-ReadinessPattern `
    -Description "Android SDK environment restoration" `
    -Pattern 'Remove-Item\s+Env:ANDROID_SDK_ROOT'
Require-ReadinessPattern `
    -Description "Java environment restoration" `
    -Pattern 'Remove-Item\s+Env:JAVA_HOME'
Reject-ReadinessPattern `
    -Description "package or baseline must not be caller-overridable" `
    -Pattern '(?m)^\s*\[string\]\$(ExpectedPackageName|UpdateBaselineTag|UpdateBaselineAssetName|UpdateBaselineApkSha256)\b'
Reject-ReadinessPattern `
    -Description "readiness must not accept unrelated release-signing credentials" `
    -Pattern 'ANDROID_RELEASE_'

Require-ConfiguratorPattern `
    -Description "published v0.2.416 signer pin" `
    -Pattern 'FD0E3D5ACF435C1D23BFC5C426E99AA9EB5808619FF1FC214FFCA99CFAC7E57A'
Require-ConfiguratorPattern `
    -Description "dedicated local-update secret writes" `
    -Pattern '(?s)secret set ANDROID_LOCAL_UPDATE_KEYSTORE_BASE64.*?secret set ANDROID_LOCAL_UPDATE_KEYSTORE_PASSWORD.*?secret set ANDROID_LOCAL_UPDATE_KEY_ALIAS'
Require-ConfiguratorPattern `
    -Description "dedicated local-update signer-variable write" `
    -Pattern 'variable set ANDROID_LOCAL_UPDATE_SIGNER_SHA256'
Require-ConfiguratorPattern `
    -Description "wrong-key rejection before repository writes" `
    -Pattern '(?s)if \(\$signerSha256 -ne \$expectedSigner\).*?No GitHub secret or variable was changed\..*?gh secret set'
Require-ConfiguratorPattern `
    -Description "verified offline-key-backup confirmation before repository writes" `
    -Pattern '(?s)if \(-not \$OfflineBackupConfirmed\).*?No GitHub secret or variable was changed\..*?gh secret set'
Reject-ConfiguratorPattern `
    -Description "unrelated release-signing credentials remain" `
    -Pattern 'ANDROID_RELEASE_'
Reject-ConfiguratorPattern `
    -Description "published signer must not be caller-overridable" `
    -Pattern '(?m)^\s*\[string\]\$ExpectedSignerSha256\b'

$requiredPackagePattern = 'com\.sts2launcher\.overhaul\.fork\.local'
$requiredSignerPattern = 'fd0e3d5acf435c1d23bfc5c426e99aa9eb5808619ff1fc214ffca99cfac7e57a'
$requiredBaselineTagPattern = 'v0\.2\.416-startup-recovery-ime'
$requiredBaselineAssetPattern = 'StS2Launcher-v0\.2\.416-startup-recovery-ime-local-arm64-v8a\.apk'
$requiredBaselineHashPattern = 'fdf2dcfcf2352d0e1a370da76922fb5b70cee3654d98c5fe9afbbd39554fc17b'

Require-StaticPattern `
    -Content $matrixReviewer `
    -Subject 'matrix reviewer' `
    -Description 'fixed affected-user .local package' `
    -Pattern "(?m)^\`$requiredCandidatePackage\s*=\s*`"$requiredPackagePattern`"\s*`$"
Require-StaticPattern `
    -Content $matrixReviewer `
    -Subject 'matrix reviewer' `
    -Description 'fixed published v0.2.416 signer' `
    -Pattern "(?s)\`$requiredCandidateSignerSha256\s*=\s*`"$requiredSignerPattern`""
Require-StaticPattern `
    -Content $matrixReviewer `
    -Subject 'matrix reviewer' `
    -Description 'fixed update-baseline tag' `
    -Pattern "(?m)^\`$requiredBaselineTag\s*=\s*`"$requiredBaselineTagPattern`"\s*`$"
Require-StaticPattern `
    -Content $matrixReviewer `
    -Subject 'matrix reviewer' `
    -Description 'fixed update-baseline asset' `
    -Pattern "(?s)\`$requiredBaselineAsset\s*=\s*`"$requiredBaselineAssetPattern`""
Require-StaticPattern `
    -Content $matrixReviewer `
    -Subject 'matrix reviewer' `
    -Description 'fixed update-baseline bytes' `
    -Pattern "(?s)\`$requiredBaselineSha256\s*=\s*`"$requiredBaselineHashPattern`""
Require-StaticPattern `
    -Content $matrixReviewer `
    -Subject 'matrix reviewer' `
    -Description 'published v0.2.416 versionCode floor' `
    -Pattern '(?m)^\$requiredMinimumVersionCode\s*=\s*416001\s*$'
Require-StaticPattern `
    -Content $matrixReviewer `
    -Subject 'matrix reviewer' `
    -Description 'package pin enforcement' `
    -Pattern "(?m)Assert-Equal\s+-Label\s+'Required affected-user package'.*?-Expected\s+\`$requiredCandidatePackage"
Require-StaticPattern `
    -Content $matrixReviewer `
    -Subject 'matrix reviewer' `
    -Description 'signer pin enforcement' `
    -Pattern "(?m)Assert-Equal\s+-Label\s+'Required v0\.2\.416 update signer'.*?-Expected\s+\`$requiredCandidateSignerSha256"
Require-StaticPattern `
    -Content $matrixReviewer `
    -Subject 'matrix reviewer' `
    -Description 'version floor enforcement' `
    -Pattern "(?m)versionCode\s+-gt\s+\`$requiredMinimumVersionCode"
Require-StaticPattern `
    -Content $matrixReviewer `
    -Subject 'matrix reviewer' `
    -Description 'exact update-baseline identity enforcement' `
    -Pattern "(?s)'Required update-baseline tag'.*?-Expected\s+\`$requiredBaselineTag.*?'Required update-baseline asset'.*?-Expected\s+\`$requiredBaselineAsset.*?'Required update-baseline APK SHA-256'.*?-Expected\s+\`$requiredBaselineSha256"

Require-StaticPattern `
    -Content $matrixTemplate `
    -Subject 'matrix template' `
    -Description 'fixed affected-user .local package' `
    -Pattern "packageName\s*=\s*'$requiredPackagePattern'"
Require-StaticPattern `
    -Content $matrixTemplate `
    -Subject 'matrix template' `
    -Description 'fixed published v0.2.416 signer' `
    -Pattern "signerSha256\s*=\s*'$requiredSignerPattern'"
Require-StaticPattern `
    -Content $matrixTemplate `
    -Subject 'matrix template' `
    -Description 'candidate version newer than the pinned floor' `
    -Pattern "versionCode\s*=\s*'417003'"
Require-StaticPattern `
    -Content $matrixTemplate `
    -Subject 'matrix template' `
    -Description 'fixed update-baseline tag' `
    -Pattern "updateBaselineTag\s*=\s*'$requiredBaselineTagPattern'"
Require-StaticPattern `
    -Content $matrixTemplate `
    -Subject 'matrix template' `
    -Description 'fixed update-baseline asset' `
    -Pattern "updateBaselineAssetName\s*=\s*'$requiredBaselineAssetPattern'"
Require-StaticPattern `
    -Content $matrixTemplate `
    -Subject 'matrix template' `
    -Description 'fixed update-baseline bytes' `
    -Pattern "updateBaselineApkSha256\s*=\s*'$requiredBaselineHashPattern'"

$guardOutputRoot = Join-Path (
    [IO.Path]::GetTempPath()
) "sts2-stage5-mutation-guard-$([Guid]::NewGuid().ToString('N'))"
foreach ($forbiddenSwitch in @("ClearLogcat", "EnableVerboseSaveDiagnostics")) {
    $rejected = $false
    try {
        $arguments = @{
            PackageName = "com.sts2launcher.overhaul.fork.local"
            Stage5Row = "1"
            OutputRoot = $guardOutputRoot
            AdbPath = (Join-Path $guardOutputRoot "adb-must-not-run.exe")
        }
        $arguments[$forbiddenSwitch] = $true
        & $collectorPath @arguments
    } catch {
        $rejected = $_.Exception.Message -like `
            "*Stage 5 evidence capture is read-only*"
    }
    if (-not $rejected) {
        throw "Stage 5 collector did not reject -$forbiddenSwitch before ADB access."
    }
    if (Test-Path -LiteralPath $guardOutputRoot) {
        throw "Stage 5 collector created evidence before rejecting -$forbiddenSwitch."
    }
}

Write-Host (
    "Stage 5 release gate passed: the workflow can retain an APK candidate " +
    "but cannot publish it before device signoff."
)
