param(
    [string]$AndroidHome = "$(Join-Path $env:USERPROFILE '.w40k-android-toolchain\android-sdk')",
    [string]$JavaHome = "$(Join-Path $env:USERPROFILE '.w40k-android-toolchain\jdk-17')",
    [string]$OutputDirectory = ""
)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$output = if ($OutputDirectory) { [IO.Path]::GetFullPath($OutputDirectory) } else { Join-Path $repo 'tmp/chunk-download-work/device/helper' }
$ownedRoot = [IO.Path]::GetFullPath((Join-Path $repo 'tmp')) + [IO.Path]::DirectorySeparatorChar
if (-not $output.StartsWith($ownedRoot, [StringComparison]::OrdinalIgnoreCase)) { throw 'Helper output must stay under repository tmp.' }
$classes = Join-Path $output 'classes'
$dex = Join-Path $output 'dex'
New-Item -ItemType Directory -Force $classes, $dex | Out-Null
$env:JAVA_HOME = $JavaHome
$env:ANDROID_HOME = $AndroidHome
$buildTools = Join-Path $AndroidHome 'build-tools/35.0.0'
$androidJar = Join-Path $AndroidHome 'platforms/android-35/android.jar'
function Checked([string]$tool, [string[]]$toolArguments) {
    & $tool @toolArguments
    if ($LASTEXITCODE -ne 0) { throw "Validation helper tool failed: $tool" }
}
Checked "$JavaHome/bin/javac.exe" @('-encoding','UTF-8','-source','8','-target','8','-bootclasspath',$androidJar,'-d',$classes,"$PSScriptRoot/Preservation.java")
Checked "$JavaHome/bin/jar.exe" @('cf',"$output/classes.jar",'-C',$classes,'.')
Checked "$buildTools/d8.bat" @('--lib',$androidJar,'--min-api','24','--output',$dex,"$output/classes.jar")
Checked "$buildTools/aapt.exe" @('package','-f','-M',"$PSScriptRoot/AndroidManifest.xml",'-I',$androidJar,'-F',"$output/unsigned.apk")
Push-Location $dex
try { Checked "$buildTools/aapt.exe" @('add',"$output/unsigned.apk",'classes.dex') }
finally { Pop-Location }
Checked "$buildTools/zipalign.exe" @('-f','4',"$output/unsigned.apk","$output/aligned.apk")
Checked "$buildTools/apksigner.bat" @('sign','--ks',"$repo/tmp/localtest.keystore",'--ks-key-alias','androiddebugkey','--ks-pass','pass:android','--out',"$output/download-validation-helper.apk","$output/aligned.apk")
. "$repo/scripts/android-signing-utils.ps1"
$identity = Get-AndroidApkIdentity -Path "$output/download-validation-helper.apk"
$identity | ConvertTo-Json | Set-Content -LiteralPath "$output/helper-identity.json" -Encoding utf8
Write-Host "Built validation helper: $output/download-validation-helper.apk"
Write-Host "Signer SHA256: $($identity.signerSha256)"
