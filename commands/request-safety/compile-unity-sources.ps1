param([string]$UnityProject)
$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
if (!$UnityProject) { $UnityProject = Join-Path $repoRoot 'Unity-MCP-Plugin' }
$UnityProject = [IO.Path]::GetFullPath($UnityProject)
$output = Join-Path $repoRoot '.local/request-safety/compile-check'
New-Item -ItemType Directory -Force $output | Out-Null
# Read existing generated references only. No editor is started and no project is regenerated.
$generated = Join-Path $UnityProject 'com.IvanMurzak.Unity.MCP.Editor.Tests.csproj'
if (!(Test-Path -LiteralPath $generated)) { throw "Missing generated Unity reference project: $generated. This check needs real installed Unity assemblies; it never creates substitutes." }
[xml]$referenceProject = Get-Content -LiteralPath $generated -Raw
$references = @{}
foreach ($reference in $referenceProject.SelectNodes("//*[local-name()='Reference']")) {
    $hint = $reference.SelectSingleNode("*[local-name()='HintPath']")
    if ($null -eq $hint) { continue }
    $path = $hint.InnerText
    if (![IO.Path]::IsPathRooted($path)) { $path = Join-Path $UnityProject $path }
    $path = [IO.Path]::GetFullPath($path)
    # The SDK supplies the standard reference profile; keep actual Unity and package DLLs.
    if ($path -match '[\\/]UnityReferenceAssemblies[\\/]') { continue }
    if (!(Test-Path -LiteralPath $path)) { throw "Missing actual Unity/package reference: $path" }
    $references[$reference.GetAttribute('Include')] = $path
}
foreach ($reference in $referenceProject.SelectNodes("//*[local-name()='ProjectReference']")) {
    $name = [IO.Path]::GetFileNameWithoutExtension($reference.GetAttribute('Include'))
    $path = Join-Path $UnityProject "Library/ScriptAssemblies/$name.dll"
    if (!(Test-Path -LiteralPath $path)) { throw "Missing actual compiled Unity dependency: $path" }
    $references[$name] = $path
}
function Escape-Xml([string]$value) { [Security.SecurityElement]::Escape($value) }
$package = Join-Path $repoRoot 'Unity-MCP-Plugin/Packages/com.ivanmurzak.unity.mcp'
foreach ($mode in 'Editor', 'Player') {
    $defines = if ($mode -eq 'Editor') { 'UNITY_EDITOR;UNITY_2022_3' } else { 'UNITY_2022_3' }
    $sources = @('Runtime/Utils/QueuedExecution.cs', 'Runtime/Utils/DurableOperationStore.cs', "Runtime/Utils/MainThread.$mode.cs")
    if ($mode -eq 'Editor') { $sources += @('Editor/Scripts/API/Tool/Operation.cs', 'Tests/Editor/RequestSafetyMainThreadTests.cs') }
    $items = foreach ($source in $sources) { '<Compile Include="' + (Escape-Xml (Join-Path $package $source)) + '" />' }
    $items += foreach ($name in $references.Keys | Sort-Object) {
        '<Reference Include="' + (Escape-Xml $name) + '"><HintPath>' + (Escape-Xml $references[$name]) + '</HintPath></Reference>'
    }
    $project = Join-Path $output "UnitySourceCheck.$mode.csproj"
    $xml = '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>netstandard2.1</TargetFramework><LangVersion>9.0</LangVersion><Nullable>enable</Nullable><EnableDefaultCompileItems>false</EnableDefaultCompileItems><DefineConstants>' + $defines + '</DefineConstants><NoWarn>0436</NoWarn><NuGetAudit>false</NuGetAudit></PropertyGroup><ItemGroup>' + ($items -join "`n") + '</ItemGroup></Project>'
    [IO.File]::WriteAllText($project, $xml)
    & dotnet build $project -c Release -m:1 -p:BuildInParallel=false -v:q
    if ($LASTEXITCODE -ne 0) { throw "Compile-only Unity $mode source check failed." }
}
Write-Host 'Compile-only checks passed against actual Unity references. This does not confirm engine execution.'
