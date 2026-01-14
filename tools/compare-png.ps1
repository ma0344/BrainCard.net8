[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string] $Old,

    [Parameter(Mandatory = $true, Position = 1)]
    [string] $New,

    [Parameter(Mandatory = $false)]
    [string] $OutDir = "./Dump/Compare",

    [Parameter(Mandatory = $false)]
    [ValidateSet("white", "black", "transparent")]
    [string] $Background = "white",

    [Parameter(Mandatory = $false)]
    [switch] $Open
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Resolve-ExistingPath([string] $p) {
    $resolved = Resolve-Path -LiteralPath $p -ErrorAction SilentlyContinue
    if (-not $resolved) {
        throw "File not found: $p"
    }
    return $resolved.Path
}

function Ensure-Magick {
    $cmd = Get-Command magick -ErrorAction SilentlyContinue
    if (-not $cmd) {
        throw "ImageMagick 'magick' was not found in PATH. Install ImageMagick and reopen the terminal." 
    }
}

function Invoke-Magick([string[]] $Arguments, [string] $stdoutPath = $null, [string] $stderrPath = $null) {
    $prev = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'

        if ($stdoutPath -and $stderrPath) {
            & magick @Arguments 1> $stdoutPath 2> $stderrPath | Out-Null
        }
        elseif ($stderrPath) {
            & magick @Arguments 2> $stderrPath | Out-Null
        }
        elseif ($stdoutPath) {
            & magick @Arguments 1> $stdoutPath | Out-Null
        }
        else {
            & magick @Arguments | Out-Null
        }

        return $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $prev
    }
}

function Magick-Run([string[]] $Arguments) {
    return (Invoke-Magick -Arguments $Arguments)
}

function To-WindowsPath([string] $p) {
    return ($p -replace '/', '\\')
}

function Magick-CompareMetric([string] $metric, [string] $a, [string] $b) {
    $tmp = Join-Path $env:TEMP ("magick-metric-{0}-{1}.txt" -f $metric, ([Guid]::NewGuid().ToString('N')))
    try {
        $aw = To-WindowsPath $a
        $bw = To-WindowsPath $b
        $tw = To-WindowsPath $tmp

        $cmd = 'magick compare -metric {0} "{1}" "{2}" null: 1> nul 2> "{3}"' -f $metric, $aw, $bw, $tw
        cmd.exe /c $cmd | Out-Null

        if (-not (Test-Path -LiteralPath $tmp)) { return '' }
        $lines = Get-Content -LiteralPath $tmp -ErrorAction SilentlyContinue

        # Pick the last line that looks like a metric value.
        $line = $lines | Where-Object { $_ -match '(?i)(inf|[0-9]+(\.[0-9]+)?([eE][+-]?[0-9]+)?)' } | Select-Object -Last 1
        if ($null -eq $line) { return '' }
        return $line.ToString().Trim()
    }
    finally {
        Remove-Item -LiteralPath $tmp -Force -ErrorAction SilentlyContinue
    }
}

Ensure-Magick

$oldPath = To-WindowsPath (Resolve-ExistingPath $Old)
$newPath = To-WindowsPath (Resolve-ExistingPath $New)

$root = $null
if (-not (Test-Path -LiteralPath $OutDir)) {
    New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
}
$root = (Resolve-Path -LiteralPath $OutDir).Path

$baseOld = [IO.Path]::GetFileNameWithoutExtension($oldPath)
$baseNew = [IO.Path]::GetFileNameWithoutExtension($newPath)
$stamp = (Get-Date).ToString('yyyyMMdd-HHmmss')

$oldFlat = Join-Path $root "$baseOld-old-flat-$Background-$stamp.png"
$newFlat = Join-Path $root "$baseNew-new-flat-$Background-$stamp.png"
$diff = Join-Path $root "diff-$stamp.png"
$diffLevel = Join-Path $root "diff-level-$stamp.png"

switch ($Background) {
    'transparent' {
        Copy-Item -LiteralPath $oldPath -Destination $oldFlat -Force
        Copy-Item -LiteralPath $newPath -Destination $newFlat -Force
    }
    default {
        [void](Magick-Run @($oldPath,'-background',$Background,'-alpha','remove','-alpha','off',$oldFlat))
        [void](Magick-Run @($newPath,'-background',$Background,'-alpha','remove','-alpha','off',$newFlat))
    }
}

if (-not (Test-Path -LiteralPath $oldFlat)) { throw "Failed to create: $oldFlat" }
if (-not (Test-Path -LiteralPath $newFlat)) { throw "Failed to create: $newFlat" }

$metrics = [ordered]@{}
$metrics.SSIM = Magick-CompareMetric SSIM $oldFlat $newFlat
$metrics.PSNR = Magick-CompareMetric PSNR $oldFlat $newFlat
$metrics.AE   = Magick-CompareMetric AE   $oldFlat $newFlat

# Diff image
[void](Magick-Run @('compare','-metric','AE',$oldFlat,$newFlat,$diff))
[void](Magick-Run @($diff,'-auto-level',$diffLevel))
 
$report = [ordered]@{
    Old = $oldPath
    New = $newPath
    Background = $Background
    OldFlat = $oldFlat
    NewFlat = $newFlat
    Diff = $diff
    DiffLevel = $diffLevel
    Metrics = $metrics
}

$reportJson = Join-Path $root "report-$stamp.json"
$report | ConvertTo-Json -Depth 4 | Out-File -FilePath $reportJson -Encoding utf8

Write-Host "SSIM: $($metrics.SSIM)" -ForegroundColor Cyan
Write-Host "PSNR: $($metrics.PSNR)" -ForegroundColor Cyan
Write-Host "AE:   $($metrics.AE)" -ForegroundColor Cyan
Write-Host "Diff: $diffLevel" -ForegroundColor Green
Write-Host "Report: $reportJson" -ForegroundColor DarkGray

if ($Open) {
    foreach ($p in @($oldFlat, $newFlat, $diffLevel)) {
        Start-Process -FilePath $p | Out-Null
    }
}
