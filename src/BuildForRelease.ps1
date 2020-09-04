param(
    [Parameter(HelpMessage = "If set, cleans up the previous build instead of performing a new one")]
    [switch]
    $clean = $false
)

<#
.DESCRIPTION
This script builds the solution for debugging (manual or automatic testing).
#>

$ErrorActionPreference = "Stop"

Import-Module (Join-Path $PSScriptRoot Common.psm1) -Function `
    AssertDotnet, `
    GetArtefactsDir

<#
.SYNOPSIS
Search for MSBuild in the path and at expected locations using `vswhere.exe`.
#>
function FindMSBuild
{
    $msbuild = $null

    $msbuildCommand = Get-Command "MSBuild.exe" -ErrorAction SilentlyContinue
    $msbuildFailedSearches = @()
    if ($null -ne $msbuildCommand)
    {
        $msbuild = $msbuildCommand.Source
    }
    else
    {
        $vswherePath = "${Env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
        if (!(Test-Path $vswherePath))
        {
            throw "Could not find vswhere at: $vswherePath"
        }

        $ids = 'Community', 'Professional', 'Enterprise', 'BuildTools' `
            | ForEach-Object { 'Microsoft.VisualStudio.Product.' + $_ }

        $instance = & $vswherePath -latest -products $ids -requires Microsoft.Component.MSBuild -format json `
            | Convertfrom-Json `
            | Select-Object -first 1

        $msbuildPath = Join-Path $instance.installationPath 'MSBuild\15.0\Bin\MSBuild.exe'
        if (Test-Path $msbuildPath)
        {
            $msbuild = $msbuildPath
        }
        else
        {
            $msbuildFailedSearches += $msbuildPath

            $msbuildPath = Join-Path $instance.installationPath 'MSBuild\Current\Bin\MSBuild.exe'
            if (Test-Path $msbuildPath)
            {
                $msbuild = $msbuildPath
            }
            else
            {
                $msbuildFailedSearches += $msbuildPath
            }
        }
    }

    if (!$msbuild)
    {
        throw "Could not find MSBuild in PATH and at these locations: $( $msbuildFailedSearches -join ';' )"
    }

    return $msbuild
}

function CopyContentForDemo($Destination)
{
    $contentForDemoDir = Join-Path (Split-Path -Parent $PSScriptRoot) "content-for-demo"
    if (!(Test-Path $contentForDemoDir))
    {
       throw "The directory with the content for demo does not exist: $contentForDemoDir"
    }

    Write-Host "Copying content for demo from $contentForDemoDir to: $Destination"

    Get-ChildItem -Path $contentForDemoDir `
        | Copy-Item -Destination $Destination -Recurse -Container -Force
}

function Main
{
    Set-Location $PSScriptRoot

    $baseBuildDir = Join-Path $( GetArtefactsDir ) "build" `
        | Join-Path -ChildPath "Release"

    if ($clean)
    {
        Write-Host "dotnet clean'ing ..."
        dotnet clean
        if ($LASTEXITCODE -ne 0)
        {
            throw "Failed to dotnet clean."
        }

        Write-Host "Removing the build directory: $baseBuildDir"
        Remove-Item -LiteralPath $baseBuildDir -Force -Recurse
    }
    else
    {
        AssertDotnet

        ##
        # Build dotnet targets
        ##

        $dotnetTargets = $(
        "AasxServerBlazor"
        "AasxServerCore"
        )

        foreach ($target in $dotnetTargets)
        {
            $buildDir = Join-Path $baseBuildDir $target

            Write-Host "Publishing with dotnet $target to: $buildDir"

            dotnet publish -c Release -o $buildDir $target
            if ($LASTEXITCODE -ne 0)
            {
                throw "Failed to dotnet publish: $target"
            }

            CopyContentForDemo -Destination $buildDir
        }

        ##
        # Build AasxServerWindows with MSBuild
        #
        # This is necessary as AasxServerWindows depends on
        # .NET Framework 4.7.2.
        ##

        if ($null -eq (Get-Command "nuget.exe" -ErrorAction SilentlyContinue))
        {
           throw "Unable to find nuget.exe in your PATH"
        }

        $msbuild = FindMSBuild

        $target = "AasxServerWindows"

        Write-Host "Restoring NuGet dependencies for $target ..."
        nuget.exe restore $target -PackagesDirectory packages

        $buildDir = Join-Path $baseBuildDir $target
        Write-Host "Building with MSBuild $target to: $buildDir"
        & $msbuild `
            "/p:OutputPath=$buildDir" `
            "/p:Configuration=Release" `
            "/p:Platform=x64" `
            /maxcpucount `
            $target `
            /t:build

        if ($LASTEXITCODE -ne 0)
        {
            throw "Failed to MSBuild $target."
        }

        CopyContentForDemo -Destination $buildDir
    }
}

$previousLocation = Get-Location; try
{
    Main
}
finally
{
    Set-Location $previousLocation
}
