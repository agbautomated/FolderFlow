[CmdletBinding()]
param(
    [switch]$Store,
    [string]$IdentityConfig,
    [string]$DotNetPath = 'dotnet',
    [string]$MakeAppxPath,
    [string]$OutputDirectory
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Find-MakeAppx {
    if ($MakeAppxPath) {
        if (-not (Test-Path -LiteralPath $MakeAppxPath -PathType Leaf)) { throw "MakeAppx.exe not found: $MakeAppxPath" }
        return (Resolve-Path -LiteralPath $MakeAppxPath).Path
    }
    $tool = Get-Command MakeAppx.exe -ErrorAction SilentlyContinue
    if ($tool) { return $tool.Source }
    $sdkBin = Join-Path ${env:ProgramFiles(x86)} 'Windows Kits\10\bin'
    if (Test-Path -LiteralPath $sdkBin) {
        $found = Get-ChildItem -LiteralPath $sdkBin -Directory | Sort-Object Name -Descending |
            ForEach-Object { Join-Path $_.FullName 'x64\MakeAppx.exe' } |
            Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } | Select-Object -First 1
        if ($found) { return $found }
    }
    throw 'Install the Windows SDK MSIX Packaging Tools, or supply -MakeAppxPath pointing to MakeAppx.exe (Microsoft.Windows.SDK.BuildTools also contains it).'
}

function Read-PackageVersion([string]$Value, [string]$Label) {
    if ($Value -notmatch '^\d+\.\d+\.\d+\.\d+$') { throw "$Label must have four numeric components." }
    $parsed = [version]::Parse($Value)
    foreach ($part in @($parsed.Major, $parsed.Minor, $parsed.Build, $parsed.Revision)) {
        if ($part -gt 65535) { throw "$Label components cannot exceed 65535." }
    }
    return $parsed
}

if ($Store -and -not $IdentityConfig) { throw '-Store requires -IdentityConfig with the exact identity assigned by Microsoft Partner Center.' }
if (-not $Store -and $IdentityConfig) { throw 'Custom identity configs require -Store. Omit -IdentityConfig for an isolated development package.' }

if ($Store) {
    $identity = Get-Content -LiteralPath $IdentityConfig -Raw | ConvertFrom-Json
    $required = @('name', 'publisher', 'publisherDisplayName', 'displayName', 'version', 'architecture', 'minOSVersion')
    foreach ($key in $required) {
        if (-not ($identity.PSObject.Properties.Name -contains $key) -or [string]::IsNullOrWhiteSpace([string]$identity.$key)) {
            throw "Identity config is missing $key. Copy packaging\identity.config.example.json and fill in the Partner Center values."
        }
        if ($identity.$key -isnot [string]) { throw "Identity field $key must be a JSON string." }
        if ($identity.$key -match '(?i)(REPLACE|PLACEHOLDER|YOUR[ ._-]|EXAMPLE|FolderFlow\.Development|FolderFlowDevelopment|[{}<>])') {
            throw "Identity field $key contains an example or development value. Copy the exact value from Partner Center."
        }
        if ($identity.$key.Trim() -cne $identity.$key) { throw "Identity field $key contains leading or trailing whitespace." }
    }
    foreach ($key in $identity.PSObject.Properties.Name) {
        if ($key -notin ($required + @('$schema'))) { throw "Unknown identity field: $key" }
    }
} else {
    $identity = [pscustomobject]@{
        name = 'FolderFlow.Development'
        publisher = 'CN=FolderFlowDevelopment, OID.2.25.311729368913984317654407730594956997722=1'
        publisherDisplayName = 'FolderFlow Development (test only)'
        displayName = 'FolderFlow Development'
        version = '1.0.0.0'
        architecture = 'x64'
        minOSVersion = '10.0.22000.0'
    }
}

if ($identity.name -notmatch '^[A-Za-z0-9.-]{3,50}$') { throw 'Package identity name must be 3-50 letters, digits, dots or hyphens.' }
if ($identity.publisher.Length -gt 8192 -or $identity.publisher -notmatch '^CN=') { throw 'Publisher must be the exact Partner Center distinguished name beginning CN=.' }
if ($Store -and $identity.publisher -match '2\.25\.311729368913984317654407730594956997722') { throw 'The unsigned-development publisher OID is forbidden in Store packages.' }
try { $null = New-Object System.Security.Cryptography.X509Certificates.X500DistinguishedName($identity.publisher) }
catch { throw 'Publisher is not a valid X.500 distinguished name.' }
foreach ($key in @('publisherDisplayName', 'displayName')) {
    if ($identity.$key.Length -gt 256) { throw "$key cannot exceed 256 characters." }
}
if ($identity.architecture -notin @('x64', 'arm64')) { throw 'Architecture must be x64 or arm64.' }
$packageVersion = Read-PackageVersion $identity.version 'Package version'
if ($packageVersion.Major -lt 1 -or $packageVersion.Revision -ne 0) { throw 'Store version must begin above zero and have a fourth component of 0 (for example 1.0.0.0).' }
$minimumVersion = Read-PackageVersion $identity.minOSVersion 'Minimum OS version'
if ($minimumVersion -lt [version]'10.0.22000.0' -or $minimumVersion -gt [version]'10.0.26100.0') {
    throw 'Minimum OS must be between 10.0.22000.0 and the manifest MaxVersionTested 10.0.26100.0.'
}

