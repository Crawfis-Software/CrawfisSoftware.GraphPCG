<#
.SYNOPSIS
    Updates the nuget-package.yml workflow file with repository-specific environment variables.

.DESCRIPTION
    This script updates the env section of .github/workflows/nuget-package.yml with the
    appropriate values for DOTNET_PROJECT, UPM_PACKAGE_ID, and UPM_PACKAGE_DIR.

.PARAMETER DotnetProject
    Path to the .csproj file to pack (e.g., "MyProject.csproj")

.PARAMETER UpmPackageId
    Unity Package Manager package ID (e.g., "com.company.package").
    Optional - only needed if you have a Unity package.

.PARAMETER UpmPackageDir
    Unity Package Manager package directory (e.g., "Packages/com.company.package").
    Optional - only needed if you have a Unity package.

.PARAMETER WorkflowFile
    Path to the workflow file. Defaults to ".github/workflows/nuget-package.yml"

.PARAMETER AutoDetect
    Attempt to auto-detect values from the repository structure.

.PARAMETER WhatIf
    Shows what changes would be made without actually modifying the file.

.EXAMPLE
    .\Update-WorkflowConfig.ps1 -DotnetProject "MyCompany.MyLib.csproj"

.EXAMPLE
    .\Update-WorkflowConfig.ps1 -DotnetProject "MyProject.csproj" -UpmPackageId "com.mycompany.mylib" -UpmPackageDir "Packages/com.mycompany.mylib"

.EXAMPLE
    .\Update-WorkflowConfig.ps1 -AutoDetect

.EXAMPLE
    .\Update-WorkflowConfig.ps1 -AutoDetect -WhatIf
#>

[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory = $false)]
    [string]$DotnetProject,

    [Parameter(Mandatory = $false)]
    [string]$UpmPackageId,

    [Parameter(Mandatory = $false)]
    [string]$UpmPackageDir,

    [Parameter(Mandatory = $false)]
    [string]$WorkflowFile = ".github/workflows/nuget-package.yml",

    [Parameter(Mandatory = $false)]
    [switch]$AutoDetect
)

$ErrorActionPreference = 'Stop'

# Find repository root
$repoRoot = Get-Location
while ($repoRoot -and -not (Test-Path (Join-Path $repoRoot ".git"))) {
    $parent = Split-Path $repoRoot -Parent
    if ($parent -eq $repoRoot) {
        Write-Error "Could not find repository root (no .git folder found)"
        exit 1
    }
    $repoRoot = $parent
}

Write-Host "Repository root: $repoRoot" -ForegroundColor Cyan

# Resolve workflow file path
$workflowPath = Join-Path $repoRoot $WorkflowFile
if (-not (Test-Path $workflowPath)) {
    Write-Error "Workflow file not found at: $workflowPath"
    exit 1
}

Write-Host "Workflow file: $workflowPath" -ForegroundColor Cyan

# Auto-detect values if requested
if ($AutoDetect) {
    Write-Host "`nAuto-detecting configuration..." -ForegroundColor Cyan

    # Find .csproj file
    if (-not $DotnetProject) {
        $csprojFiles = Get-ChildItem -Path $repoRoot -Filter "*.csproj" -File | Where-Object { $_.Directory.FullName -eq $repoRoot }
        if ($csprojFiles.Count -eq 1) {
            $DotnetProject = $csprojFiles[0].Name
            Write-Host "  Found .csproj: $DotnetProject" -ForegroundColor Green
        } elseif ($csprojFiles.Count -gt 1) {
            Write-Host "  Multiple .csproj files found in root:" -ForegroundColor Yellow
            $csprojFiles | ForEach-Object { Write-Host "    - $($_.Name)" -ForegroundColor Gray }
            Write-Host "  Please specify which one to use with -DotnetProject" -ForegroundColor Yellow
        } else {
            Write-Warning "No .csproj file found in repository root"
        }
    }

    # Find Unity package
    if (-not $UpmPackageId -or -not $UpmPackageDir) {
        $packagesDir = Join-Path $repoRoot "Packages"
        if (Test-Path $packagesDir) {
            $upmPackages = Get-ChildItem -Path $packagesDir -Directory | Where-Object {
                Test-Path (Join-Path $_.FullName "package.json")
            }

            if ($upmPackages.Count -eq 1) {
                $packageJsonPath = Join-Path $upmPackages[0].FullName "package.json"
                $packageJson = Get-Content $packageJsonPath -Raw | ConvertFrom-Json
                $UpmPackageId = $packageJson.name
                $UpmPackageDir = "Packages/$($upmPackages[0].Name)"
                Write-Host "  Found Unity package: $UpmPackageId" -ForegroundColor Green
                Write-Host "  Package directory: $UpmPackageDir" -ForegroundColor Green
            } elseif ($upmPackages.Count -gt 1) {
                Write-Host "  Multiple Unity packages found:" -ForegroundColor Yellow
                $upmPackages | ForEach-Object { Write-Host "    - $($_.Name)" -ForegroundColor Gray }
            } else {
                Write-Host "  No Unity packages found (this is fine if not using Unity)" -ForegroundColor Gray
            }
        }
    }
}

