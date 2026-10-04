$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$buildRoot = Join-Path $repoRoot '.local/request-safety'
$target = Join-Path $buildRoot ('delivery/smoke-' + [Guid]::NewGuid().ToString('N'))
& python (Join-Path $PSScriptRoot 'verify-delivery.py') --extract-windows $target
if ($LASTEXITCODE -ne 0) { throw 'Delivery verification failed.' }
$listener = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback, 0)
$listener.Start()
$port = $listener.LocalEndpoint.Port
$listener.Stop()
$sourceDigest = (Get-FileHash -LiteralPath (Join-Path $PSScriptRoot 'sources.json') -Algorithm SHA256).Hash.Substring(0,12).ToLowerInvariant()
$proxyOptions = @{ FilePath = (Get-Command node).Source; ArgumentList = ('"' + (Join-Path $PSScriptRoot 'nuget-proxy.cjs') + '"'); PassThru = $true; WindowStyle = 'Hidden' }
$proxy = $null
$server = $null
function New-IsolatedProcess([string]$executable, [string[]]$arguments, [string]$workingDirectory) {
    $info = [Diagnostics.ProcessStartInfo]::new()
    $info.FileName = $executable
    $info.WorkingDirectory = $workingDirectory
    $info.UseShellExecute = $false
    $info.CreateNoWindow = $true
    $info.RedirectStandardOutput = $true
    $info.RedirectStandardError = $true
    foreach ($argument in $arguments) { $info.ArgumentList.Add($argument) }
    foreach ($key in @($info.Environment.Keys)) {
        if ($key -like 'MCP_*' -or $key -like 'UNITY_MCP_*' -or $key -like 'ASPNETCORE_*' -or $key -like 'REDIS*') {
            $info.Environment.Remove($key) | Out-Null
        }
    }
    $info.Environment['DOTNET_BUNDLE_EXTRACT_BASE_DIR'] = Join-Path $target 'bundle-extract'
    $process = [Diagnostics.Process]::new()
    $process.StartInfo = $info
    if (!$process.Start()) { throw 'Could not start isolated delivery process.' }
    return $process
}
try {
    & node -e 'fetch("http://127.0.0.1:18763/request-safety-health").then(async r=>{if(await r.text()!=="unity-mcp-request-safety-nuget-v1")process.exitCode=1}).catch(()=>process.exitCode=1)' 2>$null
    if ($LASTEXITCODE -ne 0) {
        $proxy = Start-Process @proxyOptions
        Start-Sleep -Milliseconds 500
    }
    & dotnet build (Join-Path $PSScriptRoot 'Smoke/Smoke.csproj') -c Release -m:1 -p:BuildInParallel=false `
        "-p:RestoreConfigFile=$(Join-Path $PSScriptRoot 'NuGet.Config')" "-p:RestorePackagesPath=$(Join-Path $buildRoot "nuget/$sourceDigest")"
    if ($LASTEXITCODE -ne 0) { throw 'Could not build the isolated smoke client.' }
    $server = New-IsolatedProcess (Join-Path $target 'gamedev-mcp-server.exe') @("port=$port", 'client-transport=streamableHttp', 'auth=none', 'bind=127.0.0.1') $target
    $serverOut = $server.StandardOutput.ReadToEndAsync()
    $serverError = $server.StandardError.ReadToEndAsync()
    $ready = $false
    for ($attempt = 0; $attempt -lt 40; $attempt++) {
        if ($server.HasExited) { throw 'Packaged server exited before startup.' }
        & node -e "fetch('http://127.0.0.1:$port/help').then(r=>{if(!r.ok)process.exitCode=1}).catch(()=>process.exitCode=1)"
        if ($LASTEXITCODE -eq 0) { $ready = $true; break }
        Start-Sleep -Milliseconds 250
    }
    if (!$ready) { throw 'Packaged native server did not become ready.' }
    # Invoke the DLL directly: no dotnet-run wrapper/apphost child can outlive
    # the tracked diagnostic PID. Managed failures are caught and logged in Main.
    $arguments = @((Join-Path $PSScriptRoot 'Smoke/bin/Release/net9.0/Smoke.dll'), "http://127.0.0.1:$port/")
    $client = New-IsolatedProcess (Get-Command dotnet).Source $arguments $repoRoot
    $clientOut = $client.StandardOutput.ReadToEndAsync()
    $clientError = $client.StandardError.ReadToEndAsync()
    $timedOut = !$client.WaitForExit(45000)
    if ($timedOut) { $client.Kill($true); $client.WaitForExit(5000) | Out-Null }
    $output = $clientOut.GetAwaiter().GetResult() + $clientError.GetAwaiter().GetResult()
    [IO.File]::WriteAllText((Join-Path $target 'plugin-smoke.log'), $output)
    if ($timedOut) { throw "Isolated plugin smoke timed out; see $target/plugin-smoke.log" }
    if ($client.ExitCode -ne 0) { throw "Plugin smoke failed; see $target/plugin-smoke.log" }
    Write-Host $output
    Write-Host "Delivery smoke used only its own server PID $($server.Id), loopback port $port and scratch directory $target"
} finally {
    if ($null -ne $server) {
        if (!$server.HasExited) { $server.Kill(); $server.WaitForExit(5000) | Out-Null }
        [IO.File]::WriteAllText((Join-Path $target 'server.log'), $serverOut.GetAwaiter().GetResult() + $serverError.GetAwaiter().GetResult())
        $server.Dispose()
    }
    if ($null -ne $proxy -and !$proxy.HasExited) { Stop-Process -Id $proxy.Id }
}
