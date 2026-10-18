param([string]$DotNetPath = 'dotnet', [ValidateSet('win-x64','win-arm64')][string]$Runtime = 'win-x64')
$ErrorActionPreference = 'Stop'
& $DotNetPath publish (Join-Path $PSScriptRoot 'src\FolderFlow.App\FolderFlow.App.csproj') -c Release -r $Runtime --self-contained true -p:PublishSingleFile=false -o (Join-Path $PSScriptRoot 'portable') --configfile (Join-Path $PSScriptRoot 'NuGet.Config')
if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }
Write-Host 'Ready: portable\FolderFlow.exe. Keep all files in that folder together.'