$makeAppx = Find-MakeAppx
$dotnet = Get-Command $DotNetPath -ErrorAction Stop
$runId = (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [guid]::NewGuid().ToString('N').Substring(0, 8)
$runDirectory = Join-Path $PSScriptRoot "packaging\work\$runId"
$staging = Join-Path $runDirectory 'staging'
$null = New-Item -ItemType Directory -Path $staging
if (-not $OutputDirectory) { $OutputDirectory = Join-Path $runDirectory 'artifacts' }
$null = New-Item -ItemType Directory -Path $OutputDirectory -Force
$OutputDirectory = (Resolve-Path -LiteralPath $OutputDirectory).Path
$stagingFull = [IO.Path]::GetFullPath($staging).TrimEnd('\') + '\'
if (($OutputDirectory.TrimEnd('\') + '\').StartsWith($stagingFull, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'OutputDirectory must be outside the package staging directory.'
}
$packagePath = Join-Path $OutputDirectory ("{0}_{1}_{2}_{3}.msix" -f $identity.name, $identity.version, $identity.architecture, $runId)
$project = Join-Path $PSScriptRoot 'src\FolderFlow.App\FolderFlow.App.csproj'
$runtime = 'win-' + $identity.architecture
Write-Host "Publishing self-contained $runtime desktop app..."
& $dotnet.Source publish $project -c Release -r $runtime --self-contained true -p:PublishSingleFile=false -p:Version="$($packageVersion.Major).$($packageVersion.Minor).$($packageVersion.Build)" -o $staging --configfile (Join-Path $PSScriptRoot 'NuGet.Config')
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed; no package was produced.' }
if (-not (Test-Path -LiteralPath (Join-Path $staging 'FolderFlow.exe') -PathType Leaf)) { throw 'Published FolderFlow.exe is missing.' }
& (Join-Path $PSScriptRoot 'scripts\Copy-RuntimeNotices.ps1') -PublishDirectory $staging -DotNetPath $dotnet.Source

$template = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'packaging\AppxManifest.template.xml') -Raw
foreach ($property in $identity.PSObject.Properties) {
    $template = $template.Replace('{{' + $property.Name + '}}', [System.Security.SecurityElement]::Escape([string]$property.Value))
}
if ($template -match '\{\{[^}]+\}\}') { throw 'Unresolved manifest template field.' }
$manifestPath = Join-Path $staging 'AppxManifest.xml'
$xml = New-Object System.Xml.XmlDocument
$xml.LoadXml($template)
$xml.Save($manifestPath)
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'packaging\Assets') -Destination (Join-Path $staging 'Assets') -Recurse
Write-Host 'Building and validating the unsigned MSIX package...'
$makeAppxLog = Join-Path $runDirectory 'makeappx.log'
& $makeAppx pack /d $staging /p $packagePath /h SHA256 *> $makeAppxLog
if ($LASTEXITCODE -ne 0) {
    Get-Content -LiteralPath $makeAppxLog -Tail 30 | Write-Host
    throw "MakeAppx validation or packing failed. Full log: $makeAppxLog"
}
if (-not (Test-Path -LiteralPath $packagePath -PathType Leaf)) { throw 'MakeAppx returned without creating the expected package.' }
$hash = (Get-FileHash -LiteralPath $packagePath -Algorithm SHA256).Hash
$hash | Set-Content -LiteralPath ($packagePath + '.sha256') -Encoding ascii
Write-Host "Package: $packagePath"
Write-Host "SHA256:  $hash"
Write-Host "Packaging log: $makeAppxLog"
if ($Store) {
    Write-Host 'Unsigned Store candidate: upload to the matching Partner Center product only after packaged-app testing and certification checks.'
    Write-Host 'Microsoft signs accepted Store packages. No local certificate or certificate trust was created.'
} else {
    Write-Warning 'DEVELOPMENT IDENTITY ONLY. Do not submit this package to Microsoft Store.'
    Write-Host 'This package includes the Windows 11 unsigned-test publisher OID. A Windows 11 test VM can use Add-AppxPackage -AllowUnsigned from an elevated PowerShell; executable content affects all users. Do not use this identity for distribution.'
    Write-Host "Staged manifest: $manifestPath"
    Write-Host 'No certificate was created or trusted, no package was installed, and Developer Mode was not changed.'
}
Write-Output $packagePath
