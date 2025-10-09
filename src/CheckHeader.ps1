$header = @"
/********************************************************************************
* Copyright (c) {2019 - 2025} Contributors to the Eclipse Foundation
*
* See the NOTICE file(s) distributed with this work for additional
* information regarding copyright ownership.
*
* This program and the accompanying materials are made available under the
* terms of the Apache License Version 2.0 which is available at
* https://www.apache.org/licenses/LICENSE-2.0
*
* SPDX-License-Identifier: Apache-2.0
********************************************************************************/
"@
$headerPattern = 'Copyright \(c\) \{2019 - (2024|2025)\} Contributors to the Eclipse Foundation'
$excludedDirs = @("Debug", "obj", "Migrations")
$excludedFiles = @("ModbusTCPClient.cs")
$modifiedFiles = @()
Get-ChildItem -Recurse -Filter *.cs | Where-Object {
    $path = $_.FullName
    $isExcluded = $false
    foreach ($dir in $excludedDirs) {
        if ($path -split '\\' -contains $dir) {
            $isExcluded = $true
            break
        }
    }
    $filename = $_.Name
    if ($excludedFiles -contains $filename) {
        $isExcluded = $true
    }
    -not $isExcluded -and -not ((Get-Content $path -Raw) -match $headerPattern)
} | ForEach-Object {
    $path = $_.FullName
    $content = Get-Content $path -Raw
    Set-Content -Path $path -Value "$header`r`n$content" -Encoding UTF8
    $modifiedFiles += $path
}
$modifiedFiles | Out-File -Encoding UTF8 missing_headers.txt
Write-Host "Fertig. $(($modifiedFiles).Count) files have been changed and header has been added: see missing_headers.txt"
