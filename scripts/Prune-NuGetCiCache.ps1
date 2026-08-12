<#
.SYNOPSIS
  Shrink the NuGet global-packages folder for linux CI runners.

.DESCRIPTION
  Most of the CI cache is RavenDB.Embedded (multi-RID server payload + .nupkg).
  Deleting arbitrary package **/runtimes/** folders breaks GenerateDepsFile (assets
  still reference those files). Only strip:

  - *.nupkg copies (extracted content + sha512 remain)
  - RavenDB.Embedded contentFiles RID trees other than linux-x64
  - obvious non-linux pal/zstd binaries beside the Embedded server

  Safe to run repeatedly. Refuses the user-global ~/.nuget/packages unless -Force.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$PackagesPath,

    [string[]]$KeepRuntimes = @('linux-x64', 'linux', 'unix'),

    [switch]$Force
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $PackagesPath)) {
    Write-Host "NuGet packages path not found; skip prune: $PackagesPath"
    return
}

$resolvedPackages = (Resolve-Path -LiteralPath $PackagesPath).Path
$userCache = Join-Path $HOME '.nuget/packages'
if ((Test-Path -LiteralPath $userCache) -and -not $Force) {
    $resolvedUser = (Resolve-Path -LiteralPath $userCache).Path
    if ($resolvedPackages -eq $resolvedUser) {
        throw "Refusing to prune the user NuGet cache at $resolvedUser. Use a CI packages path or pass -Force."
    }
}

function Get-DirectorySizeBytes {
    param([string]$Path)
    $sum = 0L
    Get-ChildItem -LiteralPath $Path -Recurse -File -Force -ErrorAction SilentlyContinue |
        ForEach-Object { $sum += $_.Length }
    return $sum
}

function Format-MiB {
    param([long]$Bytes)
    return '{0:N1} MiB' -f ($Bytes / 1MB)
}

$before = Get-DirectorySizeBytes -Path $PackagesPath
Write-Host "Pruning NuGet cache at $PackagesPath (before $(Format-MiB $before))"

$keep = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($rid in $KeepRuntimes) {
    [void]$keep.Add($rid)
}

$removedNupkg = 0
$removedNupkgBytes = 0L
Get-ChildItem -LiteralPath $PackagesPath -Recurse -File -Filter '*.nupkg' -Force -ErrorAction SilentlyContinue |
    ForEach-Object {
        $removedNupkgBytes += $_.Length
        Remove-Item -LiteralPath $_.FullName -Force
        $removedNupkg++
    }

# Only Embedded's contentFiles RID trees — not package runtime assets used by deps.json.
$removedRidDirs = 0
$removedRidBytes = 0L
$embeddedRuntimes = Join-Path $PackagesPath 'ravendb.embedded'
if (Test-Path -LiteralPath $embeddedRuntimes) {
    Get-ChildItem -LiteralPath $embeddedRuntimes -Recurse -Directory -Force -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -eq 'runtimes' -and $_.FullName -match '[/\\]contentFiles[/\\]' } |
        ForEach-Object {
            Get-ChildItem -LiteralPath $_.FullName -Directory -Force -ErrorAction SilentlyContinue |
                Where-Object { -not $keep.Contains($_.Name) } |
                ForEach-Object {
                    $removedRidBytes += Get-DirectorySizeBytes -Path $_.FullName
                    Remove-Item -LiteralPath $_.FullName -Recurse -Force
                    $removedRidDirs++
                }
        }
}

$removedPal = 0
$removedPalBytes = 0L
if (Test-Path -LiteralPath $embeddedRuntimes) {
    Get-ChildItem -LiteralPath $embeddedRuntimes -Recurse -File -Force -ErrorAction SilentlyContinue |
        Where-Object {
            $_.FullName -match '[/\\]contentFiles[/\\]' -and (
                $_.Name -match '(?i)(\.dylib$|\.win\.|\.mac\.|win7?\.|maccatalyst)' -or
                ($_.Extension -eq '.dll' -and $_.Name -match '(?i)(win|rvnpal\.win|zstd\.win|DasMulli\.Win32)')
            )
        } |
        ForEach-Object {
            $removedPalBytes += $_.Length
            Remove-Item -LiteralPath $_.FullName -Force
            $removedPal++
        }
}

$after = Get-DirectorySizeBytes -Path $PackagesPath
$saved = $before - $after

Write-Host ("Removed {0} .nupkg ({1}), {2} Embedded RID folders ({3}), {4} non-linux Raven binaries ({5})" -f `
    $removedNupkg, (Format-MiB $removedNupkgBytes), `
    $removedRidDirs, (Format-MiB $removedRidBytes), `
    $removedPal, (Format-MiB $removedPalBytes))
Write-Host ("NuGet cache after prune: {0} (saved {1})" -f (Format-MiB $after), (Format-MiB $saved))
