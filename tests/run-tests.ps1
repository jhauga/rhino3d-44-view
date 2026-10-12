<#
.SYNOPSIS
    Runs the 44 View test suites.

.DESCRIPTION
    Three suites, in the order they should fail in:

      unit        xUnit over the fit math, no Rhino needed
      smoke       the plugin loads, registers and fits a model
      regression  the four fitted views measured from the live viewports

    The unit suite runs with dotnet test. The other two run inside Rhino, which
    opens a copy of a template and runs a script that writes
    tests/artifacts/rhino-report.json. Rhino's own exit code says nothing about
    the results, so this script reads that report. After a regression run it
    also assembles the before and after pictures from the view captures, when
    ImageMagick (magick) is on the PATH.

.PARAMETER Suite
    all (default), unit, rhino, smoke or regression. "rhino" means smoke and
    regression. Comma separated values are accepted, for example "unit,smoke".

.PARAMETER SkipBuild
    Skips rebuilding the plugin before the in-Rhino suites.

.PARAMETER Template
    The .3dm to start from. Rhino opens a copy of it in tests/artifacts, so the
    template itself is never opened or locked.

.EXAMPLE
    powershell -File tests\run-tests.ps1
    powershell -File tests\run-tests.ps1 -Suite unit
    powershell -File tests\run-tests.ps1 -Suite regression -SkipBuild
#>
[CmdletBinding()]
param(
    [string] $Suite = "all",
    [switch] $SkipBuild,
    [string] $Template = "$env:APPDATA\McNeel\Rhinoceros\8.0\Localization\en-US\Template Files\Small Objects - Millimeters.3dm",
    [string] $RhinoExe = "C:\Program Files\Rhino 8\System\Rhino.exe",
    [int]    $TimeoutSeconds = 300
)

$ErrorActionPreference = "Stop"

$testsDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = Split-Path -Parent $testsDir
$artifacts = Join-Path $testsDir "artifacts"
$report = Join-Path $artifacts "rhino-report.json"
$log = Join-Path $artifacts "rhino-tests.log"
$project = Join-Path $root "src\FourFourView\FourFourView.csproj"
$unitProject = Join-Path $testsDir "FourFourView.Fit.Tests\FourFourView.Fit.Tests.csproj"
$binDir = Join-Path $root "src\FourFourView\bin\Release\net8.0"

$requested = @()
foreach ($name in $Suite.Split(",")) { $requested += $name.Trim().ToLower() }

function Wants([string] $name) {
    if ($requested -contains "all") { return $true }
    if ($requested -contains $name) { return $true }
    if ($requested -contains "rhino" -and ($name -eq "smoke" -or $name -eq "regression")) { return $true }
    return $false
}

