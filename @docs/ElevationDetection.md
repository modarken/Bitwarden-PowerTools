# Elevation Detection Implementation

## Overview
The application now runs **without requiring admin privileges by default**, but automatically detects when it encounters a protected window (like Windows Security credential dialogs) and offers a quick way to restart with admin rights.

## What Changed

### 1. **Manifest** - Now runs as normal user
- Changed from `highestAvailable` to `asInvoker`
- No UAC prompt on startup
- Works for 95% of windows

### 2. **ElevationHelper.cs** - Smart elevation detection
- `IsRunningAsAdministrator()` - Checks current privilege level
- `DoesWindowRequireElevation(windowHandle)` - Checks if target window needs admin rights
  - Compares integrity levels between processes
  - Returns true for protected windows like CredentialUIBroker
- `RestartAsAdministrator()` - Restarts the app with elevation (shows UAC prompt)

### 3. **AutoTypeViewModel.cs** - Integrated detection
- Checks elevation status on startup
- Before autotyping, checks if target window requires elevation
- Shows warning banner when elevated window is detected
- Provides "Restart as Admin" button

### 4. **MainWindow.xaml** - Warning UI
- Yellow warning banner appears when elevation is needed
- Shows which window requires admin access
- One-click restart button with shield icon

## How It Works

1. **Normal operation:** App runs without admin rights, works for all normal windows
2. **When you try to autotype into RDP credential dialog:**
   - App detects the window has higher integrity level (CredentialUIBroker)
   - Shows yellow warning: "The window 'Windows Security' requires administrator privileges..."
   - Provides "Restart as Admin" button
3. **Click restart:** UAC prompt appears, app restarts with admin rights
4. **Now works:** Can autotype into protected windows

## Technical Details

### Integrity Level Detection
The code checks Windows integrity levels:
- **Untrusted:** 0x0000
- **Low:** 0x1000
- **Medium:** 0x2000 (normal apps)
- **High:** 0x3000 (admin apps)
- **System:** 0x4000

Protected windows run at High/System level. If target > current, elevation required.

### Why This Approach?
- ✅ No UAC prompt every time you start the app
- ✅ Works immediately for normal windows
- ✅ Only need elevation when actually necessary
- ✅ User is informed WHY elevation is needed
- ✅ One-click solution when needed

## Future Enhancements
If you want even smoother UX, consider:
- Remember "always run as admin" preference
- Auto-restart on first detection (with user consent)
- Task Scheduler setup for startup if running elevated frequently
