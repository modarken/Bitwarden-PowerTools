# Building and Releasing Bitwarden AutoType

This document explains how to build, test, and release the Bitwarden AutoType application.

## Overview

The application uses **Velopack** for packaging and distribution. The build process creates:
- A Windows installer (`Setup.exe`)
- Update packages (`*.nupkg`)
- A portable ZIP version
- Auto-update manifests

## Build Tools

- **.NET SDK 10.0+** - Application framework
- **Velopack CLI (`vpk`)** - Packaging and installer creation
- **GitHub Actions** - Automated CI/CD

## Development Build

For development and testing during coding:

```powershell
# Standard debug build
dotnet build

# Run from Visual Studio or command line
dotnet run --project Bitwarden.AutoType\Bitwarden.AutoType.Desktop\Bitwarden.AutoType.Desktop.csproj
```

**Note:** Updates are disabled when running from development (not installed via Velopack).

---

## Release Build - Option 1: Local Testing (Recommended First)

Use this to build and test the installer on your local machine before releasing to GitHub.

### Prerequisites

Install Velopack CLI (one-time setup):
```powershell
dotnet tool install -g vpk
```

### Build the Release

From the repository root:

```powershell
# Build with current version from .csproj
.\scripts\Build-Release.ps1

# Build with specific version
.\scripts\Build-Release.ps1 -Version "1.3.2"

# Build and install immediately for testing
.\scripts\Build-Release.ps1 -Version "1.3.2" -Install
```

### Script Parameters

| Parameter | Description | Example |
|-----------|-------------|---------|
| `-Version` | Version number to build | `"1.3.2"` |
| `-Install` | Install immediately after build | `-Install` |
| `-NoDelta` | Skip delta package generation | `-NoDelta` |

### Output Files

Files are created in `.\releases\` folder:

```
releases/
├── Bitwarden.AutoType-win-Setup.exe          ← Main installer (distribute this)
├── Bitwarden.AutoType-1.3.2-full.nupkg       ← Update package
├── Bitwarden.AutoType-win-Portable.zip       ← Portable version
├── RELEASES                                   ← Version manifest
├── releases.win.json                          ← Release metadata
└── assets.win.json                            ← Asset metadata
```

### Testing the Build

1. Install the build:
   ```powershell
   .\releases\Bitwarden.AutoType-win-Setup.exe
   ```

2. Test the application thoroughly:
   - Check basic functionality
   - Test auto-type with normal windows
   - Test elevation detection with protected windows
   - Verify settings persist
   - Test system tray menu

3. Uninstall when done testing:
   - Via Windows Settings → Apps
   - Or run the uninstaller from install directory

---

## Release Build - Option 2: Automated via GitHub Actions

This is the recommended approach for official releases. GitHub Actions automatically builds, packages, and creates a release.

### Prerequisites

- All changes committed to git
- Version number decided (e.g., `1.3.2`)

### Release Process

1. **Update version (optional - GitHub Actions does this automatically)**
   
   If you want to manually set the version first:
   ```xml
   <!-- In Bitwarden.AutoType.Desktop.csproj -->
   <Version>1.3.2</Version>
   <AssemblyVersion>1.3.2.0</AssemblyVersion>
   <FileVersion>1.3.2.0</FileVersion>
   ```

2. **Commit your changes**
   ```bash
   git add .
   git commit -m "feat: Add smart elevation detection for protected windows"
   ```

3. **Create and push version tag**
   ```bash
   git tag v1.3.2
   git push origin main
   git push origin v1.3.2
   ```

4. **Monitor the build**
   - Go to: `https://github.com/modarken/Bitwarden-PowerTools/actions`
   - Click on the running workflow
   - Wait 5-10 minutes for completion

5. **Verify the release**
   - Go to: `https://github.com/modarken/Bitwarden-PowerTools/releases`
   - Verify files are uploaded
   - Download and test the installer

### What GitHub Actions Does

1. ✅ Checks out code
2. ✅ Installs .NET SDK 10.0
3. ✅ Installs Velopack CLI tool
4. ✅ Updates version numbers in project file
5. ✅ Restores NuGet packages
6. ✅ Builds in Release configuration
7. ✅ Publishes self-contained Windows executable
8. ✅ Packages with Velopack (creates installer)
9. ✅ Creates GitHub Release
10. ✅ Uploads all files to release
11. ✅ Generates release notes

### Workflow Configuration

The automated build is configured in `.github/workflows/release.yml`.

**Triggers:**
- Push of tag matching `v*` (e.g., `v1.3.2`, `v2.0.0`)
- Manual workflow dispatch (Actions tab on GitHub)

**Environment Variables:**
```yaml
DOTNET_VERSION: '10.0.x'
PROJECT_PATH: 'Bitwarden.AutoType/Bitwarden.AutoType.Desktop/Bitwarden.AutoType.Desktop.csproj'
APP_ID: 'Bitwarden.AutoType'
EXE_NAME: 'Bitwarden.AutoType.Desktop.exe'
```

