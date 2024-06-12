#!/usr/bin/env pwsh

<#
.SYNOPSIS
This script builds all the docker images.
#>

$ErrorActionPreference = "Stop"

function Main
{
    if ($null -eq (Get-Command "docker" -ErrorAction SilentlyContinue))
    {
        throw "Unable to find docker in your PATH"
    }

    Set-Location (Split-Path -Parent $PSScriptRoot)

    ##
    # AasxServerBlazor
    ##

    $imageTag = "aasx-server-blazor-for-demo"
    Write-Host "Building the docker image: $imageTag"
    docker build `
        -t $imageTag `
        -f src/docker/Dockerfile-AasxServerBlazor `
        .

    Write-Host "The image $imageTag has been built."

    ##
    # AasxServerCore
    ##

    $imageTag = "aasx-server-core-for-demo"
    Write-Host "Building the docker image: $imageTag"
    docker build `
        -t $imageTag `
        -f src/docker/Dockerfile-AasxServerCore `
        .

    Write-Host "The image $imageTag has been built."
}

$previousLocation = Get-Location
try
{
    Main
}
finally
{
    Set-Location $previousLocation
}
