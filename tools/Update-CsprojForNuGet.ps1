<#
.SYNOPSIS
    Updates a .csproj file to include required NuGet packaging properties for the workflow-kit.

.DESCRIPTION
    This script adds or updates properties in a .csproj file to ensure proper NuGet packaging
    with symbols (.snupkg), source link, and metadata required by the nuget-workflow-kit.

.PARAMETER CsprojPath
    Path to the .csproj file to update.

.PARAMETER PackageId
    Optional. The NuGet package ID. If not provided, uses the project filename.

.PARAMETER Authors
    Optional. Package authors. Defaults to existing value or prompts if not set.

.PARAMETER Description
    Optional. Package description. Defaults to existing value or prompts if not set.

.PARAMETER RepositoryUrl
    Optional. Git repository URL. Attempts to detect from git remote if not provided.

.PARAMETER LicenseFile
    Optional. License file name. Defaults to "LICENSE" or "LICENSE.txt" if found in repo root.

.PARAMETER IconFile
    Optional. Icon file name for package icon (e.g., "icon.png").

.PARAMETER WhatIf
    Shows what changes would be made without actually modifying the file.

.EXAMPLE
    .\Update-CsprojForNuGet.ps1 -CsprojPath "MyProject.csproj"

.EXAMPLE
    .\Update-CsprojForNuGet.ps1 -CsprojPath "MyProject.csproj" -PackageId "Company.MyProject" -WhatIf

.EXAMPLE
    .\Update-CsprojForNuGet.ps1 -CsprojPath "MyProject.csproj" -Authors "My Company" -Description "My awesome library"
#>

[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$CsprojPath,

    [Parameter(Mandatory = $false)]
    [string]$PackageId,

    [Parameter(Mandatory = $false)]
    [string]$Authors,

    [Parameter(Mandatory = $false)]
    [string]$Description,

    [Parameter(Mandatory = $false)]
    [string]$RepositoryUrl,

    [Parameter(Mandatory = $false)]
    [string]$LicenseFile,

    [Parameter(Mandatory = $false)]
    [string]$IconFile
)

$ErrorActionPreference = 'Stop'

# Resolve the .csproj path
if (-not (Test-Path $CsprojPath)) {
    Write-Error "Cannot find .csproj file at: $CsprojPath"
    exit 1
}

$CsprojPath = Resolve-Path $CsprojPath
$projectDir = Split-Path -Parent $CsprojPath
$projectName = [System.IO.Path]::GetFileNameWithoutExtension($CsprojPath)

Write-Host "Updating project: $projectName" -ForegroundColor Cyan
Write-Host "Path: $CsprojPath" -ForegroundColor Gray

# Load XML
[xml]$xml = Get-Content $CsprojPath

# Helper function to get or create PropertyGroup
function Get-OrCreatePropertyGroup {
    param([System.Xml.XmlDocument]$XmlDoc, [string]$Condition = $null)

    $project = $XmlDoc.Project

    if ($Condition) {
        $propGroup = $project.PropertyGroup | Where-Object { $_.Condition -eq $Condition }
    } else {
        # Find a PropertyGroup without conditions and without TargetFramework
        $propGroup = $project.PropertyGroup | Where-Object {
            -not $_.Condition -and -not $_.TargetFramework -and -not $_.TargetFrameworks
        } | Select-Object -First 1
    }

    if (-not $propGroup) {
        $propGroup = $XmlDoc.CreateElement('PropertyGroup')
        if ($Condition) {
            $propGroup.SetAttribute('Condition', $Condition)
        }
        $project.AppendChild($propGroup) | Out-Null
    }

    return $propGroup
}

# Helper function to set or update element
function Set-PropertyElement {
    param(
        [System.Xml.XmlElement]$PropertyGroup,
        [string]$ElementName,
        [string]$Value,
        [switch]$OnlyIfMissing
    )

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return
    }

    $element = $PropertyGroup.SelectSingleNode($ElementName)

    if ($element) {
        if (-not $OnlyIfMissing) {
            Write-Host "  Updating <$ElementName>: $Value" -ForegroundColor Yellow
            $element.InnerText = $Value
        } else {
            Write-Host "  Keeping existing <$ElementName>: $($element.InnerText)" -ForegroundColor Gray
        }
    } else {
        Write-Host "  Adding <$ElementName>: $Value" -ForegroundColor Green
        $newElement = $xml.CreateElement($ElementName)
        $newElement.InnerText = $Value
        $PropertyGroup.AppendChild($newElement) | Out-Null
    }
}

# Get the main PropertyGroup
$mainPropGroup = Get-OrCreatePropertyGroup -XmlDoc $xml

