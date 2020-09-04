<#
.DESCRIPTION
This script checks that the help output from the program and the message
documented in the Readme coincide.
#>

function Main
{
    $repoRoot = Split-Path -Parent $PSScriptRoot

    $buildDir = Join-Path $repoRoot "artefacts" `
        | Join-Path -ChildPath "build" `
        | Join-Path -ChildPath "Release" `
        | Join-Path -ChildPath "AasxServerCore"

    if (!(Test-Path $buildDir))
    {
        throw ("The build directory does not exist: $buildDir; " +
                "did you `dotnet publish -c Release` to it?")
    }

    $program = Join-Path $buildDir "AasxServerCore"
    if (!(Test-Path $program))
    {
        $program = Join-Path $buildDir "AasxServerCore.exe"

        if (!(Test-Path $program))
        {
            throw ("The program could not be found " +
                    "in the build directory: $buildDir; " +
                    "did you `dotnet publish -c Release` to it?")
        }
    }

    $help = & $program --help |Out-String

    # Trim the help so that the message does not take unnecessary space in the
    # Readme
    $help = $help.Trim()

    # Make help lines valid markdown to make the comparison against Readme
    # a bit more concise
    $helpLines = (
        @("``````") +
        $help.Split(@("`r`n", "`r", "`n"), [StringSplitOptions]::None) +
        @("``````"))

    $readmePath = Join-Path $repoRoot "README.md"
    if (!(Test-Path $readmePath))
    {
        throw "The readme could not be found: $readmePath"
    }
    $readme = Get-Content $readmePath

    $readmeLines = $readme.Split(
            @("`r`n", "`r", "`n"), [StringSplitOptions]::None)

    $startMarker = "<!--- Help starts. -->"
    $endMarker = "<!--- Help ends. -->"

    $startMarkerAt = -1
    $endMarkerAt = -1
    for($i = 0; $i -lt $readmeLines.Length; $i++)
    {
        $trimmed = $readmeLines[$i].Trim()
        if ($trimmed -eq "<!--- Help starts. -->")
        {
            $startMarkerAt = $i
        }

        if ($trimmed -eq "<!--- Help ends. -->")
        {
            $endMarkerAt = $i
        }
    }

    if ($startMarkerAt -eq -1)
    {
        throw "The start marker $startMarker could not be found in: $readmePath"
    }
    if ($endMarkerAt -eq -1)
    {
        throw "The end marker $endMarker could not be found in: $readmePath"
    }

    for($i = $startMarkerAt + 1; $i -lt $endMarkerAt; $i++)
    {
        $helpLineIdx = $i - $startMarkerAt - 1
        $helpLine = $helpLines[$helpLineIdx]
        $readmeLine = $readmeLines[$i]
        if ($helpLine -ne $readmeLine)
        {
            throw (
            "The line $( $i + 1 ) in $readmePath does not " +
                    "coincide with the line $( $helpLineIdx + 1 ) of --help: " +
                    "$( $readmeLine|ConvertTo-Json ) != " +
                    $( $helpLine|ConvertTo-Json ))
        }
    }
    Write-Host "The --help message coincides with the Readme."
}

Main
