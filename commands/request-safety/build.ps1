param([switch]$SkipPublish, [switch]$SeedToolProject, [switch]$ReleaseAssets)
$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$buildRoot = Join-Path $repoRoot '.local/request-safety'
$framework = Join-Path $buildRoot 'deps/MCP-Plugin-dotnet'
$server = Join-Path $buildRoot 'deps/GameDev-MCP-Server'
$feed = Join-Path $buildRoot 'feed'
$evidence = Join-Path $buildRoot 'results'
$sourceDigest = (Get-FileHash -LiteralPath (Join-Path $PSScriptRoot 'sources.json') -Algorithm SHA256).Hash.Substring(0, 12).ToLowerInvariant()
$previousHttpCache = $env:NUGET_HTTP_CACHE_PATH
$env:NUGET_HTTP_CACHE_PATH = Join-Path $buildRoot 'http-cache'
$proxy = $null
Push-Location $repoRoot
try {
    & (Join-Path $PSScriptRoot 'prepare.ps1')
    New-Item -ItemType Directory -Force $feed, $evidence | Out-Null
    & node -e 'fetch("http://127.0.0.1:18763/request-safety-health").then(async r=>{if(await r.text()!=="unity-mcp-request-safety-nuget-v1")process.exitCode=1}).catch(()=>process.exitCode=1)' 2>$null
    if ($LASTEXITCODE -ne 0) {
        $proxyOptions = @{ FilePath = (Get-Command node).Source; ArgumentList = ('"' + (Join-Path $PSScriptRoot 'nuget-proxy.cjs') + '"'); PassThru = $true }
        if ($env:OS -eq 'Windows_NT') { $proxyOptions.WindowStyle = 'Hidden' }
        $proxy = Start-Process @proxyOptions
        Start-Sleep -Milliseconds 500
        & node -e 'fetch("http://127.0.0.1:18763/request-safety-health").then(async r=>{if(await r.text()!=="unity-mcp-request-safety-nuget-v1")process.exitCode=1}).catch(()=>process.exitCode=1)'
        if ($LASTEXITCODE -ne 0) { throw 'Loopback NuGet proxy failed to start or its port is occupied.' }
    }
    $restore = @('-m:1', '-p:BuildInParallel=false', "-p:RestoreConfigFile=$(Join-Path $PSScriptRoot 'NuGet.Config')",
        "-p:RestorePackagesPath=$(Join-Path $buildRoot "nuget/$sourceDigest")", "-p:RestoreAdditionalProjectSources=$feed", '-p:NuGetAudit=false')
    function Invoke-Dotnet([string[]]$Arguments) {
        & dotnet @Arguments @restore
        if ($LASTEXITCODE -ne 0) { throw "dotnet $($Arguments[0]) failed." }
    }
    Invoke-Dotnet -Arguments @('pack', (Join-Path $framework 'McpPlugin.Common/McpPlugin.Common.csproj'), '-c', 'Release', '-o', $feed)
    Invoke-Dotnet -Arguments @('pack', (Join-Path $framework 'McpPlugin/McpPlugin.csproj'), '-c', 'Release', '-o', $feed)
    Invoke-Dotnet -Arguments @('test', (Join-Path $framework 'McpPlugin.Server.Tests/McpPlugin.Server.Tests.csproj'), '-c', 'Release', '-f', 'net9.0', '--filter', 'RequestExecution', '--logger', 'trx', '--results-directory', $evidence)
    $regressionFilter = 'FullyQualifiedName~ClientUtilsTokenRouting|FullyQualifiedName~DirectTool|FullyQualifiedName~Pinned|FullyQualifiedName~ProjectPinV2|FullyQualifiedName~NotificationRouting|FullyQualifiedName~SelectionRouting|FullyQualifiedName~PerSessionExecution|FullyQualifiedName~AgentActionableErrors'
    Invoke-Dotnet -Arguments @('test', (Join-Path $framework 'McpPlugin.Server.Tests/McpPlugin.Server.Tests.csproj'), '-c', 'Release', '-f', 'net9.0', '--no-build', '--no-restore', '--filter', $regressionFilter, '--logger', 'trx', '--results-directory', $evidence)
    Invoke-Dotnet -Arguments @('test', 'Unity-MCP-Plugin/Tests~/EngineFree/EngineFree.csproj', '-c', 'Release', '--logger', 'trx', '--results-directory', $evidence)
    & python .github/scripts/check_nuget_gate.py
    if ($LASTEXITCODE -ne 0) { throw 'NuGet generation gate failed.' }
    if ($ReleaseAssets) {
        & python (Join-Path $PSScriptRoot 'release_assets.py') check-config
        if ($LASTEXITCODE -ne 0) { throw 'Release pin validation failed.' }
        foreach ($rid in 'win-x64', 'win-x86', 'win-arm64', 'linux-x64', 'linux-arm64', 'osx-x64', 'osx-arm64') {
            Invoke-Dotnet -Arguments @('publish', (Join-Path $server 'com.IvanMurzak.GameDev.MCP.Server.csproj'),
                '-c', 'Release', '-r', $rid, '--self-contained', 'true',
                '-p:PublishSingleFile=true', '-p:PublishTrimmed=false', '-p:PublishAot=false',
                '-p:IncludeNativeLibrariesForSelfExtract=true',
                '-o', (Join-Path $buildRoot "release-publish/$rid"))
        }
        & python (Join-Path $PSScriptRoot 'release_assets.py') assemble
        if ($LASTEXITCODE -ne 0) { throw 'Release asset validation failed.' }
    }
    if (!$SkipPublish) {
        # Local artifact generation only; no network publication, deployment or release.
        Invoke-Dotnet -Arguments @('publish', (Join-Path $server 'com.IvanMurzak.GameDev.MCP.Server.csproj'), '-c', 'Release', '-r', 'win-x64', '--self-contained', 'false', '-o', (Join-Path $buildRoot 'publish'))
    }
    if ($SeedToolProject) {
        $unity = Join-Path $repoRoot 'Unity-MCP-Plugin'
        $cache = Join-Path $unity 'Library/NuGetCache'
        $plugins = Join-Path $unity 'Assets/Plugins/NuGet'
        New-Item -ItemType Directory -Force $cache, $plugins | Out-Null
        $version = (Get-Content (Join-Path $PSScriptRoot 'sources.json') -Raw | ConvertFrom-Json).packageVersion
        foreach ($name in 'McpPlugin', 'McpPlugin.Common') {
            Copy-Item -LiteralPath (Join-Path $framework "$name/bin/Release/netstandard2.1/$name.dll") -Destination $plugins
            Copy-Item -LiteralPath (Join-Path $feed "com.IvanMurzak.$name.$version.nupkg") -Destination $cache
        }
        $manifestPath = Join-Path $plugins '.nuget-installed.json'
        if (Test-Path -LiteralPath $manifestPath) {
            $installed = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
            $installed.packages.'com.IvanMurzak.McpPlugin'.version = $version
            $installed.packages.'com.IvanMurzak.McpPlugin.Common'.version = $version
            [IO.File]::WriteAllText($manifestPath, ($installed | ConvertTo-Json -Depth 20).Replace("`r`n", "`n") + "`n")
        }
        Write-Host 'Seeded paired local dependencies in the Unity-MCP tool project only.'
    }
    Write-Host "Request-safety build and tests complete. Local artifacts: $buildRoot"
} finally {
    if ($null -ne $proxy -and !$proxy.HasExited) { Stop-Process -Id $proxy.Id }
    $env:NUGET_HTTP_CACHE_PATH = $previousHttpCache
    Pop-Location
}
