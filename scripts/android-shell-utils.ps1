function ConvertTo-AndroidShellSingleQuoted([string]$Value) {
    return "'" + (($Value -split "'") -join "'\''") + "'"
}

function ConvertTo-AndroidShellPathSingleQuoted([string]$Value) {
    if ($Value.Contains("'")) {
        throw "Unsupported single quote in device path: $Value"
    }

    return ConvertTo-AndroidShellSingleQuoted $Value
}
