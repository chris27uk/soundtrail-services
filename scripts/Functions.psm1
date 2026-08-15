<#
.DESCRIPTION
    Common functions used in scripts.
#>

# Writes a coloured title for use in logs
function Write-Title($Title) {
  if ($null -ne $PSStyle) {
    Write-Host ""
    Write-Host "$($PSStyle.Foreground.Green)------------------------------------------------------$($PSStyle.Reset)"
    Write-Host "$($PSStyle.Foreground.Green)$Title$($PSStyle.Reset)"
    Write-Host "$($PSStyle.Foreground.Green)------------------------------------------------------$($PSStyle.Reset)"
  } else {
    Write-Host "`n------------------------------------------------------"
    Write-Host $Title
    Write-Host "------------------------------------------------------"
  }
}

# Switches the current working directory and runs the script block in that
# context. The working directory is restored after the operation has been
# executed.
function Invoke-InPath([string]$Path, [ScriptBlock]$Action) {
    Try {
        Push-Location $Path
        Write-Host "New location is $(Get-Location)"
        & $Action
    }
    Catch {
        Write-Host $_
        Throw;
    }
    Finally {
        Pop-Location
    }
}

# Runs an external command and throws an exception if the runner fails.
function Exec {
    param(
        [Parameter(Position=0, Mandatory=$true)]
        [string]$exe,
        [Parameter(Position=1, Mandatory=$false)]
        [string[]]$arguments = @()
    )

    Write-Host "> $exe $($arguments -join ' ')" -ForegroundColor Cyan

    # Capture both stdout and stderr
    $output = & $exe $arguments 2>&1
    $exitCode = $LASTEXITCODE

    # Display all output
    $output | ForEach-Object {
        if ($_ -is [System.Management.Automation.ErrorRecord]) {
            Write-Host $_.Exception.Message -ForegroundColor Magenta
        } else {
            Write-Host $_
        }
    }

    if ($exitCode -ne 0) {
        throw "Command failed with exit code ${exitCode}: $exe $($arguments -join ' ')"
    }
}

# Writes a title and invokes a script function - used for readability of named
# blocks of powershell. Prints stage elapsed time so CI logs separate restore/build/test cost.
function Invoke-Stage(
    [string]$Name,
    [ScriptBlock]$Action)
{
    Write-Title $Name
    $stageWatch = [System.Diagnostics.StopWatch]::StartNew()
    try {
        Invoke-Command -ScriptBlock $Action
    }
    finally {
        $stageWatch.Stop()
        Write-Host ("Stage '{0}' completed in {1}" -f $Name, $stageWatch.Elapsed.ToString()) -ForegroundColor DarkGray
    }
}

# Prints a concise TRX summary so CI logs still show totals after noisy host output.
function Write-TestRunSummary {
    param(
        [Parameter(Mandatory = $true)]
        [string]$TrxPath
    )

    if (-not (Test-Path -LiteralPath $TrxPath)) {
        Write-Host "Test summary: TRX not found at $TrxPath" -ForegroundColor Yellow
        return
    }

    try {
        [xml]$trx = Get-Content -LiteralPath $TrxPath -Raw
        $ns = New-Object System.Xml.XmlNamespaceManager($trx.NameTable)
        $ns.AddNamespace("t", "http://microsoft.com/schemas/VisualStudio/TeamTest/2010")
        $counters = $trx.SelectSingleNode("//t:ResultSummary/t:Counters", $ns)
        if ($null -eq $counters) {
            $counters = $trx.SelectSingleNode("//ResultSummary/Counters")
        }
        if ($null -eq $counters) {
            Write-Host "Test summary: unable to parse counters from $TrxPath" -ForegroundColor Yellow
            return
        }

        $total = [int]$counters.GetAttribute("total")
        $passed = [int]$counters.GetAttribute("passed")
        $failed = [int]$counters.GetAttribute("failed")
        $skipped = [int]($counters.GetAttribute("notExecuted"))
        if ($counters.HasAttribute("total") -eq $false) {
            $total = $passed + $failed + $skipped
        }

        $color = if ($failed -gt 0) { "Red" } else { "Green" }
        Write-Host ""
        Write-Host ("Test summary: {0} total, {1} passed, {2} failed, {3} skipped ({4})" -f `
            $total, $passed, $failed, $skipped, $TrxPath) -ForegroundColor $color
    }
    catch {
        Write-Host "Test summary: failed to read $TrxPath ($_)" -ForegroundColor Yellow
    }
}

Export-ModuleMember -Function Write-Title, Invoke-InPath, Exec, Invoke-Stage, Write-TestRunSummary
