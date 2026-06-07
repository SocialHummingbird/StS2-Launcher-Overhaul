param(
    [Parameter(Mandatory = $true)]
    [string]$LogcatPath,

    [switch]$RequirePostSteamGuard
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $LogcatPath)) {
    throw "Logcat file not found: $LogcatPath"
}

$log = Get-Content -LiteralPath $LogcatPath -Raw

$fatalPatterns = @(
    "FATAL EXCEPTION",
    "Fatal signal",
    "BUG: Unreferenced static string",
    "CryptoNative",
    "Interop\+Crypto",
    "AndroidCryptoNative_",
    "SafeEvpCipherCtxHandle",
    "SafeSslHandle",
    "System\.Net\.WebSockets\.WebSocketHandle\.CreateSecKeyAndSecWebSocketAccept",
    "System\.Security\.Cryptography\.SHA1\.TryHashData",
    "MethodAccessException",
    "MissingMethodException",
    "EntryPointNotFoundException",
    "Android Java SHA-1 TryHashData bridge failed",
    "HTTP bridge request failed: GET wss://",
    "Android Java HTTP bridge cannot handle WebSocket CM requests",
    "unknown protocol: wss"
)

$loginFailurePatterns = @(
    "The SteamClient instance must be connected",
    "Could not establish a Steam auth connection",
    "\[Auth\] Login failed"
)

$preSteamGuardBoundaryPatterns = @(
    "\[Auth\] Steam Guard 2FA code required",
    "\[Auth\] Authentication successful",
    "\[Launcher\] Ownership verified"
)

$postSteamGuardBoundaryPatterns = @(
    "\[Auth\] Authentication successful",
    "\[Launcher\] Ownership verified"
)

$requiredTransportEvidencePatterns = @(
    "\[Auth\] Android Steam CM protocol configured: WebSocket",
    "\[Auth\] Steam CM WebSocket using managed \.NET transport",
    "\[Auth\] Android Java SHA-1 TryHashData bridge active"
)

$fatalMatches = @()
foreach ($pattern in $fatalPatterns) {
    if ($log -match $pattern) {
        $fatalMatches += $pattern
    }
}

if ($fatalMatches.Count -gt 0) {
    Write-Error "Steam login crash regression detected. Matched: $($fatalMatches -join ', ')"
    exit 1
}

$loginFailureMatches = @()
foreach ($pattern in $loginFailurePatterns) {
    if ($log -match $pattern) {
        $loginFailureMatches += $pattern
    }
}

if ($loginFailureMatches.Count -gt 0) {
    Write-Error "Steam login failure regression detected. Matched: $($loginFailureMatches -join ', ')"
    exit 1
}

$missingTransportEvidence = @()
foreach ($pattern in $requiredTransportEvidencePatterns) {
    if ($log -notmatch $pattern) {
        $missingTransportEvidence += $pattern
    }
}

if ($missingTransportEvidence.Count -gt 0) {
    Write-Error "Steam login transport evidence missing. Missing: $($missingTransportEvidence -join ', ')"
    exit 1
}

$requiredPatterns = if ($RequirePostSteamGuard) {
    $postSteamGuardBoundaryPatterns
} else {
    $preSteamGuardBoundaryPatterns
}

$reachedBoundary = $false
foreach ($pattern in $requiredPatterns) {
    if ($log -match $pattern) {
        $reachedBoundary = $true
        break
    }
}

if (-not $reachedBoundary -and $RequirePostSteamGuard) {
    Write-Error "Steam login did not reach successful auth or ownership verification after Steam Guard."
    exit 1
}

if (-not $reachedBoundary) {
    Write-Error "Steam login did not reach Steam Guard, successful auth, or ownership verification boundary."
    exit 1
}

Write-Host "Steam login transport/auth regression check passed."
