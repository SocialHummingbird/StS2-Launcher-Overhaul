param([Parameter(Mandatory)][string]$SourceDirectory, [Parameter(Mandatory)][string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$expectedOutput = [IO.Path]::GetFullPath((Join-Path $repo 'tmp/chunk-download-work/device/managed'))
if ([IO.Path]::GetFullPath($OutputDirectory) -ne $expectedOutput) { throw 'Generated source output escaped its test-owned directory' }
New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
$controller = [IO.File]::ReadAllText((Join-Path $SourceDirectory 'Launcher/LauncherController.cs'))
$start = [regex]::new('(?m)^    internal void Start\(\)\r?\n    \{')
if ($start.Matches($controller).Count -ne 1) { throw 'Expected exactly one launcher controller Start method' }
$controller = $start.Replace($controller, "    internal void Start()`r`n    {`r`n        if (DownloadValidationRunner.TryStart(_model, _view, _runOnMainThread)) return;", 1)
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'LauncherController.cs'), $controller)
$lifecycle = [IO.File]::ReadAllText((Join-Path $SourceDirectory 'Launcher/LauncherUI.Lifecycle.cs'))
if ([regex]::Matches($lifecycle, '            AutoLaunchIfRequested\(\);').Count -ne 1) { throw 'Expected exactly one launcher automatic launch call' }
$lifecycle = $lifecycle.Replace('            AutoLaunchIfRequested();', '            if (!DownloadValidationRunner.Enabled) AutoLaunchIfRequested();')
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'LauncherUI.Lifecycle.cs'), $lifecycle)
$depot = [IO.File]::ReadAllText((Join-Path $SourceDirectory 'Steam/DepotDownloader.DepotDownload.cs'))
$begin = '        var loaded = await LoadDepotManifestAsync(depot, ct).ConfigureAwait(false);'
if ([regex]::Matches($depot, [regex]::Escape($begin)).Count -ne 1) { throw 'Expected exactly one main-game manifest load' }
$depot = $depot.Replace($begin, '        ApplyValidationProvider();' + "`r`n" + $begin)
$fetch = '(chunk, buffer, token) => DownloadChunkWithRetriesAsync(depot.DepotId, chunk, buffer, loaded.DepotKey, "game content", token),'
$observedFetch = '(chunk, buffer, token) => ValidationFetchAsync(depot.DepotId, chunk, token, () => DownloadChunkWithRetriesAsync(depot.DepotId, chunk, buffer, loaded.DepotKey, "game content", token)),'
if ([regex]::Matches($depot, [regex]::Escape($fetch)).Count -ne 1) { throw 'Expected exactly one main-game chunk fetch callback' }
$depot = $depot.Replace($fetch, $observedFetch)
$progress = 'progress => { last = progress; ReportChunkProgress(progress); },'
$observedProgress = 'progress => { last = progress; ValidationRawProgressChanged?.Invoke(depot.DepotId, progress); ReportChunkProgress(progress); },'
if ([regex]::Matches($depot, [regex]::Escape($progress)).Count -ne 1) { throw 'Expected exactly one main-game raw progress callback' }
$depot = $depot.Replace($progress, $observedProgress)
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'DepotDownloader.DepotDownload.cs'), $depot)
$http = [IO.File]::ReadAllText((Join-Path $SourceDirectory 'Steam/AndroidJavaHttpMessageHandler.Request.cs'))
$startHttp = [regex]::new('(?s)(var requestId = StartHttpRequestBridge\(\s*request,\s*bodyBytes\s*\);)')
if ($startHttp.Matches($http).Count -ne 1) { throw 'Expected exactly one HTTP bridge start call' }
$http = $startHttp.Replace($http, "var validationDispatchStarted = Environment.TickCount64;`r`n            " + '$1' + "`r`n            ValidationHttpStart(requestId, request.RequestUri, validationDispatchStarted);", 1)
$pollHttp = 'var response = PollHttpRequestBridge(requestId);'
if ([regex]::Matches($http, [regex]::Escape($pollHttp)).Count -ne 1) { throw 'Expected exactly one HTTP bridge poll call' }
$http = $http.Replace($pollHttp, "var validationPollStarted = Environment.TickCount64;`r`n                " + $pollHttp + "`r`n                ValidationHttpPoll(requestId, !string.IsNullOrEmpty(response), validationPollStarted);")
$cancelHttp = 'CancelHttpRequestBridge(requestId);'
if ([regex]::Matches($http, [regex]::Escape($cancelHttp)).Count -ne 3) { throw 'Expected exactly three HTTP bridge cancellation call sites' }
$http = $http.Replace($cancelHttp, 'ValidationHttpCancel(requestId); ' + $cancelHttp)
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'AndroidJavaHttpMessageHandler.Request.cs'), $http)
$download = [IO.File]::ReadAllText((Join-Path $SourceDirectory 'Steam/DepotDownloader.cs'))
$directory = '_servers = await LoadCdnServersAsync(ct);'
if ([regex]::Matches($download, [regex]::Escape($directory)).Count -ne 1) { throw 'Expected exactly one main-game CDN directory load' }
$download = $download.Replace($directory, '_servers = await LoadValidationCdnServersAsync(ct);')
$preparation = '            installUpdate.UpdatePhase("preparing-android-pck");'
if ([regex]::Matches($download, [regex]::Escape($preparation)).Count -ne 1) { throw 'Expected exactly one Android preparation boundary.' }
$download = $download.Replace($preparation, '            await ValidationBeforePckPreparationAsync().ConfigureAwait(false);' + "`r`n" + $preparation)
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'DepotDownloader.cs'), $download)
$cdn = [IO.File]::ReadAllText((Join-Path $SourceDirectory 'Steam/DepotDownloader.CdnDownloadOperation.cs'))
$chunkCall = '=> owner._cdnClient.DownloadDepotChunkAsync(depotId, chunk, Server, buffer, depotKey, cdnAuthToken: cdnAuthToken);'
$manifestCall = '=> owner._cdnClient.DownloadManifestAsync(depotId, manifestId, manifestRequestCode, Server, depotKey, cdnAuthToken: cdnAuthToken);'
foreach ($call in @($chunkCall, $manifestCall)) {
    if ([regex]::Matches($cdn, [regex]::Escape($call)).Count -ne 1) { throw 'Expected exactly one SDK request boundary.' }
}
$cdn = $cdn.Replace($chunkCall, '{ var server = Server; return owner.ValidationCdnRequest(server, Index, "chunk", depotId, Convert.ToHexString(chunk.ChunkID), () => owner._cdnClient.DownloadDepotChunkAsync(depotId, chunk, server, buffer, depotKey, cdnAuthToken: cdnAuthToken), cdnAuthToken); }')
$cdn = $cdn.Replace($manifestCall, '{ var server = Server; return owner.ValidationCdnRequest(server, Index, "manifest", depotId, manifestId.ToString(), () => owner._cdnClient.DownloadManifestAsync(depotId, manifestId, manifestRequestCode, server, depotKey, cdnAuthToken: cdnAuthToken), cdnAuthToken); }')
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'DepotDownloader.CdnDownloadOperation.cs'), $cdn)
$paths = [IO.File]::ReadAllText((Join-Path $SourceDirectory 'AppPaths.cs'))
$pathGetter = 'internal static string AppPrivateDataDir => ResolveAppPrivateDataDirectory();'
if ([regex]::Matches($paths, [regex]::Escape($pathGetter)).Count -ne 1) { throw 'Expected exactly one global private-data getter.' }
$paths = $paths.Replace($pathGetter, 'internal static string ValidationDataRoot; internal static string AppPrivateDataDir => ValidationDataRoot ?? ResolveAppPrivateDataDirectory();')
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'AppPaths.cs'), $paths)
$modelDownloads = [IO.File]::ReadAllText((Join-Path $SourceDirectory 'Launcher/LauncherModel.Downloads.cs'))
$created = '        downloader.LogMessage += RaiseDownloadLogReceived;'
if ([regex]::Matches($modelDownloads, [regex]::Escape($created)).Count -ne 1) { throw 'Expected exactly one launcher downloader factory.' }
$modelDownloads = $modelDownloads.Replace($created, $created + "`r`n        SandboxDownloadValidation.ObserveDownloader(downloader);")
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'LauncherModel.Downloads.cs'), $modelDownloads)
Write-Host 'Prepared validation-only startup interception and main-game chunk/progress observation copies.'
