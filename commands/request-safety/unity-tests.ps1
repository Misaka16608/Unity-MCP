param([Parameter(Mandatory = $true)][string]$UnityExe)
$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$toolProject = Join-Path $repoRoot 'Unity-MCP-Plugin'
$project = Join-Path $repoRoot ('.local/request-safety/unity-isolated-' + [Guid]::NewGuid().ToString('N'))
if (!(Test-Path -LiteralPath $UnityExe -PathType Leaf)) { throw 'Unity executable does not exist.' }
# Every run creates an isolated project; no existing editor, project or MCP endpoint is used.
New-Item -ItemType Directory -Force (Join-Path $project 'Assets/Plugins'), (Join-Path $project 'Packages') | Out-Null
Copy-Item -LiteralPath (Join-Path $toolProject 'ProjectSettings') -Destination $project -Recurse
Copy-Item -LiteralPath (Join-Path $toolProject 'Assets/Plugins/NuGet') -Destination (Join-Path $project 'Assets/Plugins') -Recurse
Copy-Item -LiteralPath (Join-Path $toolProject 'Packages/com.ivanmurzak.unity.mcp') -Destination (Join-Path $project 'Packages') -Recurse
foreach ($name in 'com.unity.test-framework', 'com.unity.ext.nunit', 'extensions.unity.playerprefsex') {
    $cached = @(Get-ChildItem -LiteralPath (Join-Path $toolProject 'Library/PackageCache') -Directory | Where-Object Name -Like "$name@*")
    if ($cached.Count -ne 1) { throw "Expected one cached $name package in the tool project." }
    Copy-Item -LiteralPath $cached[0].FullName -Destination (Join-Path $project "Packages/$name") -Recurse
}
$manifest = @{
    dependencies = @{
        'com.ivanmurzak.unity.mcp' = 'file:com.ivanmurzak.unity.mcp'
        'com.unity.test-framework' = 'file:com.unity.test-framework'
        'com.unity.ext.nunit' = 'file:com.unity.ext.nunit'
        'extensions.unity.playerprefsex' = 'file:extensions.unity.playerprefsex'
        'com.unity.modules.uielements' = '1.0.0'
    }
    testables = @('com.ivanmurzak.unity.mcp')
}
[IO.File]::WriteAllText((Join-Path $project 'Packages/manifest.json'), ($manifest | ConvertTo-Json -Depth 8))
Write-Host "Isolated Unity project: $project"
$arguments = @('-batchmode', '-nographics', '-projectPath', ('"' + $project + '"'), '-runTests', '-testPlatform', 'EditMode',
    '-testFilter', 'RequestSafetyMainThreadTests', '-testResults', ('"' + (Join-Path $project 'results.xml') + '"'),
    '-logFile', ('"' + (Join-Path $project 'Editor.log') + '"'),
    '--UNITY_MCP_KEEP_CONNECTED=false', '--UNITY_MCP_START_SERVER=false', '--UNITY_MCP_CONNECTION_MODE=local')
$process = Start-Process -FilePath $UnityExe -ArgumentList $arguments -WindowStyle Hidden -PassThru
# Bounded execution; stop only the exact isolated process this script created.
$deadline = [DateTime]::UtcNow.AddMinutes(4)
while (!$process.HasExited -and [DateTime]::UtcNow -lt $deadline) {
    Start-Sleep -Seconds 1
    $process.Refresh()
    if ($process.MainWindowTitle -eq 'Sentinel LDK Protection System' -and !(Test-Path -LiteralPath (Join-Path $project 'Editor.log'))) {
        Stop-Process -Id $process.Id
        throw "Vendor Sentinel startup protection prevented this isolated Editor from initializing; no Unity tests ran. Project: $project"
    }
}
if (!$process.HasExited) { Stop-Process -Id $process.Id; throw "Isolated Unity test timed out; see $project/Editor.log" }
if ($process.ExitCode -ne 0) { throw "Isolated Unity exited $($process.ExitCode); see $project/Editor.log" }
if (!(Test-Path -LiteralPath (Join-Path $project 'results.xml'))) { throw "Unity produced no test results; see $project/Editor.log" }
[xml]$results = Get-Content -LiteralPath (Join-Path $project 'results.xml')
if ($results.'test-run'.failed -ne '0' -or [int]$results.'test-run'.passed -lt 2) { throw "Unity EditMode tests failed or did not run; see $project/results.xml" }
Write-Host "Isolated Unity EditMode tests passed: $project/results.xml"
