# Building and Releasing Bitwarden AutoType

This document explains how to build, test, and release the Bitwarden AutoType application.

## Recommended Workflow

Use this sequence for normal development and releases:

1. Create a feature branch from `main`.
2. Make changes, run tests, and open a pull request.
3. Merge the pull request into `main`.
4. Fast-forward local `main` and clean up the merged branch.
5. Pick a brand new release version that has never been used before.
6. Run a local release build and test the installer.
7. Commit the version bump on `main`.
8. Create and push a new `v*` tag.
9. Verify the GitHub Actions release workflow and published release assets.

This keeps PR work separate from release work and matches normal GitHub flow.

## Critical Rule: Never Reuse A Release Version

Do not reuse a version number after it has been:

- published as a Git tag
- published as a GitHub release
- installed locally on a machine where you test auto-updates

Velopack compares semantic version only. If a machine already has `1.3.2` installed, publishing a different build that is also `1.3.2` will not produce an update prompt.

The `Build-Release.ps1` script now blocks this by default when:

- the requested version already exists as a tag or GitHub release
- the same version is already installed locally

You can override that behavior with `-ForceVersionReuse`, but that should be rare.

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
.\scripts\Build-Release.ps1 -Version "1.3.3"

# Build and install immediately for testing
.\scripts\Build-Release.ps1 -Version "1.3.3" -Install
```

### Script Parameters

| Parameter | Description | Example |
|-----------|-------------|---------|
| `-Version` | Version number to build | `"1.3.3"` |
| `-Install` | Install immediately after build | `-Install` |
| `-NoDelta` | Skip delta package generation | `-NoDelta` |
| `-ForceVersionReuse` | Override version reuse safety checks | `-ForceVersionReuse` |

### Output Files

Files are created in `.\releases\` folder:

```
releases/
├── Bitwarden.AutoType-win-Setup.exe          ← Main installer (distribute this)
├── Bitwarden.AutoType-1.3.3-full.nupkg       ← Update package
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
- Version number decided (e.g., `1.3.3`)

### Release Process

1. **Merge all intended pull requests first**

   Releases should be cut from `main` after the relevant PRs are merged.

2. **Update version on `main`**

   This repository commits the version bump before tagging so the tagged commit and the built release match.

   Example:
   ```xml
   <!-- In Bitwarden.AutoType.Desktop.csproj -->
   <Version>1.3.3</Version>
   <AssemblyVersion>1.3.3.0</AssemblyVersion>
   <FileVersion>1.3.3.0</FileVersion>
   ```

3. **Validate a local release build**

   ```powershell
   .\scripts\Build-Release.ps1 -Version "1.3.3" -Install
   ```

4. **Commit and push the version bump on `main`**

   ```bash
   git add Bitwarden.AutoType/Bitwarden.AutoType.Desktop/Bitwarden.AutoType.Desktop.csproj
   git commit -m "Bump version to 1.3.3"
   git push origin main
   ```

5. **Create and push version tag**
   ```bash
   git tag v1.3.3
   git push origin v1.3.3
   ```

6. **Monitor the build**
   - Go to: `https://github.com/modarken/Bitwarden-PowerTools/actions`
   - Click on the running workflow
   - Wait 5-10 minutes for completion

7. **Verify the release**
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
- Push of tag matching `v*` (e.g., `v1.3.4`, `v2.0.0`)
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
- **PATCH**: Bug fixes, minor improvements (e.g., `1.3.4`)

### Current Version

Check the current version:
```powershell
# In .csproj file
Select-String -Path "Bitwarden.AutoType\Bitwarden.AutoType.Desktop\Bitwarden.AutoType.Desktop.csproj" -Pattern "<Version>"

# Latest git tag
git describe --tags --abbrev=0

# Published releases
gh release list --limit 10
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
   --packVersion "1.3.3" `
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
5. Verify the published version is newer than the version installed on your update-test machine

---

## Best Practices

### Before Releasing

- ✅ Test locally first using `Build-Release.ps1 -Install`
- ✅ Verify all features work as expected
- ✅ Check version number is incremented and unused
- ✅ Update release notes if needed
- ✅ Commit all changes before tagging
- ✅ Use semantic versioning

### Release Checklist

- [ ] Code builds successfully (`dotnet build`)
- [ ] Local release build works (`.\scripts\Build-Release.ps1 -Version "1.3.3" -Install`)
- [ ] Test installer on clean machine (optional but recommended)
- [ ] Confirm version has not been used before as a release tag or installed update-test build
- [ ] Commit all changes
- [ ] Push `main`
- [ ] Create version tag (`git tag v1.3.3`)
- [ ] Push tag to GitHub
- [ ] Monitor GitHub Actions build
- [ ] Verify release appears on GitHub
- [ ] Download and test released installer
- [ ] Announce release (if applicable)

### Versioning Strategy

```
Current: v1.3.3
├─ Bug fix          → v1.3.4
├─ New feature      → v1.4.0
└─ Breaking change  → v2.0.0
```

---

## Quick Reference

```powershell
# Local test build
.\scripts\Build-Release.ps1 -Version "1.3.4" -Install

# Release to GitHub
git push origin main
git tag v1.3.4
git push origin v1.3.4

# Check current version
git describe --tags --abbrev=0

# Check published releases
gh release list --limit 10

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
