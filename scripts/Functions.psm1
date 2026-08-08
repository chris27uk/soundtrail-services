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
# blocks of powershell.
function Invoke-Stage(
    [string]$Name,
    [ScriptBlock]$Action)
{
    Write-Title $Name
    Invoke-Command -ScriptBlock $Action
}

Export-ModuleMember -Function Write-Title, Invoke-InPath, Exec, Invoke-Stage
