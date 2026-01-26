# Workflow kit (NuGet + Release assets + optional Unity UPM)

The workflow kit will:

- build + pack a .NET library into NuGet artifacts (`.nupkg` + `.snupkg`)
- optionally create a Unity Package Manager (UPM) tarball (`.tgz`) if a Unity package folder exists
- publish NuGet packages to nuget.org using OIDC (`NuGet/login@v1`)
- attach artifacts to the GitHub Release

## How to Use This Template

This repository provides a complete NuGet workflow as a downloadable package (`workflow-kit.zip`). Simply download the zip, extract it into your repository, and run the automated setup script.

**No manual configuration required** - the setup script handles everything automatically!

## Required Setup

### NuGet.org Configuration (One-time)

Before the workflow can publish to NuGet.org, you must configure your repository as a trusted source:

1. Go to [NuGet.org](https://www.nuget.org) and sign in
2. Navigate to: Username → API Keys
3. Scroll to "Package owners" section
4. Click "Add Package" → "Add new..." → "Configure GitHub Actions"
5. Select your organization and repository
6. Save

This enables OIDC-based publishing (no API keys needed in GitHub!).

### GitHub Secrets

- `NUGET_USER`: Your NuGet.org username or email (must match NuGet.org account)
  - Settings → Secrets and variables → Actions → New repository secret

### GitHub Environment

- Create an environment named `release` (used by the `publish-nuget` job)
  - Settings → Environments → New environment → Name: `release`

## Expected repo structure (recommended)

- `<RepoRoot>/<YourLibrary>.csproj`
- Optional Unity UPM folder: `Packages/<UPM_PACKAGE_ID>/package.json`

## For Maintainers: Regenerating workflow-kit.zip

If you're maintaining this template repository and need to regenerate `workflow-kit.zip` after making changes:

```powershell
# From the template repository root
powershell -NoProfile -ExecutionPolicy Bypass -File tools/create-workflow-kit.ps1
```

This regenerates `workflow-kit.zip` with all the latest files. Commit the updated zip to the repository so users can download the latest version.

## Installation Steps

### Step 1: Download and Extract

Download `workflow-kit.zip` from this repository and extract it into your target repository root:

```powershell
# From your target repository root
Expand-Archive path\to\workflow-kit.zip -DestinationPath . -Force
```

This extracts all workflow files, configuration files, and setup tools.

### Step 2: Run Automated Setup

Run the setup script to automatically configure everything:

```powershell
# Recommended: Auto-detect all values
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Setup-NuGetWorkflow.ps1 -AutoDetect
```

**What the script does:**
- ✅ Finds your .csproj file automatically
- ✅ Updates it with all required NuGet packaging properties
- ✅ Configures the workflow file with your project details
- ✅ Validates the configuration
- ✅ Provides clear next-step instructions

**Advanced usage** (if you want to specify values):
```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Setup-NuGetWorkflow.ps1 `
  -CsprojPath "YourProject.csproj" `
  -Authors "Your Company" `
  -Description "Your library description"
```

### Step 3: Configure GitHub

The setup script will tell you what to do, but here's the summary:

1. **Add Repository Secret** (required for NuGet publishing):
   - Settings → Secrets and variables → Actions → New repository secret
   - Name: `NUGET_USER`
   - Value: Your NuGet.org username or email

2. **Create Release Environment** (required for workflow):
   - Settings → Environments → New environment
   - Name: `release`

### Step 4: Verify Locally

Test that everything works:

```bash
dotnet pack YourProject.csproj -c Release -o ./artifacts
```

Verify both files are created:
- `YourPackage.x.y.z.nupkg`
- `YourPackage.x.y.z.snupkg`

### Step 5: Commit and Create Version Tag

```bash
# Commit your changes
git add .
git commit -m "feat: add NuGet workflow"
git push

# Create and push a version tag to trigger publishing
git tag v1.0.0
git push origin v1.0.0
```

**What happens:**
1. The workflow triggers on the `v1.0.0` tag
2. Builds and publishes your package to NuGet.org
3. Creates a GitHub Release with artifacts
4. Release Please starts tracking for future releases

**For future releases:**
- Make commits with conventional commit messages (feat:, fix:, etc.)
- Release Please creates a PR with version bump and changelog
- Merge the PR
- **Manually create and push the tag** matching the version from the PR:
  ```bash
  git pull
  git tag v1.1.0  # Use version from Release Please PR
  git push origin v1.1.0
  ```

---

## Advanced Usage

### Manual Step-by-Step Configuration

If you need finer control or want to run individual setup steps:

**Update .csproj only:**
```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Update-CsprojForNuGet.ps1 -CsprojPath "YourProject.csproj"
```

**Update workflow configuration only:**
```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Update-WorkflowConfig.ps1 -AutoDetect
```

**Manual configuration reference:**
See `tools/CSPROJ-CHECKLIST.md` for a complete checklist of required properties if you prefer to edit files manually.

---
