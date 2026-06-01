function Resolve-GodotSourceDirectory {
    param(
        [Parameter(Mandatory = $true)]
        [string]$GodotDir,
        [Parameter(Mandatory = $true)]
        [string]$Root
    )

    if ([System.IO.Path]::IsPathRooted($GodotDir)) {
        return [System.IO.Path]::GetFullPath($GodotDir)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $Root $GodotDir))
}

function Apply-GodotPatches {
    param(
        [Parameter(Mandatory = $true)]
        [string]$GodotDir,
        [Parameter(Mandatory = $true)]
        [string]$Root
    )

    $patchDir = Join-Path $Root "patches\godot"
    if (-not (Test-Path -LiteralPath $patchDir)) {
        return
    }

    $patches = Get-ChildItem -LiteralPath $patchDir -Filter "*.patch" | Sort-Object Name
    foreach ($patch in $patches) {
        Push-Location $GodotDir
        try {
            & git apply --check --ignore-whitespace $patch.FullName *> $null
            if ($LASTEXITCODE -eq 0) {
                Write-Host "Applying Godot patch: $($patch.Name)"
                & git apply --ignore-whitespace $patch.FullName
                if ($LASTEXITCODE -ne 0) {
                    throw "Failed to apply Godot patch: $($patch.FullName)"
                }
                continue
            }

            & git apply --reverse --check --ignore-whitespace $patch.FullName *> $null
            if ($LASTEXITCODE -eq 0) {
                Write-Host "Godot patch already applied: $($patch.Name)"
                continue
            }

            throw "Godot patch cannot be applied cleanly: $($patch.FullName)"
        } finally {
            Pop-Location
        }
    }
}