# Interactive prompts for missing required values
if (-not $DotnetProject) {
    # Try to suggest from repo
    $csprojFiles = Get-ChildItem -Path $repoRoot -Filter "*.csproj" -File | Where-Object { $_.Directory.FullName -eq $repoRoot }
    if ($csprojFiles.Count -gt 0) {
        Write-Host "`nAvailable .csproj files:" -ForegroundColor Cyan
        for ($i = 0; $i -lt $csprojFiles.Count; $i++) {
            Write-Host "  [$i] $($csprojFiles[$i].Name)" -ForegroundColor Gray
        }
        $selection = Read-Host "Select a .csproj file by number, or enter a custom path"
        if ($selection -match '^\d+$' -and [int]$selection -lt $csprojFiles.Count) {
            $DotnetProject = $csprojFiles[[int]$selection].Name
        } else {
            $DotnetProject = $selection
        }
    } else {
        $DotnetProject = Read-Host "Enter the .csproj file name (e.g., 'MyProject.csproj')"
    }
}

# Validate .csproj exists
$csprojPath = Join-Path $repoRoot $DotnetProject
if (-not (Test-Path $csprojPath)) {
    Write-Warning ".csproj file not found at: $csprojPath"
    Write-Warning "Make sure the path is relative to the repository root"
}

# UPM values are optional
if (-not $UpmPackageId -and -not $UpmPackageDir) {
    $useUpm = Read-Host "Do you have a Unity package? (y/N)"
    if ($useUpm -eq 'y' -or $useUpm -eq 'Y') {
        $UpmPackageId = Read-Host "Enter Unity package ID (e.g., 'com.company.package')"
        $UpmPackageDir = Read-Host "Enter Unity package directory (e.g., 'Packages/com.company.package')"
    } else {
        Write-Host "Skipping Unity package configuration (will use placeholder values)" -ForegroundColor Gray
        # Use placeholder values that won't break the workflow
        $UpmPackageId = "com.example.placeholder"
        $UpmPackageDir = "Packages/com.example.placeholder"
    }
}

Write-Host "`nConfiguration to apply:" -ForegroundColor Cyan
Write-Host "  DOTNET_PROJECT: $DotnetProject" -ForegroundColor Gray
Write-Host "  UPM_PACKAGE_ID: $UpmPackageId" -ForegroundColor Gray
Write-Host "  UPM_PACKAGE_DIR: $UpmPackageDir" -ForegroundColor Gray

# Read and update the workflow file
$content = Get-Content $workflowPath -Raw

# Use regex to replace the env values while preserving formatting
$originalContent = $content

# Update DOTNET_PROJECT
$content = $content -replace "(DOTNET_PROJECT:\s*')[^']*(')", "`${1}$DotnetProject`${2}"

# Update UPM_PACKAGE_ID
$content = $content -replace "(UPM_PACKAGE_ID:\s*')[^']*(')", "`${1}$UpmPackageId`${2}"

# Update UPM_PACKAGE_DIR
$content = $content -replace "(UPM_PACKAGE_DIR:\s*')[^']*(')", "`${1}$UpmPackageDir`${2}"

# Check if anything changed
if ($content -eq $originalContent) {
    Write-Warning "No changes detected. The workflow file may already have these values."
    Write-Host "`nCurrent values in workflow file:" -ForegroundColor Yellow

    if ($originalContent -match "DOTNET_PROJECT:\s*'([^']*)'") {
        Write-Host "  DOTNET_PROJECT: $($matches[1])" -ForegroundColor Gray
    }
    if ($originalContent -match "UPM_PACKAGE_ID:\s*'([^']*)'") {
        Write-Host "  UPM_PACKAGE_ID: $($matches[1])" -ForegroundColor Gray
    }
    if ($originalContent -match "UPM_PACKAGE_DIR:\s*'([^']*)'") {
        Write-Host "  UPM_PACKAGE_DIR: $($matches[1])" -ForegroundColor Gray
    }
}

# Save the file
if ($PSCmdlet.ShouldProcess($workflowPath, "Update workflow configuration")) {
    Set-Content -Path $workflowPath -Value $content -NoNewline -Encoding UTF8

    Write-Host "`n[SUCCESS] Successfully updated workflow configuration!" -ForegroundColor Green

    Write-Host "`nNext steps:" -ForegroundColor Cyan
    Write-Host "1. Review changes: git diff $WorkflowFile" -ForegroundColor Gray
    Write-Host "2. Ensure .csproj file exists at: $DotnetProject" -ForegroundColor Gray
    if ($UpmPackageId -ne "com.example.placeholder") {
        Write-Host "3. Ensure Unity package exists at: $UpmPackageDir" -ForegroundColor Gray
    }
    Write-Host "4. Test locally: dotnet pack $DotnetProject -c Release -o ./artifacts" -ForegroundColor Gray
}

Write-Host "`nDone!" -ForegroundColor Green
