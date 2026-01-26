<#
.SYNOPSIS
    Complete setup script for configuring the NuGet workflow in a repository.

.DESCRIPTION
    This script performs all necessary setup steps for the NuGet workflow kit:
    1. Updates the .csproj file with required NuGet packaging properties
    2. Updates the workflow file with repository-specific environment variables
    3. Validates the configuration

.PARAMETER CsprojPath
    Path to the .csproj file (relative to repo root, e.g., "MyProject.csproj")

.PARAMETER PackageId
    NuGet package ID. If not provided, uses project filename.

.PARAMETER Authors
    Package authors. If not provided, will prompt or use existing value.

.PARAMETER Description
    Package description. If not provided, will prompt or use existing value.

.PARAMETER UpmPackageId
    Unity package ID (optional, only if using Unity)

.PARAMETER UpmPackageDir
    Unity package directory (optional, only if using Unity)

.PARAMETER AutoDetect
    Attempt to auto-detect all values from repository structure.

.PARAMETER SkipCsproj
    Skip updating the .csproj file (if already configured)

.PARAMETER SkipWorkflow
    Skip updating the workflow file (if already configured)

.PARAMETER WhatIf
    Preview changes without making modifications.

.EXAMPLE
    .\Setup-NuGetWorkflow.ps1 -AutoDetect

.EXAMPLE
    .\Setup-NuGetWorkflow.ps1 -CsprojPath "MyProject.csproj" -Authors "My Company" -Description "My awesome library"

.EXAMPLE
    .\Setup-NuGetWorkflow.ps1 -CsprojPath "MyProject.csproj" -UpmPackageId "com.mycompany.mylib" -UpmPackageDir "Packages/com.mycompany.mylib"

.EXAMPLE
    .\Setup-NuGetWorkflow.ps1 -AutoDetect -WhatIf
#>

[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory = $false)]
    [string]$CsprojPath,

    [Parameter(Mandatory = $false)]
    [string]$PackageId,

    [Parameter(Mandatory = $false)]
    [string]$Authors,

    [Parameter(Mandatory = $false)]
    [string]$Description,

    [Parameter(Mandatory = $false)]
    [string]$UpmPackageId,

    [Parameter(Mandatory = $false)]
    [string]$UpmPackageDir,

    [Parameter(Mandatory = $false)]
    [switch]$AutoDetect,

    [Parameter(Mandatory = $false)]
    [switch]$SkipCsproj,

    [Parameter(Mandatory = $false)]
    [switch]$SkipWorkflow
)

$ErrorActionPreference = 'Stop'

Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  NuGet Workflow Setup" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

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

Write-Host "Repository root: $repoRoot" -ForegroundColor Gray
Write-Host ""

# Get script directory
$toolsDir = Split-Path -Parent $MyInvocation.MyCommand.Path

# Step 1: Update .csproj file
if (-not $SkipCsproj) {
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Yellow
    Write-Host "  Step 1: Updating .csproj for NuGet packaging" -ForegroundColor Yellow
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Yellow
    Write-Host ""

    $csprojScript = Join-Path $toolsDir "Update-CsprojForNuGet.ps1"

    if (-not (Test-Path $csprojScript)) {
        Write-Error "Could not find Update-CsprojForNuGet.ps1 in tools directory"
        exit 1
    }

    $csprojArgs = @{}
    if ($CsprojPath) { $csprojArgs['CsprojPath'] = $CsprojPath }
    if ($PackageId) { $csprojArgs['PackageId'] = $PackageId }
    if ($Authors) { $csprojArgs['Authors'] = $Authors }
    if ($Description) { $csprojArgs['Description'] = $Description }
    if ($WhatIfPreference) { $csprojArgs['WhatIf'] = $true }

    # If no CsprojPath provided, try to auto-detect
    if (-not $CsprojPath) {
        $csprojFiles = Get-ChildItem -Path $repoRoot -Filter "*.csproj" -File | Where-Object { $_.Directory.FullName -eq $repoRoot }
        if ($csprojFiles.Count -eq 1) {
            $CsprojPath = $csprojFiles[0].FullName
            $csprojArgs['CsprojPath'] = $CsprojPath
        }
    }

    if (-not $csprojArgs.ContainsKey('CsprojPath')) {
        Write-Error "No .csproj file specified. Use -CsprojPath or ensure a .csproj exists in repo root."
        exit 1
    }

    & $csprojScript @csprojArgs

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to update .csproj file"
        exit 1
    }

    Write-Host ""
} else {
    Write-Host "Skipping .csproj update (SkipCsproj specified)" -ForegroundColor Gray
    Write-Host ""
}