---

## Version Numbering

Follow semantic versioning: `MAJOR.MINOR.PATCH`

- **MAJOR**: Breaking changes (e.g., `2.0.0`)
- **MINOR**: New features, non-breaking (e.g., `1.4.0`)
- **PATCH**: Bug fixes, minor improvements (e.g., `1.3.2`)

### Current Version

Check the current version:
```powershell
# In .csproj file
Select-String -Path "Bitwarden.AutoType\Bitwarden.AutoType.Desktop\Bitwarden.AutoType.Desktop.csproj" -Pattern "<Version>"

# Latest git tag
git describe --tags --abbrev=0
```

---

## Manual Steps for Velopack Packaging

If you need to run the packaging steps manually (advanced):

```powershell
# 1. Publish the application
dotnet publish Bitwarden.AutoType\Bitwarden.AutoType.Desktop\Bitwarden.AutoType.Desktop.csproj `
  --configuration Release `
  --runtime win-x64 `
  --self-contained true `
  --output ./publish

# 2. Package with Velopack
vpk pack `
  --packId "Bitwarden.AutoType" `
  --packVersion "1.3.2" `
  --packDir "./publish" `
  --mainExe "Bitwarden.AutoType.Desktop.exe" `
  --outputDir "./releases" `
  --icon "./Bitwarden.AutoType/Bitwarden.AutoType.Desktop/Bitwarden.AutoType.ico" `
  --shortcuts "StartMenuRoot,Startup" `
  --delta BestSize
```

---

## Distribution and Updates

### For End Users

**New Installation:**
1. Download `Bitwarden.AutoType-win-Setup.exe` from GitHub Releases
2. Run the installer
3. Application installs to: `%LocalAppData%\Bitwarden.AutoType\`
4. Shortcuts created in Start Menu and Startup folder

**Updates:**
- Automatic: App checks for updates on startup and periodically
- Manual: Right-click tray icon → "Check for Updates..."
- Update source: GitHub Releases at `modarken/Bitwarden-PowerTools`

### How Auto-Updates Work

1. `UpdateService.cs` checks GitHub Releases for newer versions
2. If found, downloads the `.nupkg` package
3. User is prompted to install
4. Velopack applies update and restarts application
5. Delta updates minimize download size

---

## Troubleshooting

### Build Fails - "vpk command not found"

Install Velopack CLI:
```powershell
dotnet tool install -g vpk
```

### Build Fails - .NET SDK not found

Install .NET 10.0 SDK:
- Download from: https://dotnet.microsoft.com/download

### GitHub Actions fails

1. Check the workflow run logs in GitHub Actions tab
2. Verify the tag format is `v*` (e.g., `v1.3.2`)
3. Ensure GITHUB_TOKEN has proper permissions (should be automatic)

### Installer doesn't work

1. Verify antivirus isn't blocking the installer
2. Check Windows SmartScreen (click "More info" → "Run anyway")
3. Try building with `-NoDelta` flag
4. Check build logs for errors

### Updates not appearing for users

1. Verify files are in GitHub Releases
2. Check `RELEASES` manifest file is present
3. Verify users are on Velopack-installed version (not dev build)
4. Check `UpdateService.cs` GitHub URL matches your repo

---

## Best Practices

### Before Releasing

- ✅ Test locally first using `Build-Release.ps1 -Install`
- ✅ Verify all features work as expected
- ✅ Check version number is incremented
- ✅ Update release notes if needed
- ✅ Commit all changes before tagging
- ✅ Use semantic versioning

### Release Checklist

- [ ] Code builds successfully (`dotnet build`)
- [ ] Local release build works (`.\scripts\Build-Release.ps1 -Install`)
- [ ] Test installer on clean machine (optional but recommended)
- [ ] Commit all changes
- [ ] Create version tag (`git tag v1.3.2`)
- [ ] Push tag to GitHub
- [ ] Monitor GitHub Actions build
- [ ] Verify release appears on GitHub
- [ ] Download and test released installer
- [ ] Announce release (if applicable)

### Versioning Strategy

```
Current: v1.3.1
├─ Bug fix          → v1.3.2
├─ New feature      → v1.4.0
└─ Breaking change  → v2.0.0
```

---

## Quick Reference

```powershell
# Local test build
.\scripts\Build-Release.ps1 -Version "1.3.2" -Install

# Release to GitHub
git tag v1.3.2
git push origin v1.3.2

# Check current version
git describe --tags --abbrev=0

# View build history
git log --oneline --decorate

# Clean build artifacts
Remove-Item .\publish,.\releases -Recurse -Force
```

---

## Support

- **Issues:** https://github.com/modarken/Bitwarden-PowerTools/issues
- **Releases:** https://github.com/modarken/Bitwarden-PowerTools/releases
- **Workflows:** https://github.com/modarken/Bitwarden-PowerTools/actions
