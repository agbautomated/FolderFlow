[CmdletBinding()]
param()

# Code-native version of FolderFlow's existing folder-and-arrow icon.
# Run on Windows; uses only the framework's System.Drawing assembly.
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$assetDirectory = Join-Path $PSScriptRoot 'Assets'
$null = New-Item -ItemType Directory -Path $assetDirectory -Force

function Write-Asset([string]$Name, [int]$Width, [int]$Height) {
    $scale = 4
    $bitmap = [Drawing.Bitmap]::new(($Width * $scale), ($Height * $scale))
    $graphics = [Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.Clear([Drawing.Color]::FromArgb(0, 133, 138))
    $side = [Math]::Min($Width, $Height) * $scale
    $left = (($Width * $scale) - $side) / 2
    $top = (($Height * $scale) - $side) / 2
    $graphics.TranslateTransform([single]$left, [single]$top)
    $graphics.ScaleTransform([single]($side / 256.0), [single]($side / 256.0))
    $whiteBrush = New-Object Drawing.SolidBrush([Drawing.Color]::White)
    $tealBrush = New-Object Drawing.SolidBrush([Drawing.Color]::FromArgb(0, 133, 138))
    $folderPoints = [Drawing.PointF[]]@(
        (New-Object Drawing.PointF(48, 70)),
        (New-Object Drawing.PointF(108, 70)),
        (New-Object Drawing.PointF(108, 88)),
        (New-Object Drawing.PointF(208, 88)),
        (New-Object Drawing.PointF(208, 188)),
        (New-Object Drawing.PointF(48, 188))
    )
    $graphics.FillPolygon($whiteBrush, $folderPoints)
    $arrowPoints = [Drawing.PointF[]]@(
        (New-Object Drawing.PointF(80, 122)),
        (New-Object Drawing.PointF(155, 122)),
        (New-Object Drawing.PointF(155, 108)),
        (New-Object Drawing.PointF(184, 137)),
        (New-Object Drawing.PointF(155, 166)),
        (New-Object Drawing.PointF(155, 151)),
        (New-Object Drawing.PointF(80, 151))
    )
    $graphics.FillPolygon($tealBrush, $arrowPoints)
    $whiteBrush.Dispose()
    $tealBrush.Dispose()
    $graphics.Dispose()
    $final = [Drawing.Bitmap]::new($Width, $Height)
    $resizer = [Drawing.Graphics]::FromImage($final)
    $resizer.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $resizer.DrawImage($bitmap, 0, 0, $Width, $Height)
    $final.Save((Join-Path $assetDirectory $Name), [Drawing.Imaging.ImageFormat]::Png)
    $resizer.Dispose()
    $final.Dispose()
    $bitmap.Dispose()
}

Write-Asset 'StoreLogo.png' 50 50
Write-Asset 'Square44x44Logo.png' 44 44
Write-Asset 'Square150x150Logo.png' 150 150
Write-Asset 'Wide310x150Logo.png' 310 150
Write-Asset 'Square310x310Logo.png' 310 310
Write-Host 'Generated five package tile assets. These are package assets, not Store listing screenshots.'