# Detect or use provided values
if (-not $PackageId) {
    $existingId = $mainPropGroup.PackageId
    $PackageId = if ($existingId) { $existingId } else { $projectName }
}

if (-not $RepositoryUrl) {
    $existingUrl = $mainPropGroup.RepositoryUrl
    if ($existingUrl) {
        $RepositoryUrl = $existingUrl
    } else {
        # Try to detect from git
        Push-Location $projectDir
        try {
            $gitRemote = git remote get-url origin 2>$null
            if ($gitRemote) {
                # Convert SSH to HTTPS if needed
                $RepositoryUrl = $gitRemote -replace 'git@github.com:', 'https://github.com/' -replace '\.git$', ''
            }
        } catch {
            Write-Warning "Could not detect git repository URL"
        }
        Pop-Location
    }
}

# Detect license file
if (-not $LicenseFile) {
    $licenseFiles = @('LICENSE', 'LICENSE.txt', 'LICENSE.md', 'License.txt')
    foreach ($file in $licenseFiles) {
        $path = Join-Path $projectDir $file
        if (Test-Path $path) {
            $LicenseFile = $file
            break
        }
    }
}

# Interactive prompts for missing required values
if (-not $Authors) {
    $existingAuthors = $mainPropGroup.Authors
    if ($existingAuthors) {
        $Authors = $existingAuthors
    } else {
        $Authors = Read-Host "Enter package Authors (e.g., 'Your Company')"
    }
}

if (-not $Description) {
    $existingDesc = $mainPropGroup.Description
    if ($existingDesc) {
        $Description = $existingDesc
    } else {
        $Description = Read-Host "Enter package Description"
    }
}

Write-Host "`nApplying NuGet workflow properties..." -ForegroundColor Cyan

# Core identification properties
Set-PropertyElement -PropertyGroup $mainPropGroup -ElementName 'PackageId' -Value $PackageId
Set-PropertyElement -PropertyGroup $mainPropGroup -ElementName 'Authors' -Value $Authors -OnlyIfMissing
Set-PropertyElement -PropertyGroup $mainPropGroup -ElementName 'Description' -Value $Description -OnlyIfMissing

# Repository properties
if ($RepositoryUrl) {
    Set-PropertyElement -PropertyGroup $mainPropGroup -ElementName 'RepositoryUrl' -Value $RepositoryUrl -OnlyIfMissing
    Set-PropertyElement -PropertyGroup $mainPropGroup -ElementName 'RepositoryType' -Value 'git' -OnlyIfMissing
    Set-PropertyElement -PropertyGroup $mainPropGroup -ElementName 'PackageProjectUrl' -Value $RepositoryUrl -OnlyIfMissing
}

