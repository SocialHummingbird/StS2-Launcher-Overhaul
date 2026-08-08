function Read-MarkerValueFromText(
    [AllowNull()][string]$Text,
    [Parameter(Mandatory = $true)][string]$Prefix,
    [string]$MissingValue = "<missing>"
) {
    if ([string]::IsNullOrWhiteSpace($Text)) {
        return $MissingValue
    }

    foreach ($line in ($Text -split '\r?\n')) {
        if ($line.StartsWith($Prefix, [System.StringComparison]::OrdinalIgnoreCase)) {
            return $line.Substring($Prefix.Length).Trim()
        }
    }

    return $MissingValue
}

function Read-MarkerIntFromText(
    [AllowNull()][string]$Text,
    [Parameter(Mandatory = $true)][string]$Prefix
) {
    if ([string]::IsNullOrWhiteSpace($Text)) {
        return $null
    }

    foreach ($line in ($Text -split '\r?\n')) {
        if ($line.StartsWith($Prefix, [System.StringComparison]::OrdinalIgnoreCase)) {
            $value = $line.Substring($Prefix.Length).Trim()
            $parsed = 0
            if ([int]::TryParse($value, [ref]$parsed)) {
                return $parsed
            }
        }
    }

    return $null
}

function Read-MarkerRowsFromText(
    [AllowNull()][string]$Text,
    [Parameter(Mandatory = $true)][string]$Prefix,
    [int]$Limit = [int]::MaxValue,
    [string]$MissingValue = "<none>"
) {
    $rows = New-Object System.Collections.Generic.List[string]
    if (-not [string]::IsNullOrWhiteSpace($Text)) {
        foreach ($line in ($Text -split '\r?\n')) {
            if ($line.StartsWith($Prefix, [System.StringComparison]::OrdinalIgnoreCase)) {
                $rows.Add($line.Trim())
                if ($rows.Count -ge $Limit) {
                    break
                }
            }
        }
    }

    if ($rows.Count -eq 0) {
        return @($MissingValue)
    }

    return $rows.ToArray()
}

function Read-BranchFromMarkerText([AllowNull()][string]$Text) {
    return Read-MarkerValueFromText `
        -Text $Text `
        -Prefix "Branch:" `
        -MissingValue ""
}
