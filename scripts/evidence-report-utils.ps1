function Format-Cell {
    param([AllowNull()][string]$Value)

    if ($null -eq $Value) {
        return ""
    }

    return ($Value -replace '\|', '/' -replace "`r?`n", " ").Trim()
}

function Add-ValidationRow {
    param(
        [Parameter(Mandatory = $true)]$Lines,
        [Parameter(Mandatory = $true)][string]$Area,
        [Parameter(Mandatory = $true)][string]$Status,
        [Parameter(Mandatory = $true)][string]$Evidence,
        [Parameter(Mandatory = $true)][string]$RequiredNextAction
    )

    $Lines.Add("| $(Format-Cell $Area) | $(Format-Cell $Status) | $(Format-Cell $Evidence) | $(Format-Cell $RequiredNextAction) |")
}

function Add-HypothesisRow {
    param(
        [Parameter(Mandatory = $true)]$Lines,
        [Parameter(Mandatory = $true)][string]$Hypothesis,
        [Parameter(Mandatory = $true)][string]$Status,
        [Parameter(Mandatory = $true)][string]$Evidence,
        [Parameter(Mandatory = $true)][string]$NextProof
    )

    $Lines.Add("| $(Format-Cell $Hypothesis) | $(Format-Cell $Status) | $(Format-Cell $Evidence) | $(Format-Cell $NextProof) |")
}
