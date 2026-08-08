param()

$ErrorActionPreference = "Stop"

function Get-ByteSha256 {
    param([Parameter(Mandatory = $true)][AllowEmptyCollection()][byte[]]$Bytes)

    $algorithm = [Security.Cryptography.SHA256]::Create()
    try {
        return (($algorithm.ComputeHash($Bytes) | ForEach-Object { $_.ToString("x2") }) -join "")
    } finally {
        $algorithm.Dispose()
    }
}

function Assert-True {
    param(
        [Parameter(Mandatory = $true)][bool]$Condition,
        [Parameter(Mandatory = $true)][string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

$collectorPath = Join-Path $PSScriptRoot "collect-android-save-validation.ps1"
$testRoot = Join-Path ([IO.Path]::GetTempPath()) "sts2-stage5-collector-bytes-$([Guid]::NewGuid().ToString('N'))"
$isWindowsHost = [Environment]::OSVersion.Platform -eq [PlatformID]::Win32NT
$fakeAdbName = if ($isWindowsHost) { "fake-adb.exe" } else { "fake-adb" }
$fakeAdbPath = Join-Path $testRoot $fakeAdbName
$statePath = "files/.sts2-launcher/automatic-sync/pending-sync.json"
$environmentNames = @(
    "STS2_FAKE_ADB_MODE",
    "STS2_FAKE_STATE_BASE64",
    "STS2_FAKE_STATE_SHA256",
    "STS2_FAKE_STATE_SIZE",
    "STS2_FAKE_STATE_PATH"
)
$previousEnvironment = @{}
foreach ($name in $environmentNames) {
    $previousEnvironment[$name] = [Environment]::GetEnvironmentVariable(
        $name,
        [EnvironmentVariableTarget]::Process
    )
}

try {
    New-Item -ItemType Directory -Path $testRoot | Out-Null
    if ($isWindowsHost) {
        $fakeAdbSource = @'
using System;
using System.Collections.Generic;

public static class Stage5FakeAdb
{
    public static int Main(string[] rawArguments)
    {
        var arguments = new List<string>(rawArguments);
        if (arguments.Count >= 2 && arguments[0] == "-s")
        {
            arguments.RemoveRange(0, 2);
        }

        string joined = string.Join("\n", arguments.ToArray());
        if (arguments.Count >= 1 && arguments[0] == "devices")
        {
            Console.WriteLine("List of devices attached");
            Console.WriteLine("stage5-fake-device device product:fake model:Fake transport_id:1");
            return 0;
        }
        if (joined.Contains("echo RUN_AS_OK"))
        {
            Console.WriteLine("RUN_AS_OK");
            return 0;
        }
        if (arguments.Count >= 2 && arguments[0] == "shell" && arguments[1] == "getprop")
        {
            if (arguments.Count == 2) Console.WriteLine("[ro.product.model]: [Fake]");
            else if (arguments[2] == "ro.product.manufacturer") Console.WriteLine("Stage5");
            else if (arguments[2] == "ro.product.model") Console.WriteLine("Fake Device");
            else if (arguments[2] == "ro.build.version.sdk") Console.WriteLine("35");
            else if (arguments[2] == "ro.product.cpu.abilist") Console.WriteLine("arm64-v8a");
            else if (arguments[2] == "ro.build.fingerprint") Console.WriteLine("stage5/fake/device:15/test");
            return 0;
        }
        if (joined.StartsWith("shell\ndumpsys\npackage\n", StringComparison.Ordinal))
        {
            Console.WriteLine("Package [com.sts2launcher.overhaul.fork.local]");
            return 0;
        }
        if (joined.StartsWith("shell\npm\npath\n", StringComparison.Ordinal))
        {
            Console.WriteLine("package:/data/app/fake/base.apk");
            return 0;
        }
        if (joined.StartsWith("shell\nsha256sum\n/data/app/fake/base.apk", StringComparison.Ordinal))
        {
            Console.WriteLine(new string('a', 64) + "  /data/app/fake/base.apk");
            return 0;
        }
        if (arguments.Count >= 1 && arguments[0] == "logcat")
        {
            return 0;
        }
        if (joined.Contains("for root in files/.sts2-launcher/automatic-sync files/.sts2-launcher/recovery"))
        {
            Console.WriteLine(
                Environment.GetEnvironmentVariable("STS2_FAKE_STATE_SHA256") + "\t" +
                Environment.GetEnvironmentVariable("STS2_FAKE_STATE_SIZE") + "\t" +
                Environment.GetEnvironmentVariable("STS2_FAKE_STATE_PATH"));
            return 0;
        }
        if (joined.Contains("find files -maxdepth 9 -type f"))
        {
            return 0;
        }
        if (arguments.Count >= 5 && arguments[0] == "exec-out" &&
            arguments[1] == "run-as" && arguments[3] == "base64")
        {
            if (Environment.GetEnvironmentVariable("STS2_FAKE_ADB_MODE") == "read-failure")
            {
                Console.Error.WriteLine("simulated private-state read failure");
                return 7;
            }
            Console.Write(Environment.GetEnvironmentVariable("STS2_FAKE_STATE_BASE64"));
            return 0;
        }

        Console.Error.WriteLine("Unexpected fake adb invocation: " + joined);
        return 64;
    }
}
'@
        Add-Type `
            -TypeDefinition $fakeAdbSource `
            -OutputAssembly $fakeAdbPath `
            -OutputType ConsoleApplication
    } else {
        $fakeAdbSource = @'
#!/usr/bin/env bash
set -u

if [[ "${1:-}" == "-s" ]]; then
    shift 2
fi

joined=""
for argument in "$@"; do
    if [[ -n "$joined" ]]; then
        joined+=$'\n'
    fi
    joined+="$argument"
done

if [[ "${1:-}" == "devices" ]]; then
    printf '%s\n' "List of devices attached"
    printf '%s\n' "stage5-fake-device device product:fake model:Fake transport_id:1"
    exit 0
fi
if [[ "$joined" == *"echo RUN_AS_OK"* ]]; then
    printf '%s\n' "RUN_AS_OK"
    exit 0
fi
if [[ "${1:-}" == "shell" && "${2:-}" == "getprop" ]]; then
    if [[ $# -eq 2 ]]; then
        printf '%s\n' "[ro.product.model]: [Fake]"
    else
        case "${3:-}" in
            ro.product.manufacturer) printf '%s\n' "Stage5" ;;
            ro.product.model) printf '%s\n' "Fake Device" ;;
            ro.build.version.sdk) printf '%s\n' "35" ;;
            ro.product.cpu.abilist) printf '%s\n' "arm64-v8a" ;;
            ro.build.fingerprint) printf '%s\n' "stage5/fake/device:15/test" ;;
        esac
    fi
    exit 0
fi
if [[ "${1:-}" == "shell" && "${2:-}" == "dumpsys" && "${3:-}" == "package" ]]; then
    printf '%s\n' "Package [com.sts2launcher.overhaul.fork.local]"
    exit 0
fi
if [[ "${1:-}" == "shell" && "${2:-}" == "pm" && "${3:-}" == "path" ]]; then
    printf '%s\n' "package:/data/app/fake/base.apk"
    exit 0
fi
if [[ "${1:-}" == "shell" && "${2:-}" == "sha256sum" && "${3:-}" == "/data/app/fake/base.apk" ]]; then
    printf '%s  %s\n' "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa" "/data/app/fake/base.apk"
    exit 0
fi
if [[ "${1:-}" == "logcat" ]]; then
    exit 0
fi
if [[ "$joined" == *"for root in files/.sts2-launcher/automatic-sync files/.sts2-launcher/recovery"* ]]; then
    printf '%s\t%s\t%s\n' "${STS2_FAKE_STATE_SHA256-}" "${STS2_FAKE_STATE_SIZE-}" "${STS2_FAKE_STATE_PATH-}"
    exit 0
fi
if [[ "$joined" == *"find files -maxdepth 9 -type f"* ]]; then
    exit 0
fi
if [[ $# -ge 5 && "${1:-}" == "exec-out" && "${2:-}" == "run-as" && "${4:-}" == "base64" ]]; then
    if [[ "${STS2_FAKE_ADB_MODE-}" == "read-failure" ]]; then
        printf '%s\n' "simulated private-state read failure" >&2
        exit 7
    fi
    printf '%s' "${STS2_FAKE_STATE_BASE64-}"
    exit 0
fi

printf '%s\n' "Unexpected fake adb invocation: $joined" >&2
exit 64
'@
        [IO.File]::WriteAllText($fakeAdbPath, $fakeAdbSource, [Text.UTF8Encoding]::new($false))
        & chmod "+x" "--" $fakeAdbPath
        if ($LASTEXITCODE -ne 0) {
            throw "Could not make the non-Windows fake adb executable."
        }
    }

    $unicodeMarker = [char]0x00e5
    [byte[]]$inventoryBytes = @(
        [Text.Encoding]::UTF8.GetPreamble() +
        [Text.Encoding]::UTF8.GetBytes(
            "{`"Phase`":`"uploading`",`"Marker`":`"$unicodeMarker`"}`r`n"
        )
    )
    [byte[]]$driftBytes = [Text.Encoding]::UTF8.GetBytes(
        "{`"Phase`":`"uploading`",`"Marker`":`"changed`"}`n"
    )
    $inventorySha256 = Get-ByteSha256 -Bytes $inventoryBytes

    function Invoke-CaptureFixture {
        param(
            [Parameter(Mandatory = $true)][string]$Name,
            [Parameter(Mandatory = $true)][string]$Mode,
            [Parameter(Mandatory = $true)][AllowEmptyString()][string]$Payload
        )

        $outputRoot = Join-Path $testRoot $Name
        [Environment]::SetEnvironmentVariable("STS2_FAKE_ADB_MODE", $Mode, [EnvironmentVariableTarget]::Process)
        [Environment]::SetEnvironmentVariable("STS2_FAKE_STATE_BASE64", $Payload, [EnvironmentVariableTarget]::Process)
        [Environment]::SetEnvironmentVariable("STS2_FAKE_STATE_SHA256", $inventorySha256, [EnvironmentVariableTarget]::Process)
        [Environment]::SetEnvironmentVariable("STS2_FAKE_STATE_SIZE", [string]$inventoryBytes.LongLength, [EnvironmentVariableTarget]::Process)
        [Environment]::SetEnvironmentVariable("STS2_FAKE_STATE_PATH", $statePath, [EnvironmentVariableTarget]::Process)

        & $collectorPath `
            -PackageName "com.sts2launcher.overhaul.fork.local" `
            -AdbPath $fakeAdbPath `
            -OutputRoot $outputRoot `
            -SourceCommit ("b" * 40) `
            -LogcatTailLines 0

        $captures = @(Get-ChildItem -LiteralPath $outputRoot -Directory)
        Assert-True -Condition ($captures.Count -eq 1) -Message "$Name did not create exactly one capture."
        $captureRoot = $captures[0].FullName
        $index = @(
            [IO.File]::ReadAllText((Join-Path $captureRoot "sync-state-index.json"), [Text.Encoding]::UTF8) |
                ConvertFrom-Json
        )
        Assert-True -Condition ($index.Count -eq 1) -Message "$Name did not index exactly one private JSON document."
        return [pscustomobject]@{
            Root = $captureRoot
            Entry = $index[0]
            Manifest = [IO.File]::ReadAllText((Join-Path $captureRoot "manifest.json"), [Text.Encoding]::UTF8) |
                ConvertFrom-Json
        }
    }

    $success = Invoke-CaptureFixture `
        -Name "exact" `
        -Mode "success" `
        -Payload ([Convert]::ToBase64String($inventoryBytes))
    Assert-True -Condition ([bool]$success.Entry.byteIdentityVerified) -Message "Exact bytes were not identity-verified."
    Assert-True -Condition ([bool]$success.Entry.parsed) -Message "Exact valid JSON was not parsed."
    Assert-True -Condition ([string]$success.Entry.error -eq "") -Message "Exact capture recorded a spurious error."
    Assert-True -Condition ([string]$success.Entry.deviceSha256 -ceq $inventorySha256) -Message "Exact capture changed the device SHA-256."
    Assert-True -Condition ([string]$success.Entry.capturedSha256 -ceq $inventorySha256) -Message "Exact capture did not hash captured bytes independently."
    Assert-True -Condition ([int64]$success.Entry.deviceSizeBytes -eq $inventoryBytes.LongLength) -Message "Exact capture changed the device size."
    Assert-True -Condition ([int64]$success.Entry.capturedSizeBytes -eq $inventoryBytes.LongLength) -Message "Exact capture did not record captured size."
    $successBytes = [IO.File]::ReadAllBytes((Join-Path $success.Root ([string]$success.Entry.capturedFile)))
    Assert-True `
        -Condition ([Convert]::ToBase64String($successBytes) -ceq [Convert]::ToBase64String($inventoryBytes)) `
        -Message "The local captured file is not byte-for-byte identical to the fake device file."
    Assert-True -Condition ([int]$success.Manifest.gates.syncStateByteIdentityFailureCount -eq 0) -Message "Exact capture reported a byte-identity failure."
    Assert-True -Condition ([int]$success.Manifest.gates.syncStateParseFailureCount -eq 0) -Message "Exact capture reported a parse failure."

    $drift = Invoke-CaptureFixture `
        -Name "drift" `
        -Mode "success" `
        -Payload ([Convert]::ToBase64String($driftBytes))
    Assert-True -Condition (-not [bool]$drift.Entry.byteIdentityVerified) -Message "Device/capture drift was accepted."
    Assert-True -Condition (-not [bool]$drift.Entry.parsed) -Message "Drifted bytes were parsed."
    Assert-True -Condition ([string]$drift.Entry.error -ceq "captured bytes differ from device inventory") -Message "Drift did not produce the explicit identity error."
    Assert-True -Condition ([string]$drift.Entry.capturedSha256 -ceq (Get-ByteSha256 -Bytes $driftBytes)) -Message "Drifted captured bytes were not independently hashed."
    $driftCapturedBytes = [IO.File]::ReadAllBytes((Join-Path $drift.Root ([string]$drift.Entry.capturedFile)))
    Assert-True `
        -Condition ([Convert]::ToBase64String($driftCapturedBytes) -ceq [Convert]::ToBase64String($driftBytes)) `
        -Message "Drift evidence was not retained byte-for-byte."
    Assert-True -Condition ([int]$drift.Manifest.gates.syncStateByteIdentityFailureCount -eq 1) -Message "Drift was not surfaced by the summary gate."

    $malformed = Invoke-CaptureFixture -Name "malformed" -Mode "success" -Payload "not!base64"
    Assert-True -Condition (-not [bool]$malformed.Entry.byteIdentityVerified) -Message "Malformed base64 was accepted."
    Assert-True -Condition (-not [bool]$malformed.Entry.parsed) -Message "Malformed base64 was parsed."
    Assert-True -Condition ([string]$malformed.Entry.error -ceq "malformed base64") -Message "Malformed base64 did not produce an explicit error."
    Assert-True -Condition ([string]::IsNullOrEmpty([string]$malformed.Entry.capturedFile)) -Message "Malformed base64 created a captured file."
    Assert-True -Condition ([string]::IsNullOrEmpty([string]$malformed.Entry.capturedSha256)) -Message "Malformed base64 claimed a captured hash."

    $readFailure = Invoke-CaptureFixture -Name "read-failure" -Mode "read-failure" -Payload ""
    Assert-True -Condition (-not [bool]$readFailure.Entry.byteIdentityVerified) -Message "A failed device read was accepted."
    Assert-True -Condition (-not [bool]$readFailure.Entry.parsed) -Message "A failed device read was parsed."
    Assert-True -Condition ([string]$readFailure.Entry.error -ceq "read failed") -Message "A failed device read did not produce an explicit error."
    Assert-True -Condition ([string]::IsNullOrEmpty([string]$readFailure.Entry.capturedFile)) -Message "A failed device read created a captured file."

    Write-Host "Stage 5 collector exact-byte capture passed: exact, drift, malformed-base64, and read-failure fixtures."
} finally {
    foreach ($name in $environmentNames) {
        [Environment]::SetEnvironmentVariable(
            $name,
            $previousEnvironment[$name],
            [EnvironmentVariableTarget]::Process
        )
    }
    if (Test-Path -LiteralPath $testRoot) {
        Remove-Item -LiteralPath $testRoot -Recurse -Force
    }
}
