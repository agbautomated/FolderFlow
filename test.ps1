param([string]$DotNetPath = 'dotnet')
$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'tests\FolderFlow.Tests\FolderFlow.Tests.csproj'
& $DotNetPath restore $project --configfile (Join-Path $PSScriptRoot 'NuGet.Config')
if ($LASTEXITCODE -ne 0) { throw 'Test restore failed.' }
& $DotNetPath run --project $project -c Release --no-restore -- --fixtures (Join-Path $PSScriptRoot 'work\test-fixtures')
if ($LASTEXITCODE -ne 0) { throw 'Tests failed.' }
