function Resolve-EvidenceRepoPath(
    [Parameter(Mandatory = $true)][string]$RepoRoot,
    [Parameter(Mandatory = $true)][string]$Path
) {
    if ([System.IO.Path]::IsPathRooted($Path)) {
        return $Path
    }

    $normalized = $Path -replace '[\\/]', [System.IO.Path]::DirectorySeparatorChar
    return Join-Path $RepoRoot $normalized
}

function Get-EvidenceRelativePath(
    [Parameter(Mandatory = $true)][string]$RootPath,
    [Parameter(Mandatory = $true)][string]$Path
) {
    $rootWithSeparator = $RootPath
    if (-not $rootWithSeparator.EndsWith([System.IO.Path]::DirectorySeparatorChar)) {
        $rootWithSeparator += [System.IO.Path]::DirectorySeparatorChar
    }

    $rootUri = [System.Uri]::new($rootWithSeparator)
    $pathUri = [System.Uri]::new($Path)
    return [System.Uri]::UnescapeDataString(
        $rootUri.MakeRelativeUri($pathUri).ToString()
    ) -replace '/', [System.IO.Path]::DirectorySeparatorChar
}

function ConvertTo-EvidenceSafeFileName([AllowNull()][string]$Value) {
    if ([string]::IsNullOrWhiteSpace($Value)) {
        return "empty"
    }

    return ($Value -replace '[^A-Za-z0-9._-]', '_').Trim('_')
}
