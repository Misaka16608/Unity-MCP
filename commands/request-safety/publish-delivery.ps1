param([switch]$Publish)
$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
Push-Location $repoRoot
try {
    & python (Join-Path $PSScriptRoot 'verify-delivery.py')
    if ($LASTEXITCODE -ne 0) { throw 'Sealed delivery verification failed; nothing uploaded.' }
    if (@(& git status --porcelain).Count -ne 0) { throw 'Commit the reviewed candidate before creating its release.' }
    if (!(Get-Command gh -ErrorAction SilentlyContinue)) { throw 'An authenticated GitHub CLI is required to publish this candidate.' }
    & gh auth status --hostname github.com
    if ($LASTEXITCODE -ne 0) { throw 'GitHub CLI authentication is missing.' }
    $manifest = Get-Content Unity-MCP-Plugin/Packages/com.ivanmurzak.unity.mcp/Editor/Delivery~/manifest.json -Raw | ConvertFrom-Json
    $uri = [Uri]$manifest.releaseBaseUrl
    $parts = $uri.AbsolutePath.Trim('/').Split('/')
    $repository = "$($parts[0])/$($parts[1])"
    $tag = $parts[-1]
    $commit = (& git rev-parse HEAD).Trim()
    $assets = @(Get-ChildItem .local/request-safety/delivery/assets -File | ForEach-Object FullName)
    $arguments = @('release', 'create', $tag) + $assets + @('--repo', $repository, '--target', $commit, '--prerelease',
        '--title', "Paired native Unity MCP $($manifest.pluginVersion)", '--notes-file', (Join-Path $PSScriptRoot 'DELIVERY.md'))
    if (!$Publish) { $arguments += '--draft' }
    & gh @arguments
    if ($LASTEXITCODE -ne 0) { throw 'Release creation failed. Existing tags/assets were not overwritten.' }
} finally { Pop-Location }
