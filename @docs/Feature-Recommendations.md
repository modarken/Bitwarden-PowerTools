# Feature Recommendations

This document captures feature ideas identified from a review of the current Bitwarden PowerTools codebase, with emphasis on `Bitwarden.AutoType.Desktop`.

## Review Basis

The recommendations below are based on the current product shape and implementation in:

- `README.md`
- `@docs/ToDo.txt`
- `Bitwarden.AutoType/Bitwarden.AutoType.Desktop/ViewModels/AutoTypeViewModel.cs`
- `Bitwarden.AutoType/Bitwarden.AutoType.Desktop/Services/BitwardenService.cs`
- `Bitwarden.AutoType/Bitwarden.AutoType.Desktop/Services/NotifyIconService.cs`
- `Bitwarden.AutoType/Bitwarden.AutoType.Desktop/Services/BackupService.cs`
- `Bitwarden.AutoType/Bitwarden.AutoType.Desktop/Views/MatchSelectionWindow.xaml.cs`

The app already has strong core functionality: auto-type, regex-based targeting, TOTP support, encrypted backups, tray integration, auto-updates, and elevation awareness. The best next features are the ones that reduce manual setup, improve matching accuracy, and make execution safer.

## Recommended Priority Order

1. Advanced target matching with combined rules and exclusions
2. Visual sequence builder with templates
3. MRU and favorites for ambiguous matches
4. Safer execution modes and fallback options
5. Richer tray controls and state feedback
6. Backup verification and restore workflow
7. Regex and sequence validation before save
8. Start on login and startup posture controls

## Feature Details

## 1. Advanced Target Matching With Combined Rules And Exclusions

Priority: High

Why it is valuable:

- The current model matches by a single `Target` plus `Type`, which is flexible but still too coarse for many real-world cases.
- Users often need to distinguish between multiple similar windows from the same browser or application.
- This is already partially anticipated in `@docs/ToDo.txt`.

What to add:

- Support matching by title, process, and class at the same time.
- Support exclusion rules such as `ExcludeTitle`, `ExcludeProcess`, or `ExcludeClass`.
- Allow “all conditions must match” behavior to reduce accidental fills.

Why it fits the codebase:

- `AutoTypeViewModel` already centralizes target matching.
- The `TargetTypes` enum and cached regex model are a natural starting point.
- The match selection flow already exists, so the change can be layered onto current behavior.

## 2. Visual Sequence Builder With Templates

Priority: High

Why it is valuable:

- Today the user must author JSON inside a Bitwarden custom field by hand.
- That is powerful, but it makes onboarding harder than it needs to be.
- A visual builder would make the feature accessible without removing the advanced path.

What to add:

- A UI for composing target rules and keystroke sequences.
- Built-in templates for common flows such as browser login, username plus password plus TOTP, and delayed login forms.
- A preview of the generated JSON that will be written to the custom field.

Why it fits the codebase:

- The app already parses structured custom field content.
- The keystroke system is already expressive enough to support a builder.
- This would directly reduce setup friction for the core feature.

## 3. MRU And Favorites For Ambiguous Matches

Priority: High

Why it is valuable:

- The current multiple-match dialog works, but it slows down repeat workflows.
- In practice, users will often resolve the same conflict repeatedly for the same foreground window context.

What to add:

- Remember the last selected match for a specific window signature.
- Add favorites or pinned preferred matches.
- Optionally auto-select the remembered match after a short confidence threshold.

Why it fits the codebase:

- `MatchSelectionWindow` already handles the conflict case.
- `AutoTypeViewModel` has the decision point needed to consult a history cache before showing the chooser.

## 4. Safer Execution Modes And Fallback Options

Priority: Medium

Why it is valuable:

- Keystroke injection is inherently sensitive to focus, timing, remote sessions, and application behavior.
- Some target environments block or partially interfere with synthetic input.

What to add:

- A dry-run or preview mode showing which entry and sequence will be used.
- A clipboard fallback mode that copies the next secret and clears the clipboard after a timeout.
- Optional confirmation for high-risk entries.

Why it fits the codebase:

- The current execution flow already resolves a specific match before typing.
- Elevation detection shows the app already accounts for execution constraints.

## 5. Richer Tray Controls And State Feedback

Priority: Medium

Why it is valuable:

- The tray icon is already the operational center of the app.
- Users should be able to see health and trigger common actions without opening the main window.

What to add:

- Quick actions for enable or disable, sync now, and open recent backup folder.
- Surface last sync time, last backup time, and current update state.
- Use different tray icon colors or overlays for enabled, disabled, warning, and error states.

Why it fits the codebase:

- `NotifyIconService` already owns the tray menu and notifications.
- `@docs/ToDo.txt` already mentions state-based icon changes.

## 6. Backup Verification And Restore Workflow

Priority: Medium

Why it is valuable:

- Backup creation is already implemented well, but restore confidence depends on verification and discoverable recovery steps.
- Users trust backup features more when they can verify and inspect them from the UI.

What to add:

- A guided restore workflow.
- Backup integrity verification before restore.
- Metadata display such as creation time, size, and backup target folder.

Why it fits the codebase:

- `BackupService` already supports encrypted backup operations.
- The tray menu already exposes backup actions, so restore and verify can live in the same surface.

## 7. Regex And Sequence Validation Before Save

Priority: Medium

Why it is valuable:

- Matching and typing are driven by user-authored patterns.
- Validation would prevent the most common failure modes before they reach runtime.

What to add:

- Regex syntax validation.
- Basic regex safety and performance checks.
- Sequence token validation with helpful error messages.
- A “test against current window” action.

Why it fits the codebase:

- The app already compiles regexes up front.
- The sequence system already uses a formal placeholder model.

## 8. Start On Login And Startup Posture Controls

Priority: Medium

Why it is valuable:

- This is a tray utility whose usefulness depends on being available when the user needs it.
- Startup behavior should be explicit and configurable.

What to add:

- Start with Windows.
- Start minimized to tray.
- Optional delayed startup.
- A visible startup health indicator if registration fails.

Why it fits the codebase:

- The existing backlog already calls this out.
- The application is already structured as a background tray-first desktop app.

## Lower-Priority Ideas

These are still worth considering, but they should follow the core workflow improvements above.

- Encrypted local audit trail for auto-type events.
- Adaptive typing speed presets for slow or remote targets.
- Per-entry hotkeys for a small set of favorite items.
- Optional sync of app settings between devices.

## Recommended Next Step

If only one feature is taken forward next, it should be advanced target matching with combined rules and exclusions.

Reasoning:

- It improves the most important product behavior: choosing the right entry for the right window.
- It reduces the chance of typing secrets into the wrong target.
- It builds directly on existing code rather than requiring a new subsystem.
- It is already partially represented in the backlog, which lowers decision risk.

Suggested implementation order:

1. Expand the custom field schema to support multi-property match criteria.
2. Update matching logic in `AutoTypeViewModel` to evaluate all provided criteria.
3. Update any target-selection UI to display and edit the richer rule set.
4. Add validation and migration support for the older `Target` plus `Type` schema.