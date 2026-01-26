# .csproj NuGet Packaging Checklist

Use this checklist to manually verify your .csproj has all required properties for the NuGet workflow.

## ✅ Required Properties (MUST have)

These properties are **required** for the workflow to generate proper NuGet packages with symbols:

```xml
<PropertyGroup>
  <!-- Symbol packages (.snupkg) -->
  <IncludeSymbols>true</IncludeSymbols>
  <SymbolPackageFormat>snupkg</SymbolPackageFormat>

  <!-- Source Link (for debugging) -->
  <PublishRepositoryUrl>true</PublishRepositoryUrl>
  <EmbedUntrackedSources>true</EmbedUntrackedSources>

  <!-- Basic package metadata -->
  <PackageId>YourCompany.YourLibrary</PackageId>
  <Authors>Your Company</Authors>
  <Description>Brief description of your library</Description>

  <!-- Documentation -->
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
</PropertyGroup>
```

## 📋 Recommended Properties

```xml
<PropertyGroup>
  <!-- Repository info -->
  <RepositoryUrl>https://github.com/YourOrg/YourRepo</RepositoryUrl>
  <RepositoryType>git</RepositoryType>
  <PackageProjectUrl>https://github.com/YourOrg/YourRepo</PackageProjectUrl>

  <!-- License -->
  <PackageLicenseFile>LICENSE</PackageLicenseFile>
  <!-- OR use SPDX expression: -->
  <!-- <PackageLicenseExpression>MIT</PackageLicenseExpression> -->

  <!-- README and release notes -->
  <PackageReadmeFile>README.md</PackageReadmeFile>
  <PackageReleaseNotes>Please see CHANGELOG.md for release details.</PackageReleaseNotes>

  <!-- Optional but nice -->
  <PackageIcon>icon.png</PackageIcon>
  <PackageTags>your; tags; here</PackageTags>
  <Title>User-friendly title for your package</Title>
</PropertyGroup>
```

## 📦 ItemGroup Inclusions

If you reference files in PropertyGroup, include them in an ItemGroup:

```xml
<ItemGroup>
  <!-- README (if PackageReadmeFile is set) -->
  <None Include="README.md" Pack="true" PackagePath="\" />

  <!-- License file (if PackageLicenseFile is set) -->
  <None Include="LICENSE" Pack="true" PackagePath="\" />

  <!-- Icon (if PackageIcon is set) -->
  <None Include="icon.png" Pack="true" PackagePath="\" />
</ItemGroup>
```

## 🔧 Using the Update Script

Quick update using the PowerShell script:

```powershell
# Interactive mode (will prompt for missing values)
.\tools\Update-CsprojForNuGet.ps1 -CsprojPath "YourProject.csproj"

# With all parameters
.\tools\Update-CsprojForNuGet.ps1 `
  -CsprojPath "YourProject.csproj" `
  -PackageId "Company.Project" `
  -Authors "Your Company" `
  -Description "Your description" `
  -RepositoryUrl "https://github.com/YourOrg/YourRepo" `
  -LicenseFile "LICENSE.txt" `
  -IconFile "icon.png"

# Preview changes without modifying
.\tools\Update-CsprojForNuGet.ps1 -CsprojPath "YourProject.csproj" -WhatIf
```

## ✅ Verification Steps

After updating your .csproj:

1. **Build and pack locally:**
   ```bash
   dotnet restore YourProject.csproj
   dotnet build YourProject.csproj -c Release
   dotnet pack YourProject.csproj -c Release -o ./artifacts
   ```

2. **Verify artifacts are created:**
   - `YourPackage.1.0.0.nupkg` (main package)
   - `YourPackage.1.0.0.snupkg` (symbols package)

3. **Inspect the package contents:**
   ```bash
   # Rename .nupkg to .zip and extract, or use:
   dotnet nuget verify ./artifacts/YourPackage.1.0.0.nupkg
   ```

4. **Check for XML documentation:**
   The package should include your assembly's XML documentation file.

## 🚨 Common Issues

### Missing .snupkg
**Symptom:** Only .nupkg is generated, no .snupkg
**Fix:** Ensure both `<IncludeSymbols>true</IncludeSymbols>` and `<SymbolPackageFormat>snupkg</SymbolPackageFormat>` are set

### Missing XML docs
**Symptom:** No documentation in NuGet package
**Fix:** Set `<GenerateDocumentationFile>true</GenerateDocumentationFile>`

### License/README not in package
**Symptom:** Files referenced but not included
**Fix:** Add them to an `<ItemGroup>` with `Pack="true"` and `PackagePath="\"`

### Source Link not working
**Symptom:** Can't step into source during debugging
**Fix:** Ensure `<PublishRepositoryUrl>true</PublishRepositoryUrl>` and `<EmbedUntrackedSources>true</EmbedUntrackedSources>` are set