# Builds <state>.png: the four view captures in the four-view arrangement, each
# labelled with its view name, under a title strip.
function New-Composite([string] $state, [string] $title) {
    $names = @("Top", "Perspective", "Front", "Right")
    $tiles = @()
    foreach ($name in $names) {
        $source = Join-Path $artifacts "$state-$name.png"
        if (-not (Test-Path $source)) { return $null }
        $tile = Join-Path $artifacts "$state-$name-tile.png"
        & magick $source -gravity northwest -font Arial -pointsize 18 -fill "#24324a" -undercolor "#ffffffd9" `
            -annotate +10+8 " $name " -bordercolor "#d5dbe4" -border 3 $tile
        $tiles += $tile
    }
    $upper = Join-Path $artifacts "$state-upper-row.png"
    $lower = Join-Path $artifacts "$state-lower-row.png"
    & magick $tiles[0] $tiles[1] +append +repage $upper
    & magick $tiles[2] $tiles[3] +append +repage $lower
    $output = Join-Path $artifacts "$state.png"
    & magick $upper $lower -append +repage -gravity north -background "#24324a" -splice 0x56 `
        -font Arial -pointsize 30 -fill "#ffffff" -annotate +0+12 $title $output
    $built = ($LASTEXITCODE -eq 0) -and (Test-Path $output)
    Remove-Item ($tiles + @($upper, $lower)) -Force -ErrorAction SilentlyContinue
    if ($built) { return $output }
    return $null
}

if (-not (Test-Path $artifacts)) { New-Item -ItemType Directory -Path $artifacts | Out-Null }

$unitOk = $true
$rhinoOk = $true
$ranAnything = $false

# ---- unit -------------------------------------------------------------------

if (Wants "unit") {
    $ranAnything = $true
    Write-Host ""
    Write-Host "== unit suite (dotnet test) ==" -ForegroundColor Cyan
    dotnet test $unitProject --nologo -v q
    if ($LASTEXITCODE -ne 0) { $unitOk = $false }
}

# ---- in-Rhino suites --------------------------------------------------------

$rhinoSuites = @()
foreach ($name in @("smoke", "regression")) {
    if (Wants $name) { $rhinoSuites += $name }
}

if ($rhinoSuites.Count -gt 0) {
    $ranAnything = $true

    if (-not (Test-Path $RhinoExe)) {
        Write-Host "Rhino was not found at $RhinoExe" -ForegroundColor Red
        exit 1
    }
    if (-not (Test-Path $Template)) {
        Write-Host "Template document not found: $Template" -ForegroundColor Red
        Write-Host "Pass -Template with the path of any Rhino template." -ForegroundColor Red
        exit 1
    }

    if (-not $SkipBuild) {
        Write-Host ""
        Write-Host "== building the plugin (Release) ==" -ForegroundColor Cyan
        dotnet build $project -c Release --nologo -v q
        if ($LASTEXITCODE -ne 0) {
            Write-Host "The plugin did not build; the in-Rhino suites cannot run." -ForegroundColor Red
            exit 1
        }
    }

    Write-Host ""
    Write-Host ("== in-Rhino suites: " + ($rhinoSuites -join ", ") + " ==") -ForegroundColor Cyan

    # Results from an earlier run must not be mistaken for this one's.
    Get-ChildItem -Path $artifacts -File |
        Where-Object { @(".png", ".json", ".log", ".3dm", ".3dmbak", ".rhl") -contains $_.Extension } |
        Remove-Item -Force -ErrorAction SilentlyContinue

    $model = Join-Path $artifacts "test-model.3dm"
    Copy-Item -LiteralPath $Template -Destination $model -Force

    # RHINO_PACKAGE_DIRS makes Rhino load the freshly built plugin with no install step.
    $env:RHINO_PACKAGE_DIRS = $binDir + "\"
    $env:FFV_TESTS_DIR = $testsDir
    $env:FFV_RHP = Join-Path $binDir "FourFourView.rhp"
    $env:FFV_SUITES = ($rhinoSuites -join ",")

    $entry = Join-Path $testsDir "rhino\run_in_rhino.py"
    $rhinoArgs = "`"$model`" /nosplash /netcore /runscript=`"-_RunPythonScript $entry`""
    $process = Start-Process -FilePath $RhinoExe -ArgumentList $rhinoArgs -PassThru
    Write-Host ("Rhino started (pid " + $process.Id + "); waiting up to $TimeoutSeconds s")

    if (-not $process.WaitForExit($TimeoutSeconds * 1000)) {
        Write-Host "Rhino did not close on its own; stopping it." -ForegroundColor Yellow
        Stop-Process -Id $process.Id -Force -Confirm:$false
    }

    if (Test-Path $log) { Get-Content $log }

    if (-not (Test-Path $report)) {
        Write-Host ""
        Write-Host "No report was written. Rhino never reached the end of the suites." -ForegroundColor Red
        $rhinoOk = $false
    }
    else {
        $data = Get-Content $report -Raw | ConvertFrom-Json
        Write-Host ""
        Write-Host ("in-Rhino: " + $data.counts.passed + " passed, " +
                    $data.counts.failed + " failed, " + $data.counts.error + " errored")
        if (-not $data.ok) { $rhinoOk = $false }
    }

    if ($rhinoSuites -contains "regression") {
        if (Get-Command magick -ErrorAction SilentlyContinue) {
            foreach ($pair in @(@("before", "4View"), @("after", "44View"))) {
                $picture = New-Composite $pair[0] $pair[1]
                if ($picture) { Write-Host ("picture: tests/artifacts/" + (Split-Path -Leaf $picture)) }
                else { Write-Host ("No " + $pair[0] + " picture: its view captures are missing.") -ForegroundColor Yellow }
            }
        }
        else {
            Write-Host "magick is not on the PATH, so the before and after pictures were not assembled." -ForegroundColor Yellow
        }
    }
}

# ---- summary ----------------------------------------------------------------

Write-Host ""
if (-not $ranAnything) {
    Write-Host "No suite matched -Suite '$Suite'. Use all, unit, rhino, smoke or regression." -ForegroundColor Yellow
    exit 1
}
if ($unitOk -and $rhinoOk) {
    Write-Host "All requested suites passed." -ForegroundColor Green
    exit 0
}
Write-Host "Some suites failed." -ForegroundColor Red
exit 1