# Step 2: Update workflow configuration
if (-not $SkipWorkflow) {
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Yellow
    Write-Host "  Step 2: Updating workflow configuration" -ForegroundColor Yellow
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Yellow
    Write-Host ""

    $workflowScript = Join-Path $toolsDir "Update-WorkflowConfig.ps1"

    if (-not (Test-Path $workflowScript)) {
        Write-Error "Could not find Update-WorkflowConfig.ps1 in tools directory"
        exit 1
    }

    $workflowArgs = @{}

    # Use CsprojPath from earlier if not already set
    if (-not $CsprojPath -and $csprojArgs.ContainsKey('CsprojPath')) {
        $CsprojPath = Split-Path -Leaf $csprojArgs['CsprojPath']
    }

    if ($CsprojPath) { $workflowArgs['DotnetProject'] = Split-Path -Leaf $CsprojPath }
    if ($UpmPackageId) { $workflowArgs['UpmPackageId'] = $UpmPackageId }
    if ($UpmPackageDir) { $workflowArgs['UpmPackageDir'] = $UpmPackageDir }
    if ($AutoDetect) { $workflowArgs['AutoDetect'] = $true }
    if ($WhatIfPreference) { $workflowArgs['WhatIf'] = $true }

    & $workflowScript @workflowArgs

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to update workflow configuration"
        exit 1
    }

    Write-Host ""
} else {
    Write-Host "Skipping workflow update (SkipWorkflow specified)" -ForegroundColor Gray
    Write-Host ""
}

# Step 3: Validation and next steps
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Green
Write-Host "  Setup Complete!" -ForegroundColor Green
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Green
Write-Host ""

if (-not $WhatIfPreference) {
    Write-Host "Next steps to complete the workflow setup:" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "1. GitHub Repository Settings:" -ForegroundColor Yellow
    Write-Host "   • Add secret: NUGET_USER (your NuGet.org username/email)" -ForegroundColor Gray
    Write-Host "   • Create environment: 'release'" -ForegroundColor Gray
    Write-Host "   • Settings → Environments → New environment → 'release'" -ForegroundColor DarkGray
    Write-Host ""

    Write-Host "2. Verify Required Files Exist:" -ForegroundColor Yellow
    Write-Host "   • README.md (in repo root)" -ForegroundColor Gray
    Write-Host "   • LICENSE or LICENSE.txt (in repo root)" -ForegroundColor Gray
    Write-Host "   • CHANGELOG.md will be created by Release Please" -ForegroundColor DarkGray
    Write-Host ""

    Write-Host "3. Test Locally:" -ForegroundColor Yellow
    if ($CsprojPath) {
        $csprojFile = Split-Path -Leaf $CsprojPath
        Write-Host "   dotnet restore $csprojFile" -ForegroundColor Gray
        Write-Host "   dotnet build $csprojFile -c Release" -ForegroundColor Gray
        Write-Host "   dotnet pack $csprojFile -c Release -o ./artifacts" -ForegroundColor Gray
    } else {
        Write-Host "   dotnet restore YourProject.csproj" -ForegroundColor Gray
        Write-Host "   dotnet build YourProject.csproj -c Release" -ForegroundColor Gray
        Write-Host "   dotnet pack YourProject.csproj -c Release -o ./artifacts" -ForegroundColor Gray
    }
    Write-Host "   # Verify both .nupkg and .snupkg are created" -ForegroundColor DarkGray
    Write-Host ""

    Write-Host "4. Commit and Test Workflow:" -ForegroundColor Yellow
    Write-Host "   git add ." -ForegroundColor Gray
    Write-Host "   git commit -m 'feat: setup NuGet workflow'" -ForegroundColor Gray
    Write-Host "   git push" -ForegroundColor Gray
    Write-Host "   # Release Please will create a PR with version bump" -ForegroundColor DarkGray
    Write-Host "   # Merge the PR to trigger release and NuGet publish" -ForegroundColor DarkGray
    Write-Host ""

    Write-Host "5. Documentation:" -ForegroundColor Yellow
    Write-Host "   See docs/WorkflowKit.md for complete documentation" -ForegroundColor Gray
    Write-Host ""

} else {
    Write-Host "Preview mode (WhatIf) - no changes were made" -ForegroundColor Yellow
    Write-Host "Run without -WhatIf to apply changes" -ForegroundColor Gray
    Write-Host ""
}

Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
