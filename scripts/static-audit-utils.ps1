function Initialize-StaticAudit(
    [string]$ScriptRoot,
    [switch]$Quiet
) {
    $script:StaticAuditRoot = Resolve-Path (Join-Path $ScriptRoot "..")
    $script:StaticAuditFailures = New-Object System.Collections.Generic.List[string]
    $script:StaticAuditPasses = 0
    $script:StaticAuditQuiet = [bool]$Quiet
}

function Resolve-RepoPath([string]$RelativePath) {
    $normalized = $RelativePath -replace '[\\/]', [System.IO.Path]::DirectorySeparatorChar
    return Join-Path $script:StaticAuditRoot $normalized
}

function Read-RepoFile([string]$RelativePath) {
    $path = Resolve-RepoPath $RelativePath
    if (-not (Test-Path -LiteralPath $path)) {
        $script:StaticAuditFailures.Add("Missing file: $RelativePath")
        return $null
    }

    return Get-Content -LiteralPath $path -Raw
}

function Add-Check(
    [string]$RelativePath,
    [string]$Description,
    [string[]]$RequiredPatterns
) {
    $content = Read-RepoFile $RelativePath
    if ($null -eq $content) {
        return
    }

    foreach ($pattern in $RequiredPatterns) {
        if ($content -notmatch $pattern) {
            $script:StaticAuditFailures.Add("$RelativePath - $Description - missing pattern: $pattern")
            return
        }
    }

    $script:StaticAuditPasses += 1
    if (-not $script:StaticAuditQuiet) {
        Write-Host "PASS $RelativePath - $Description"
    }
}

function Add-ForbiddenCheck(
    [string]$RelativePath,
    [string]$Description,
    [string[]]$ForbiddenPatterns
) {
    $content = Read-RepoFile $RelativePath
    if ($null -eq $content) {
        return
    }

    foreach ($pattern in $ForbiddenPatterns) {
        if ($content -match $pattern) {
            $script:StaticAuditFailures.Add("$RelativePath - $Description - forbidden pattern present: $pattern")
            return
        }
    }

    $script:StaticAuditPasses += 1
    if (-not $script:StaticAuditQuiet) {
        Write-Host "PASS $RelativePath - $Description"
    }
}

function Complete-StaticAudit(
    [string]$FailureHeading,
    [string]$SuccessMessage,
    [switch]$ThrowOnFailure
) {
    if ($script:StaticAuditFailures.Count -gt 0) {
        Write-Host $FailureHeading
        foreach ($failure in $script:StaticAuditFailures) {
            Write-Host "FAIL $failure"
        }

        if ($ThrowOnFailure) {
            throw "$FailureHeading with $($script:StaticAuditFailures.Count) failure(s)."
        }

        exit 1
    }

    Write-Host ($SuccessMessage -f $script:StaticAuditPasses)
}
