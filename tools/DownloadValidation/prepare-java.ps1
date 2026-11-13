param([Parameter(Mandatory)][string]$OutputDirectory)
$ErrorActionPreference='Stop'
$repo=(Resolve-Path "$PSScriptRoot/../..").Path
$output=[IO.Path]::GetFullPath($OutputDirectory)
$ownedRoot=[IO.Path]::GetFullPath((Join-Path $repo 'tmp'))+[IO.Path]::DirectorySeparatorChar
if(-not $output.StartsWith($ownedRoot,[StringComparison]::OrdinalIgnoreCase)){throw 'Generated Java must stay under repository tmp.'}
$java=[IO.File]::ReadAllText("$repo/android/src/com/game/sts2launcher/GodotApp.java").Replace("`r`n","`n")
function Once([string]$text,[string]$before,[string]$after){
    if([regex]::Matches($text,[regex]::Escape($before)).Count-ne 1){throw "Java validation boundary drift: $before"}
    return $text.Replace($before,$after)
}
$start=$java.IndexOf('public String httpRequestAsyncStart(')
$end=$java.IndexOf('public String httpRequestAsyncPoll(',$start)
$async=$java.Substring($start,$end-$start)
$async=Once $async 'Thread worker = new Thread(() -> {' "DownloadValidationHttpDiagnostics.accept(this, requestId, method, urlString, headersJson, bodyBase64, timeoutMs);`n`t`tThread worker = new Thread(() -> {`n`t`t`tDownloadValidationHttpDiagnostics.workerEnter(requestId);`n`t`t`ttry {"
$async=Once $async 'if (canceledHttpRequests.remove(requestId) != null)' "DownloadValidationHttpDiagnostics.response(requestId, response);`n`t`t`tif (canceledHttpRequests.remove(requestId) != null)"
$async=Once $async 'httpRequestResponses.put(requestId, new AsyncHttpResponse(response));' "httpRequestResponses.put(requestId, new AsyncHttpResponse(response));`n`t`t`t} finally { DownloadValidationHttpDiagnostics.workerExit(requestId); }"
$java=$java.Substring(0,$start)+$async+$java.Substring($end)
$cancel='public void httpRequestAsyncCancel(String requestId) {'
$java=Once $java $cancel ($cancel+"`n`t`tDownloadValidationHttpDiagnostics.cancel(requestId);")
$start=$java.IndexOf('private String performHttpRequest(')
$end=$java.IndexOf('private byte[] readFullyLimited(',$start)
$http=$java.Substring($start,$end-$start)
$http=Once $http 'int status = connection.getResponseCode();' "DownloadValidationHttpDiagnostics.phase(`"connection-start`" );`n`t`t`tint status = connection.getResponseCode();`n`t`t`tDownloadValidationHttpDiagnostics.headers(status);"
$line='InputStream stream = status >= 400 ? connection.getErrorStream() : connection.getInputStream();'
$http=Once $http $line ($line+"`n`t`t`tstream = DownloadValidationHttpDiagnostics.track(stream);")
$http=Once $http '} catch (Exception e) {' "} catch (Exception e) {`n`t`t`tDownloadValidationHttpDiagnostics.error(e);"
$http=Once $http 'connection.disconnect();' "DownloadValidationHttpDiagnostics.phase(`"disconnect-start`" );`n`t`t`t`tconnection.disconnect();`n`t`t`t`tDownloadValidationHttpDiagnostics.phase(`"disconnect-finished`" );"
$java=$java.Substring(0,$start)+$http+$java.Substring($end)
$java=Once $java 'return File.createTempFile("sts2_cdn_", ".bin", dir);' 'return DownloadValidationHttpDiagnostics.createdFile(File.createTempFile("sts2_cdn_", ".bin", dir));'
$methods=@'
    public String configureDownloadValidationHttp(String runId) throws Exception {
        return DownloadValidationHttpDiagnostics.configure(this, runId);
    }
    public String getDownloadValidationHttpState() throws Exception { return DownloadValidationHttpDiagnostics.state(); }
    public String getDownloadValidationDnsAfter(String requestId) throws Exception { return DownloadValidationHttpDiagnostics.dnsAfter(requestId); }
    public String replayDownloadValidationResource(String kind) throws Exception {
        return DownloadValidationHttpDiagnostics.replay(kind, (method, url, headers, body, timeout) -> performHttpRequest(method, url, headers, body, timeout));
    }
    public String takeDownloadValidationReplay(String requestId) { return DownloadValidationHttpDiagnostics.takeResponse(requestId); }

'@
$java=Once $java 'public String httpRequestAsyncStart(' ($methods+'    public String httpRequestAsyncStart(')
$package=Join-Path $output 'com/game/sts2launcher'
New-Item -ItemType Directory -Force $package | Out-Null
[IO.File]::WriteAllText("$package/GodotApp.java",$java)
Copy-Item -LiteralPath "$PSScriptRoot/DownloadValidationHttpDiagnostics.java" -Destination "$package/DownloadValidationHttpDiagnostics.java" -Force
Write-Output 'Generated timing-only Java sources; production source unchanged.'
