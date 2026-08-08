param()

$ErrorActionPreference = 'Stop'

function Assert-True {
    param(
        [Parameter(Mandatory = $true)][bool]$Condition,
        [Parameter(Mandatory = $true)][string]$Message
    )
    if (-not $Condition) { throw $Message }
}

function Get-RealSha256 {
    param([Parameter(Mandatory = $true)][string]$Path)
    return (Microsoft.PowerShell.Utility\Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
}

$preflightPath = Join-Path $PSScriptRoot 'check-stage5-affected-device.ps1'
$testRoot = Join-Path ([IO.Path]::GetTempPath()) "sts2-stage5-affected-preflight-$([Guid]::NewGuid().ToString('N'))"
$runningOnWindows = [IO.Path]::DirectorySeparatorChar -eq '\'
$fakeAdb = Join-Path $testRoot $(if ($runningOnWindows) { 'fake-adb.exe' } else { 'fake-adb' })
$fakeApkTool = Join-Path $testRoot $(if ($runningOnWindows) { 'fake-apk-tool.exe' } else { 'fake-apk-tool' })
$adbLog = Join-Path $testRoot 'adb-invocations.txt'
$packageName = 'com.sts2launcher.overhaul.fork.local'
$baselineHash = 'fdf2dcfcf2352d0e1a370da76922fb5b70cee3654d98c5fe9afbbd39554fc17b'
$signer = 'FD0E3D5ACF435C1D23BFC5C426E99AA9EB5808619FF1FC214FFCA99CFAC7E57A'
$sourceCommit = (& git -C (Resolve-Path (Join-Path $PSScriptRoot '..')).Path rev-parse HEAD).Trim().ToLowerInvariant()
if ($LASTEXITCODE -ne 0 -or $sourceCommit -notmatch '^[0-9a-f]{40}$') { throw 'Could not resolve fixture source commit.' }

$environmentNames = @(
    'STS2_PREFLIGHT_FAKE_INSTALLED_APK',
    'STS2_PREFLIGHT_FAKE_PROCESS',
    'STS2_PREFLIGHT_FAKE_UID',
    'STS2_PREFLIGHT_FAKE_FIRST_INSTALL',
    'STS2_PREFLIGHT_FAKE_DEVICE_MODE',
    'STS2_PREFLIGHT_FAKE_ADB_LOG'
)
$oldEnvironment = @{}
foreach ($name in $environmentNames) {
    $oldEnvironment[$name] = [Environment]::GetEnvironmentVariable($name, [EnvironmentVariableTarget]::Process)
}

try {
    New-Item -ItemType Directory -Path $testRoot | Out-Null

    if ($runningOnWindows) {
        $fakeAdbSource = @'
using System;
using System.Collections.Generic;
using System.IO;

public static class Stage5AffectedFakeAdb
{
    private static Dictionary<string, string> ReadFields(string path)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string line in File.ReadAllLines(path))
        {
            int separator = line.IndexOf('=');
            if (separator > 0) values[line.Substring(0, separator)] = line.Substring(separator + 1);
        }
        return values;
    }

    public static int Main(string[] raw)
    {
        string log = Environment.GetEnvironmentVariable("STS2_PREFLIGHT_FAKE_ADB_LOG");
        if (!String.IsNullOrEmpty(log)) File.AppendAllText(log, String.Join("\t", raw) + Environment.NewLine);

        var args = new List<string>(raw);
        if (args.Count == 2 && args[0] == "devices" && args[1] == "-l")
        {
            Console.WriteLine("List of devices attached");
            Console.WriteLine("affected-device\tdevice product:fake model:Fold transport_id:1");
            if (Environment.GetEnvironmentVariable("STS2_PREFLIGHT_FAKE_DEVICE_MODE") == "multiple")
                Console.WriteLine("unexpected-device\tdevice product:fake model:Other transport_id:2");
            return 0;
        }
        if (args.Count >= 2 && args[0] == "-s") args.RemoveRange(0, 2);
        string joined = String.Join("\n", args.ToArray());
        string installed = Environment.GetEnvironmentVariable("STS2_PREFLIGHT_FAKE_INSTALLED_APK");
        var fields = String.IsNullOrEmpty(installed) ? new Dictionary<string, string>() : ReadFields(installed);

        if (joined == "shell\npidof\ncom.sts2launcher.overhaul.fork.local")
        {
            string process = Environment.GetEnvironmentVariable("STS2_PREFLIGHT_FAKE_PROCESS");
            if (!String.IsNullOrEmpty(process)) { Console.WriteLine(process); return 0; }
            return 1;
        }
        if (joined == "shell\ngetprop\nro.product.model") { Console.WriteLine("Alexander's Z Fold8"); return 0; }
        if (joined == "shell\ngetprop\nro.build.fingerprint") { Console.WriteLine("samsung/fake/fold:16/test"); return 0; }
        if (joined == "shell\npm\npath\ncom.sts2launcher.overhaul.fork.local")
        {
            Console.WriteLine("package:/data/app/affected/base.apk");
            return 0;
        }
        if (joined == "shell\npm\nlist\npackages\n-U\ncom.sts2launcher.overhaul.fork.local")
        {
            Console.WriteLine("package:com.sts2launcher.overhaul.fork.local uid:" + Environment.GetEnvironmentVariable("STS2_PREFLIGHT_FAKE_UID"));
            return 0;
        }
        if (joined == "shell\ndumpsys\npackage\ncom.sts2launcher.overhaul.fork.local")
        {
            Console.WriteLine("Package [com.sts2launcher.overhaul.fork.local] (fixture):");
            Console.WriteLine("  appId=" + Environment.GetEnvironmentVariable("STS2_PREFLIGHT_FAKE_UID"));
            Console.WriteLine("  versionCode=" + fields["versionCode"] + " minSdk=28 targetSdk=35");
            Console.WriteLine("  dataDir=/data/user/0/com.sts2launcher.overhaul.fork.local");
            Console.WriteLine("  firstInstallTime=" + Environment.GetEnvironmentVariable("STS2_PREFLIGHT_FAKE_FIRST_INSTALL"));
            return 0;
        }
        if (args.Count == 3 && args[0] == "pull" && args[1] == "/data/app/affected/base.apk")
        {
            File.Copy(installed, args[2], true);
            Console.WriteLine("1 file pulled");
            return 0;
        }

        Console.Error.WriteLine("Unexpected fake adb invocation: " + joined);
        return 64;
    }
}
'@
        Add-Type -TypeDefinition $fakeAdbSource -OutputAssembly $fakeAdb -OutputType ConsoleApplication

        $fakeApkToolSource = @'
using System;
using System.Collections.Generic;
using System.IO;

public static class Stage5AffectedFakeApkTool
{
    private static Dictionary<string, string> ReadFields(string path)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string line in File.ReadAllLines(path))
        {
            int separator = line.IndexOf('=');
            if (separator > 0) values[line.Substring(0, separator)] = line.Substring(separator + 1);
        }
        return values;
    }

    public static int Main(string[] args)
    {
        if (args.Length < 3) return 64;
        string path = args[args.Length - 1];
        var fields = ReadFields(path);
        if (args[0] == "dump" && args[1] == "badging")
        {
            Console.WriteLine("package: name='" + fields["package"] + "' versionCode='" + fields["versionCode"] + "' versionName='" + fields["versionName"] + "'");
            Console.WriteLine("native-code: 'arm64-v8a'");
            return 0;
        }
        if (args[0] == "verify" && args[1] == "--print-certs")
        {
            Console.WriteLine("Signer #1 certificate SHA-256 digest: " + fields["signer"]);
            return 0;
        }
        return 64;
    }
}
'@
        Add-Type -TypeDefinition $fakeApkToolSource -OutputAssembly $fakeApkTool -OutputType ConsoleApplication
    } else {
        $fakeAdbSource = @'
#!/bin/sh

if [ -n "${STS2_PREFLIGHT_FAKE_ADB_LOG:-}" ]; then
    {
        first=true
        for argument in "$@"; do
            if [ "$first" = false ]; then printf '\t'; fi
            printf '%s' "$argument"
            first=false
        done
        printf '\n'
    } >> "$STS2_PREFLIGHT_FAKE_ADB_LOG"
fi

if [ "$#" -eq 2 ] && [ "$1" = devices ] && [ "$2" = -l ]; then
    printf 'List of devices attached\n'
    printf 'affected-device\tdevice product:fake model:Fold transport_id:1\n'
    if [ "${STS2_PREFLIGHT_FAKE_DEVICE_MODE:-}" = multiple ]; then
        printf 'unexpected-device\tdevice product:fake model:Other transport_id:2\n'
    fi
    exit 0
fi

if [ "$#" -ge 2 ] && [ "$1" = -s ]; then
    shift 2
fi

installed=${STS2_PREFLIGHT_FAKE_INSTALLED_APK:-}
field() {
    sed -n "s/^$1=//p" "$installed"
}

if [ "$#" -eq 3 ] && [ "$1" = shell ] && [ "$2" = pidof ] && [ "$3" = com.sts2launcher.overhaul.fork.local ]; then
    if [ -n "${STS2_PREFLIGHT_FAKE_PROCESS:-}" ]; then
        printf '%s\n' "$STS2_PREFLIGHT_FAKE_PROCESS"
        exit 0
    fi
    exit 1
fi
if [ "$#" -eq 3 ] && [ "$1" = shell ] && [ "$2" = getprop ] && [ "$3" = ro.product.model ]; then
    printf "%s\n" "Alexander's Z Fold8"
    exit 0
fi
if [ "$#" -eq 3 ] && [ "$1" = shell ] && [ "$2" = getprop ] && [ "$3" = ro.build.fingerprint ]; then
    printf 'samsung/fake/fold:16/test\n'
    exit 0
fi
if [ "$#" -eq 4 ] && [ "$1" = shell ] && [ "$2" = pm ] && [ "$3" = path ] && [ "$4" = com.sts2launcher.overhaul.fork.local ]; then
    printf 'package:/data/app/affected/base.apk\n'
    exit 0
fi
if [ "$#" -eq 6 ] && [ "$1" = shell ] && [ "$2" = pm ] && [ "$3" = list ] && [ "$4" = packages ] && [ "$5" = -U ] && [ "$6" = com.sts2launcher.overhaul.fork.local ]; then
    printf 'package:com.sts2launcher.overhaul.fork.local uid:%s\n' "$STS2_PREFLIGHT_FAKE_UID"
    exit 0
fi
if [ "$#" -eq 4 ] && [ "$1" = shell ] && [ "$2" = dumpsys ] && [ "$3" = package ] && [ "$4" = com.sts2launcher.overhaul.fork.local ]; then
    printf 'Package [com.sts2launcher.overhaul.fork.local] (fixture):\n'
    printf '  appId=%s\n' "$STS2_PREFLIGHT_FAKE_UID"
    printf '  versionCode=%s minSdk=28 targetSdk=35\n' "$(field versionCode)"
    printf '  dataDir=/data/user/0/com.sts2launcher.overhaul.fork.local\n'
    printf '  firstInstallTime=%s\n' "$STS2_PREFLIGHT_FAKE_FIRST_INSTALL"
    exit 0
fi
if [ "$#" -eq 3 ] && [ "$1" = pull ] && [ "$2" = /data/app/affected/base.apk ]; then
    cp "$installed" "$3"
    printf '1 file pulled\n'
    exit 0
fi

printf 'Unexpected fake adb invocation:' >&2
for argument in "$@"; do printf ' %s' "$argument" >&2; done
printf '\n' >&2
exit 64
'@
        $fakeApkToolSource = @'
#!/bin/sh

if [ "$#" -lt 3 ]; then exit 64; fi
for path in "$@"; do :; done
field() {
    sed -n "s/^$1=//p" "$path"
}

if [ "$1" = dump ] && [ "$2" = badging ]; then
    printf "package: name='%s' versionCode='%s' versionName='%s'\n" "$(field package)" "$(field versionCode)" "$(field versionName)"
    printf "native-code: 'arm64-v8a'\n"
    exit 0
fi
if [ "$1" = verify ] && [ "$2" = --print-certs ]; then
    printf 'Signer #1 certificate SHA-256 digest: %s\n' "$(field signer)"
    exit 0
fi
exit 64
'@
        [IO.File]::WriteAllText($fakeAdb, $fakeAdbSource.Replace("`r`n", "`n"), [Text.UTF8Encoding]::new($false))
        [IO.File]::WriteAllText($fakeApkTool, $fakeApkToolSource.Replace("`r`n", "`n"), [Text.UTF8Encoding]::new($false))
        & chmod '+x' $fakeAdb $fakeApkTool
        if ($LASTEXITCODE -ne 0) { throw 'Could not make non-Windows fake tools executable.' }
    }

    function Write-ApkFixture {
        param(
            [Parameter(Mandatory = $true)][string]$Path,
            [Parameter(Mandatory = $true)][string]$Role,
            [Parameter(Mandatory = $true)][string]$VersionCode,
            [Parameter(Mandatory = $true)][string]$VersionName,
            [Parameter(Mandatory = $true)][string]$Signer
        )
        [IO.File]::WriteAllLines($Path, @(
            "role=$Role",
            "package=$packageName",
            "versionCode=$VersionCode",
            "versionName=$VersionName",
            "signer=$Signer"
        ), [Text.UTF8Encoding]::new($false))
    }

    function New-CandidateFixture {
        param(
            [Parameter(Mandatory = $true)][string]$Name,
            [string]$VersionCode = '417003',
            [string]$VersionName = '0.2.417-stage5-fixture',
            [string]$Signer = $signer
        )
        $directory = Join-Path $testRoot $Name
        New-Item -ItemType Directory -Path $directory | Out-Null
        $apk = Join-Path $directory 'candidate.apk'
        Write-ApkFixture -Path $apk -Role candidate -VersionCode $VersionCode -VersionName $VersionName -Signer $Signer
        $hash = Get-RealSha256 -Path $apk
        $checksum = "$apk.sha256"
        [IO.File]::WriteAllText($checksum, "$hash  candidate.apk`n", [Text.Encoding]::ASCII)
        $buildInfo = "$apk.build-info.txt"
        [IO.File]::WriteAllLines($buildInfo, @(
            "version_name=$VersionName",
            "version_code=$VersionCode",
            "package_name=$packageName",
            'abi=arm64-v8a',
            'signing_channel=release',
            "signer_sha256=$Signer",
            'release_tag=v0.2.417-stage5-fixture',
            "source_commit=$sourceCommit",
            'candidate_run_id=123456789',
            'candidate_run_attempt=1',
            "apk_sha256=$hash",
            'update_baseline_tag=v0.2.416-startup-recovery-ime',
            'update_baseline_asset_name=StS2Launcher-v0.2.416-startup-recovery-ime-local-arm64-v8a.apk',
            "update_baseline_apk_sha256=$baselineHash"
        ), [Text.UTF8Encoding]::new($false))
        return [pscustomobject]@{ Apk = $apk; Checksum = $checksum; BuildInfo = $buildInfo; Hash = $hash }
    }

    # The production script pins the real v0.2.416 digest. This isolated hash
    # double lets a tiny textual baseline fixture stand in for those large APK
    # bytes; all candidate, manifest, and sidecar hashes remain real SHA-256.
    function Get-FileHash {
        param(
            [Parameter(Mandatory = $true)][string]$LiteralPath,
            [string]$Algorithm = 'SHA256'
        )
        $text = [IO.File]::ReadAllText($LiteralPath)
        if ($text.StartsWith('role=baseline')) {
            return [pscustomobject]@{ Hash = $baselineHash.ToUpperInvariant() }
        }
        return Microsoft.PowerShell.Utility\Get-FileHash -LiteralPath $LiteralPath -Algorithm $Algorithm
    }

    function Invoke-Preflight {
        param(
            [Parameter(Mandatory = $true)][string]$Mode,
            [Parameter(Mandatory = $true)]$Candidate,
            [Parameter(Mandatory = $true)][string]$OutputName,
            [string]$PriorManifest = ''
        )
        $arguments = @{
            Mode = $Mode
            CandidateApkPath = $Candidate.Apk
            CandidateChecksumPath = $Candidate.Checksum
            CandidateBuildInfoPath = $Candidate.BuildInfo
            ExpectedSourceCommit = $sourceCommit
            ExpectedCandidateRunId = '123456789'
            ExpectedCandidateRunAttempt = '1'
            AdbPath = $fakeAdb
            AaptPath = $fakeApkTool
            ApkSignerPath = $fakeApkTool
            GitPath = 'git'
            OutputRoot = (Join-Path $testRoot $OutputName)
        }
        if ($PriorManifest) { $arguments.PriorManifestPath = $PriorManifest }
        & $preflightPath @arguments *> $null
        return @(Get-ChildItem -LiteralPath $arguments.OutputRoot -Directory | Sort-Object Name | Select-Object -Last 1)
    }

    function Assert-PreflightRejects {
        param(
            [Parameter(Mandatory = $true)][scriptblock]$Action,
            [Parameter(Mandatory = $true)][string]$Pattern,
            [Parameter(Mandatory = $true)][string]$Label
        )
        $message = ''
        try { & $Action; throw "Fixture unexpectedly passed: $Label" } catch { $message = $_.Exception.Message }
        if ($message -like 'Fixture unexpectedly passed:*' -or $message -notmatch $Pattern) {
            throw "$Label did not fail closed as expected. Error: $message"
        }
    }

    [Environment]::SetEnvironmentVariable('STS2_PREFLIGHT_FAKE_ADB_LOG', $adbLog, [EnvironmentVariableTarget]::Process)
    [Environment]::SetEnvironmentVariable('STS2_PREFLIGHT_FAKE_PROCESS', '', [EnvironmentVariableTarget]::Process)
    [Environment]::SetEnvironmentVariable('STS2_PREFLIGHT_FAKE_UID', '10234', [EnvironmentVariableTarget]::Process)
    [Environment]::SetEnvironmentVariable('STS2_PREFLIGHT_FAKE_FIRST_INSTALL', '2026-06-01 12:34:56', [EnvironmentVariableTarget]::Process)
    [Environment]::SetEnvironmentVariable('STS2_PREFLIGHT_FAKE_DEVICE_MODE', '', [EnvironmentVariableTarget]::Process)

    $baselineApk = Join-Path $testRoot 'baseline.apk'
    Write-ApkFixture -Path $baselineApk -Role baseline -VersionCode '416001' -VersionName '0.2.416-startup-recovery-ime-local' -Signer $signer
    $valid = New-CandidateFixture -Name valid

    [Environment]::SetEnvironmentVariable('STS2_PREFLIGHT_FAKE_INSTALLED_APK', $baselineApk, [EnvironmentVariableTarget]::Process)
    $preDirectory = Invoke-Preflight -Mode PreInstall -Candidate $valid -OutputName pre-success
    $preManifest = Join-Path $preDirectory.FullName 'preflight-manifest.json'
    Assert-True (Test-Path -LiteralPath $preManifest) 'PreInstall did not retain a manifest.'
    Assert-True (Test-Path -LiteralPath "$preManifest.sha256") 'PreInstall did not retain a manifest checksum.'
    $pre = [IO.File]::ReadAllText($preManifest) | ConvertFrom-Json
    Assert-True ([string]$pre.mode -eq 'PreInstall' -and [string]$pre.result -eq 'verified') 'PreInstall manifest is not verified.'
    Assert-True ([string]$pre.device.pulledApkSha256 -eq $baselineHash) 'PreInstall did not bind the pinned baseline bytes.'

    [Environment]::SetEnvironmentVariable('STS2_PREFLIGHT_FAKE_INSTALLED_APK', $valid.Apk, [EnvironmentVariableTarget]::Process)
    $postDirectory = Invoke-Preflight -Mode PostInstall -Candidate $valid -OutputName post-success -PriorManifest $preManifest
    $post = [IO.File]::ReadAllText((Join-Path $postDirectory.FullName 'preflight-manifest.json')) | ConvertFrom-Json
    Assert-True ([string]$post.mode -eq 'PostInstall' -and [string]$post.device.pulledApkSha256 -eq $valid.Hash) 'PostInstall did not bind exact candidate bytes.'
    Assert-True ([string]$post.device.uid -eq [string]$pre.device.uid) 'PostInstall did not preserve UID continuity.'

    $wrongVersion = New-CandidateFixture -Name wrong-version -VersionCode '416001' -VersionName '0.2.416-wrong'
    Assert-PreflightRejects -Label 'wrong candidate version' -Pattern 'greater than baseline' -Action {
        Invoke-Preflight -Mode PreInstall -Candidate $wrongVersion -OutputName reject-version | Out-Null
    }

    $wrongSignerValue = ('0' * 64)
    $wrongSigner = New-CandidateFixture -Name wrong-signer -Signer $wrongSignerValue
    Assert-PreflightRejects -Label 'wrong candidate signer' -Pattern 'Candidate signer mismatch' -Action {
        Invoke-Preflight -Mode PreInstall -Candidate $wrongSigner -OutputName reject-signer | Out-Null
    }

    $wrongSource = New-CandidateFixture -Name wrong-source
    [IO.File]::WriteAllText(
        $wrongSource.BuildInfo,
        ([IO.File]::ReadAllText($wrongSource.BuildInfo)).Replace("source_commit=$sourceCommit", "source_commit=$('0' * 40)"),
        [Text.UTF8Encoding]::new($false)
    )
    Assert-PreflightRejects -Label 'wrong candidate source binding' -Pattern 'source_commit mismatch' -Action {
        Invoke-Preflight -Mode PreInstall -Candidate $wrongSource -OutputName reject-source | Out-Null
    }

    $wrongRun = New-CandidateFixture -Name wrong-run
    [IO.File]::WriteAllText(
        $wrongRun.BuildInfo,
        ([IO.File]::ReadAllText($wrongRun.BuildInfo)).Replace('candidate_run_id=123456789', 'candidate_run_id=987654321'),
        [Text.UTF8Encoding]::new($false)
    )
    Assert-PreflightRejects -Label 'wrong candidate run binding' -Pattern 'candidate_run_id mismatch' -Action {
        Invoke-Preflight -Mode PreInstall -Candidate $wrongRun -OutputName reject-run | Out-Null
    }

    $wrongHash = New-CandidateFixture -Name wrong-hash
    [IO.File]::AppendAllText($wrongHash.Apk, 'tampered', [Text.UTF8Encoding]::new($false))
    Assert-PreflightRejects -Label 'wrong candidate hash binding' -Pattern 'checksum mismatch' -Action {
        Invoke-Preflight -Mode PreInstall -Candidate $wrongHash -OutputName reject-hash | Out-Null
    }

    [Environment]::SetEnvironmentVariable('STS2_PREFLIGHT_FAKE_DEVICE_MODE', 'multiple', [EnvironmentVariableTarget]::Process)
    Assert-PreflightRejects -Label 'multiple authorized targets' -Pattern 'exactly one authorized Android target' -Action {
        Invoke-Preflight -Mode PreInstall -Candidate $valid -OutputName reject-multiple-devices | Out-Null
    }
    [Environment]::SetEnvironmentVariable('STS2_PREFLIGHT_FAKE_DEVICE_MODE', '', [EnvironmentVariableTarget]::Process)

    [Environment]::SetEnvironmentVariable('STS2_PREFLIGHT_FAKE_INSTALLED_APK', $baselineApk, [EnvironmentVariableTarget]::Process)
    [Environment]::SetEnvironmentVariable('STS2_PREFLIGHT_FAKE_PROCESS', '9876', [EnvironmentVariableTarget]::Process)
    Assert-PreflightRejects -Label 'running app process' -Pattern 'app process is running' -Action {
        Invoke-Preflight -Mode PreInstall -Candidate $valid -OutputName reject-running | Out-Null
    }
    [Environment]::SetEnvironmentVariable('STS2_PREFLIGHT_FAKE_PROCESS', '', [EnvironmentVariableTarget]::Process)

    [Environment]::SetEnvironmentVariable('STS2_PREFLIGHT_FAKE_INSTALLED_APK', $valid.Apk, [EnvironmentVariableTarget]::Process)
    [Environment]::SetEnvironmentVariable('STS2_PREFLIGHT_FAKE_UID', '10235', [EnvironmentVariableTarget]::Process)
    Assert-PreflightRejects -Label 'changed post-install UID' -Pattern 'does not match PreInstall: uid' -Action {
        Invoke-Preflight -Mode PostInstall -Candidate $valid -OutputName reject-post-uid -PriorManifest $preManifest | Out-Null
    }
    [Environment]::SetEnvironmentVariable('STS2_PREFLIGHT_FAKE_UID', '10234', [EnvironmentVariableTarget]::Process)
    [Environment]::SetEnvironmentVariable('STS2_PREFLIGHT_FAKE_FIRST_INSTALL', '2026-08-08 22:00:00', [EnvironmentVariableTarget]::Process)
    Assert-PreflightRejects -Label 'changed post-install firstInstallTime' -Pattern 'does not match PreInstall: firstInstallTime' -Action {
        Invoke-Preflight -Mode PostInstall -Candidate $valid -OutputName reject-post-time -PriorManifest $preManifest | Out-Null
    }

    $source = [IO.File]::ReadAllText($preflightPath)
    $forbiddenLiteralPatterns = @(
        '(?i)["''](?:install|install-multiple|install-multi-package|uninstall|push|root|remount|reboot|sync|reverse|forward)["'']',
        '(?i)force-stop',
        '(?is)["'']shell["''].{0,100}["''](?:am|input|monkey|settings|rm|mv|cp|mkdir|touch|truncate|dd|tee)["'']',
        '(?is)["'']shell["''].{0,100}["'']pm["''].{0,50}["'']clear["'']'
    )
    foreach ($pattern in $forbiddenLiteralPatterns) {
        Assert-True (-not [regex]::IsMatch($source, $pattern)) "Static audit found a mutating ADB operation: $pattern"
    }
    Assert-True ($source -notmatch '-Executable\s+\$(?:resolvedAdb|Adb)\s+-Arguments\s+\$(?!\()') 'Static audit found a dynamically supplied ADB argument list.'

    $allowedAdb = @(
        '^devices\t-l$',
        '^-s\taffected-device\tshell\tpidof\tcom\.sts2launcher\.overhaul\.fork\.local$',
        '^-s\taffected-device\tshell\tgetprop\tro\.(?:product\.model|build\.fingerprint)$',
        '^-s\taffected-device\tshell\tpm\tpath\tcom\.sts2launcher\.overhaul\.fork\.local$',
        '^-s\taffected-device\tshell\tpm\tlist\tpackages\t-U\tcom\.sts2launcher\.overhaul\.fork\.local$',
        '^-s\taffected-device\tshell\tdumpsys\tpackage\tcom\.sts2launcher\.overhaul\.fork\.local$',
        '^-s\taffected-device\tpull\t/data/app/affected/base\.apk\t.+$'
    )
    foreach ($invocation in [IO.File]::ReadAllLines($adbLog)) {
        Assert-True (@($allowedAdb | Where-Object { $invocation -match $_ }).Count -eq 1) "Unexpected ADB operation observed: $invocation"
    }

    Write-Host 'Stage 5 affected-device preflight tests passed: pre/post exact identity, fail-closed version/signer/process/continuity, and no mutating ADB operations.'
} finally {
    foreach ($name in $environmentNames) {
        [Environment]::SetEnvironmentVariable($name, $oldEnvironment[$name], [EnvironmentVariableTarget]::Process)
    }
    if (Test-Path -LiteralPath $testRoot) {
        Remove-Item -LiteralPath $testRoot -Recurse -Force
    }
}