# License
if ($LicenseFile) {
    Set-PropertyElement -PropertyGroup $mainPropGroup -ElementName 'PackageLicenseFile' -Value $LicenseFile

    # Check if license is in ItemGroup
    $itemGroup = $xml.Project.ItemGroup | Where-Object { $_.None | Where-Object { $_.Include -like "*LICENSE*" } } | Select-Object -First 1
    if (-not $itemGroup) {
        $itemGroup = $xml.CreateElement('ItemGroup')
        $xml.Project.AppendChild($itemGroup) | Out-Null

        $noneElement = $xml.CreateElement('None')
        $noneElement.SetAttribute('Include', $LicenseFile)
        $noneElement.SetAttribute('Pack', 'true')
        $noneElement.SetAttribute('PackagePath', '\')
        $itemGroup.AppendChild($noneElement) | Out-Null
        Write-Host "  Adding LICENSE to ItemGroup" -ForegroundColor Green
    }
}

# Icon (if provided)
if ($IconFile) {
    Set-PropertyElement -PropertyGroup $mainPropGroup -ElementName 'PackageIcon' -Value $IconFile

    # Add to ItemGroup if not present
    $iconInItemGroup = $xml.Project.ItemGroup.None | Where-Object { $_.Include -eq $IconFile }
    if (-not $iconInItemGroup) {
        $itemGroup = $xml.Project.ItemGroup | Select-Object -First 1
        if (-not $itemGroup) {
            $itemGroup = $xml.CreateElement('ItemGroup')
            $xml.Project.AppendChild($itemGroup) | Out-Null
        }

        $noneElement = $xml.CreateElement('None')
        $noneElement.SetAttribute('Include', $IconFile)
        $noneElement.SetAttribute('Pack', 'true')
        $noneElement.SetAttribute('PackagePath', '\')
        $itemGroup.AppendChild($noneElement) | Out-Null
        Write-Host "  Adding icon to ItemGroup" -ForegroundColor Green
    }
}

# README
$readmeElement = $mainPropGroup.PackageReadmeFile
if (-not $readmeElement) {
    Set-PropertyElement -PropertyGroup $mainPropGroup -ElementName 'PackageReadmeFile' -Value 'README.md'

    # Add to ItemGroup
    $readmeInItemGroup = $xml.Project.ItemGroup.None | Where-Object { $_.Include -eq 'README.md' }
    if (-not $readmeInItemGroup) {
        $itemGroup = $xml.Project.ItemGroup | Select-Object -First 1
        if (-not $itemGroup) {
            $itemGroup = $xml.CreateElement('ItemGroup')
            $xml.Project.AppendChild($itemGroup) | Out-Null
        }

        $noneElement = $xml.CreateElement('None')
        $noneElement.SetAttribute('Include', 'README.md')
        $noneElement.SetAttribute('Pack', 'true')
        $noneElement.SetAttribute('PackagePath', '\')
        $itemGroup.AppendChild($noneElement) | Out-Null
        Write-Host "  Adding README.md to ItemGroup" -ForegroundColor Green
    }
}

# Release notes reference
Set-PropertyElement -PropertyGroup $mainPropGroup -ElementName 'PackageReleaseNotes' -Value 'Please see CHANGELOG.md for release details.' -OnlyIfMissing

# Documentation
$docFileElement = $mainPropGroup.GenerateDocumentationFile
if (-not $docFileElement -or $docFileElement -eq 'false') {
    Set-PropertyElement -PropertyGroup $mainPropGroup -ElementName 'GenerateDocumentationFile' -Value 'true'
}

# Symbol packages (.snupkg) - REQUIRED for workflow
Set-PropertyElement -PropertyGroup $mainPropGroup -ElementName 'IncludeSymbols' -Value 'true'
Set-PropertyElement -PropertyGroup $mainPropGroup -ElementName 'SymbolPackageFormat' -Value 'snupkg'

# Source Link - REQUIRED for proper debugging experience
Set-PropertyElement -PropertyGroup $mainPropGroup -ElementName 'PublishRepositoryUrl' -Value 'true'
Set-PropertyElement -PropertyGroup $mainPropGroup -ElementName 'EmbedUntrackedSources' -Value 'true'

# Packaging flags
Set-PropertyElement -PropertyGroup $mainPropGroup -ElementName 'IsPackable' -Value 'true' -OnlyIfMissing

Write-Host "`nValidating required properties..." -ForegroundColor Cyan

# Validation
$requiredProperties = @{
    'PackageId' = $PackageId
    'Authors' = $Authors
    'Description' = $Description
    'IncludeSymbols' = 'true'
    'SymbolPackageFormat' = 'snupkg'
    'PublishRepositoryUrl' = 'true'
    'EmbedUntrackedSources' = 'true'
}

$missingProperties = @()
foreach ($prop in $requiredProperties.Keys) {
    $element = $mainPropGroup.SelectSingleNode($prop)
    if (-not $element -or [string]::IsNullOrWhiteSpace($element.InnerText)) {
        $missingProperties += $prop
    }
}

if ($missingProperties.Count -gt 0) {
    Write-Warning "Missing required properties: $($missingProperties -join ', ')"
}

# Save the file
if ($PSCmdlet.ShouldProcess($CsprojPath, "Update .csproj for NuGet packaging")) {
    # Format XML nicely
    $settings = New-Object System.Xml.XmlWriterSettings
    $settings.Indent = $true
    $settings.IndentChars = '  '
    $settings.NewLineChars = "`r`n"
    $settings.Encoding = [System.Text.UTF8Encoding]::new($false) # UTF-8 without BOM

    $writer = [System.Xml.XmlWriter]::Create($CsprojPath, $settings)
    try {
        $xml.Save($writer)
        Write-Host "`nSuccessfully updated: $CsprojPath" -ForegroundColor Green
    }
    finally {
        $writer.Dispose()
    }

    Write-Host "`nNext steps:" -ForegroundColor Cyan
    Write-Host "1. Review the changes in $projectName.csproj" -ForegroundColor Gray
    Write-Host "2. Ensure LICENSE, README.md exist in your repo root" -ForegroundColor Gray
    if ($IconFile) {
        Write-Host "3. Ensure $IconFile exists in your repo root" -ForegroundColor Gray
    }
    Write-Host "4. Test build: dotnet pack $projectName.csproj -c Release" -ForegroundColor Gray
    Write-Host "5. Verify .nupkg and .snupkg are generated in bin/Release/" -ForegroundColor Gray
}

Write-Host "`nDone!" -ForegroundColor Green
