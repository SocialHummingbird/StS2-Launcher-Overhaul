param(
    [Parameter(Mandatory = $false)]
    [string] $GuardCode,

    [Parameter(Mandatory = $false)]
    [string] $CredentialsPath = "tmp\steam-login.local.json"
)

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "steam-login-utils.ps1")

function Fail($Message) {
    Write-Error $Message
    exit 1
}

if (-not (Test-Path -LiteralPath $CredentialsPath)) {
    Fail "Credentials file not found: $CredentialsPath"
}

try {
    if ([string]::IsNullOrWhiteSpace($GuardCode)) {
        $GuardCode = Read-HiddenText "Enter current Steam Guard code"
    }

    $GuardCode = Normalize-SteamGuardCode $GuardCode
} catch {
    Fail $_.Exception.Message
}

$credentials = Get-Content -LiteralPath $CredentialsPath -Raw | ConvertFrom-Json
if ($null -eq $credentials) {
    Fail "Credentials file is empty or invalid JSON: $CredentialsPath"
}

$properties = @($credentials.PSObject.Properties.Name)
if (-not $properties.Contains("username") -or -not $properties.Contains("password")) {
    Fail "Credentials file must already contain username and password fields."
}

if ($properties.Contains("guard_code")) {
    $credentials.guard_code = $GuardCode
} else {
    $credentials | Add-Member -MemberType NoteProperty -Name "guard_code" -Value $GuardCode
}

$credentials |
    ConvertTo-Json -Depth 10 |
    Set-Content -LiteralPath $CredentialsPath -Encoding UTF8

Write-Host "Updated Steam Guard code in $CredentialsPath"
