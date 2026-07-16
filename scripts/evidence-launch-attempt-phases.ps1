$script:LaunchAttemptSuccessfulHandoffPhases = @(
    "restart requested",
    "safe android restart requested",
    "in-process signalled"
)

$script:LaunchAttemptRejectedProofPhases = @(
    "setup failed",
    "checking",
    "mod readiness checking",
    "ready",
    "blocked",
    "blocked in model",
    "readiness failed",
    "mod readiness failed",
    "in-process signal failed",
    "launch handoff failed",
    "launch handoff not requested",
    "restart requested without ready files"
)

function Normalize-LaunchAttemptPhase([string]$Phase) {
    if ([string]::IsNullOrWhiteSpace($Phase)) {
        return ""
    }

    return $Phase.Trim().ToLowerInvariant()
}

function Test-SuccessfulLaunchAttemptPhase([string]$Phase) {
    return $script:LaunchAttemptSuccessfulHandoffPhases -contains (Normalize-LaunchAttemptPhase -Phase $Phase)
}

function Get-LaunchAttemptSuccessfulPhaseRegex() {
    return "(?:$((@($script:LaunchAttemptSuccessfulHandoffPhases) | ForEach-Object { [regex]::Escape($_) }) -join '|'))"
}

function Get-LaunchAttemptRejectedProofPhaseRegex() {
    return "(?:$((@($script:LaunchAttemptRejectedProofPhases) | ForEach-Object { [regex]::Escape($_) }) -join '|'))"
}
