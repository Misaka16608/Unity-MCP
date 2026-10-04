$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$buildRoot = Join-Path $repoRoot '.local/request-safety'
$sources = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'sources.json') -Raw | ConvertFrom-Json

function Get-SourceHash([string]$path) {
    $normalized = [IO.File]::ReadAllText($path).Replace("`r`n", "`n")
    $sha = [Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($normalized)))).Replace('-', '').ToLowerInvariant() }
    finally { $sha.Dispose() }
}
function Test-SourceFiles($source, [string]$target, [string]$hashField) {
    foreach ($file in $source.files) {
        $fullPath = Join-Path $target $file.path
        $expected = $file.$hashField
        if ($null -eq $expected) {
            if (Test-Path -LiteralPath $fullPath) { throw "Unexpected source already exists: $fullPath" }
        } elseif (!(Test-Path -LiteralPath $fullPath) -or (Get-SourceHash $fullPath) -ne $expected) {
            throw "Source checksum mismatch: $fullPath. Preserve and inspect this isolated checkout; it will not be overwritten."
        }
    }
}

& node (Join-Path $PSScriptRoot 'fetch-sources.cjs')
if ($LASTEXITCODE -ne 0) { throw 'Pinned source download failed.' }
$previousCeiling = $env:GIT_CEILING_DIRECTORIES
try {
    foreach ($source in $sources.sources) {
        $target = Join-Path $buildRoot ('deps/' + $source.repo)
        if (Test-Path -LiteralPath $target) {
            Test-SourceFiles $source $target 'patchedSha256'
            continue
        }
        $staging = Join-Path $buildRoot ('extract-' + [Guid]::NewGuid().ToString('N'))
        Expand-Archive -LiteralPath (Join-Path $buildRoot ($source.repo + '.zip')) -DestinationPath $staging
        $roots = @(Get-ChildItem -LiteralPath $staging -Directory)
        if ($roots.Count -ne 1) { throw "Unexpected archive layout for $($source.repo)." }
        Test-SourceFiles $source $roots[0].FullName 'baseSha256'
        # Both paths are resolved beneath the explicitly isolated build root before moving.
        $resolvedSource = [IO.Path]::GetFullPath($roots[0].FullName)
        $resolvedTarget = [IO.Path]::GetFullPath($target)
        $prefix = [IO.Path]::GetFullPath($buildRoot) + [IO.Path]::DirectorySeparatorChar
        if (!$resolvedSource.StartsWith($prefix) -or !$resolvedTarget.StartsWith($prefix)) { throw 'Extraction escaped the build workspace.' }
        New-Item -ItemType Directory -Force (Split-Path $target) | Out-Null
        Move-Item -LiteralPath $resolvedSource -Destination $resolvedTarget
        # Do not discover the enclosing Unity-MCP Git index. Apply to this source tree only.
        $env:GIT_CEILING_DIRECTORIES = Split-Path $target
        Push-Location $target
        try {
            $patch = Join-Path $PSScriptRoot $source.patch
            & git apply --check $patch
            if ($LASTEXITCODE -ne 0) { throw "Patch preflight failed for $($source.repo)." }
            & git apply $patch
            if ($LASTEXITCODE -ne 0) { throw "Patch failed for $($source.repo)." }
        } finally { Pop-Location }
        Test-SourceFiles $source $target 'patchedSha256'
        Write-Host "Prepared $($source.repo) @ $($source.commit) with verified request-safety patch."
    }
} finally { $env:GIT_CEILING_DIRECTORIES = $previousCeiling }
