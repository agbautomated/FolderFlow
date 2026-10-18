[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$PublishDirectory,
    [string]$DotNetPath = 'dotnet'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$runtimeConfig = Get-Content -LiteralPath (Join-Path $PublishDirectory 'FolderFlow.runtimeconfig.json') -Raw | ConvertFrom-Json
$dependencies = Get-Content -LiteralPath (Join-Path $PublishDirectory 'FolderFlow.deps.json') -Raw | ConvertFrom-Json
$rid = ($dependencies.runtimeTarget.name -split '/')[-1]
if ($rid -notmatch '^win-(x64|arm64)$') { throw 'Runtime notices require a self-contained Windows x64 or arm64 publish.' }
$cache = $env:NUGET_PACKAGES
if ([string]::IsNullOrWhiteSpace($cache)) {
    $location = & $DotNetPath nuget locals global-packages --list --force-english-output
    if ($LASTEXITCODE -ne 0) { throw 'Could not locate the restored .NET runtime notices.' }
    $match = [regex]::Match(($location -join "`n"), '(?m)^global-packages:\s*(.+)$')
    if (-not $match.Success) { throw 'Could not identify the NuGet package cache.' }
    $cache = $match.Groups[1].Value.Trim()
}
$noticesDirectory = Join-Path $PublishDirectory 'Licenses'
$null = New-Item -ItemType Directory -Force -Path $noticesDirectory
$versions = foreach ($framework in $runtimeConfig.runtimeOptions.includedFrameworks) {
    # Preserve notices from the exact runtime patch selected during restore.
    $packageName = $framework.name.ToLowerInvariant() + '.runtime.' + $rid
    $packageDirectory = Join-Path (Join-Path $cache $packageName) $framework.version
    $files = @(Get-ChildItem -LiteralPath $packageDirectory -File | Where-Object {
        $_.Name -match '^(LICENSE|THIRD-PARTY-NOTICES)(\.TXT)?$'
    })
    if (-not ($files | Where-Object { $_.Name -match '^LICENSE(\.TXT)?$' })) {
        throw "Runtime license is missing from $packageName $($framework.version). Restore the runtime pack before packaging."
    }
    foreach ($file in $files) {
        Copy-Item -LiteralPath $file.FullName -Destination (Join-Path $noticesDirectory ($framework.name + '-' + $file.Name))
    }
    "$($framework.name) $($framework.version) ($rid)"
}
$versions | Set-Content -LiteralPath (Join-Path $noticesDirectory 'RUNTIME-VERSIONS.txt') -Encoding utf8
