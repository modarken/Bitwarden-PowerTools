# Screenshots and Images

This folder contains screenshots and images referenced in the main README.

## Required Images

Add the following screenshots to this folder:

### Installation & Setup
- `download-release.png` - GitHub releases page showing download button
- `installer.png` - Installation wizard screenshot
- `system-tray.png` - System tray showing the app icon

### Usage & Configuration
- `main-window.png` - Main application window
- `tray-menu.png` - Right-click tray menu
- `settings-connection.png` - Settings window with Bitwarden connection fields
- `settings-hotkey.png` - Hotkey configuration section
- `settings-backup.png` - Backup settings section
- `settings-full.png` - Complete settings window
- `settings-backup-full.png` - Full backup configuration
- `tray-menu-full.png` - Complete tray menu with all options

### Bitwarden Integration
- `bitwarden-custom-field.png` - Adding AutoType:Custom field in Bitwarden

### Features
- `elevation-warning.png` - Yellow warning banner when elevation is needed
- `protected-window.png` - Example of protected window detection
- `autotype-demo.gif` - Animated GIF showing auto-type in action

## Image Guidelines

### Format
- Static screenshots: PNG format
- Animations: GIF format (keep under 5MB)
- Resolution: 1920x1080 or native window size
- DPI: 96 DPI for web display

### Content
- Use light theme for consistency
- Blur or remove any personal information
- Use example credentials (e.g., user@example.com)
- Highlight relevant UI elements with red boxes or arrows if needed

### Naming
- Use descriptive kebab-case names
- Match the filenames referenced in README.md exactly
- Don't use spaces in filenames

## Optional Enhancements

Consider adding:
- `hero-banner.png` - Large banner image for top of README (1200x400px)
- `feature-comparison.png` - Table comparing with other tools
- `workflow-diagram.png` - Visual flowchart of how auto-type works
- `icon-variants/` folder - App icons in various sizes

## Example Workflow

1. Take screenshot (Win+Shift+S on Windows)
2. Save to this folder with appropriate name
3. Verify filename matches README.md reference
4. Commit and push images with your changes

```powershell
# Add all images
git add .github/images/*.png .github/images/*.gif

# Commit
git commit -m "docs: Add README screenshots"
```
