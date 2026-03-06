# Combined Target Matching Design

This document defines the proposed schema and implementation plan for the combined target matching feature recommended in [Feature-Recommendations.md](Feature-Recommendations.md).

## Goal

Allow a single AutoType rule to match against multiple window attributes at the same time, with optional exclusions, while preserving compatibility with the current `Target` plus `Type` format.

## Current State

Today an AutoType rule is represented by `AutoTypeCustomField` in `AutoTypeViewModel.cs`:

```json
{
  "Target": "^.*Gmail.*$",
  "Type": "Title",
  "Sequence": "{USERNAME}{TAB}{PASSWORD}{ENTER}"
}
```

Current behavior:

- A rule matches against exactly one property: title, process, or class.
- A compiled regex is cached per rule.
- If `Type` is missing, the code defaults to matching `Target` against the window title.
- `TargetFinderSequenceComposerControl` exposes a single target type selector plus a single regex field.

This works, but it cannot express the more specific cases that users need, such as:

- Match title and process together.
- Match class and process together.
- Match a browser tab title but exclude private or incognito windows.
- Match a process family but exclude one app shell or launcher window.

## Proposed Schema

Keep the existing properties for backward compatibility, and add a richer root-level schema for combined matching.

### Proposed JSON Shape

```json
{
  "Title": "^.*Gmail.*$",
  "Process": "^chrome$",
  "Class": "^Chrome_WidgetWin_1$",
  "ExcludeTitle": ".*Incognito.*",
  "Sequence": "{USERNAME}{TAB}{PASSWORD}{ENTER}"
}
```

### Proposed C# Model

```csharp
public class AutoTypeCustomField
{
    [JsonIgnore]
    public string? Name { get; set; }

    [JsonIgnore]
    public string? UserName { get; set; }

    public string? Target { get; set; }
    public TargetTypes? Type { get; set; }

    public string? Title { get; set; }
    public string? Process { get; set; }
    public string? Class { get; set; }

    public string? ExcludeTitle { get; set; }
    public string? ExcludeProcess { get; set; }
    public string? ExcludeClass { get; set; }

    public string? Sequence { get; set; }
}
```

## Matching Semantics

### Include Rules

- `Title`, `Process`, and `Class` are all optional.
- If one or more of them are provided, every provided include rule must match.
- If none of them are provided, the rule falls back to legacy `Target` plus `Type` behavior.

Examples:

- `Title` only: match by title.
- `Process` and `Class`: both must match.
- `Title`, `Process`, and `Class`: all three must match.

### Exclude Rules

- `ExcludeTitle`, `ExcludeProcess`, and `ExcludeClass` are all optional.
- If any exclude rule matches, the entire rule must fail.
- Excludes are evaluated after include rules.

Examples:

- `Title=Gmail` and `ExcludeTitle=Incognito`: match Gmail windows except incognito.
- `Process=chrome` and `ExcludeClass=something`: match Chrome except a specific host window.

### Legacy Fallback

Legacy rules remain valid:

```json
{
  "Target": "^.*Login.*$",
  "Type": "Title",
  "Sequence": "{USERNAME}{TAB}{PASSWORD}{ENTER}"
}
```

Legacy evaluation rules:

- If `Target` exists and no new include fields exist, evaluate using `Target` plus `Type`.
- If `Type` is null, treat legacy `Target` as a title rule, matching the current behavior.

### Precedence Rule

Use the following order:

1. If any of `Title`, `Process`, or `Class` are populated, use the new combined-rule engine.
2. Otherwise, use legacy `Target` plus `Type`.

This keeps migration simple and avoids ambiguous rule interpretation.

## Recommended Matching Engine Change

The current cache shape is built around one compiled regex:

```csharp
public record CachedAutoTypeEntry(AutoTypeCustomField Field, Regex CompiledRegex, Cipher Cipher);
```

That should be replaced with a compiled matcher object.

### Proposed Compiled Matcher

```csharp
public record CompiledTargetMatcher(
    Regex? Title,
    Regex? Process,
    Regex? Class,
    Regex? ExcludeTitle,
    Regex? ExcludeProcess,
    Regex? ExcludeClass,
    Regex? LegacyTarget,
    TargetTypes? LegacyType);

public record CachedAutoTypeEntry(AutoTypeCustomField Field, CompiledTargetMatcher Matcher, Cipher Cipher);
```

### Evaluation Algorithm

Given the active window information:

- `windowTitle`
- `processName`
- `windowClassName`

Evaluate in this order:

1. Determine whether the field is a new-format rule by checking `Title`, `Process`, and `Class`.
2. If new format:
   - Every populated include regex must match its corresponding window property.
   - No populated exclude regex may match its corresponding property.
3. If legacy format:
   - Evaluate `Target` against the property selected by `Type`.
   - If `Type` is null, evaluate against the title.
4. Return true only if the selected path succeeds.

### Error Handling

During cache construction:

- Invalid regexes should not crash the whole app.
- Log the failure with enough context to identify the cipher.
- Skip the invalid rule and continue loading the rest of the rules.

That is better than the current behavior, where a bad regex can cause broader failure during initialization.

## Migration Strategy

No data migration should be required in Bitwarden.

Support both schemas indefinitely:

- Existing entries continue working unchanged.
- New entries can use the richer schema.
- The composer should be able to generate the new schema while still recognizing the old one.

Optional later enhancement:

- Add a one-click “upgrade legacy rule” action in the composer to transform `Target` plus `Type` into one of `Title`, `Process`, or `Class`.

## UI Design Impact

The main authoring surface is `TargetFinderSequenceComposerControl`.

### Current UI

- Shows process title, process name, and class name.
- Lets the user choose one target type.
- Lets the user edit one target regex.
- Generates a single custom JSON payload.

### Proposed UI Direction

Replace the single target selector with explicit include and exclude sections.

#### Include Section

- `Title Regex`
- `Process Regex`
- `Class Regex`

#### Exclude Section

- `Exclude Title Regex`
- `Exclude Process Regex`
- `Exclude Class Regex`

#### Helper Actions

- “Use exact title” button.
- “Use exact process” button.
- “Use exact class” button.
- “Clear field” button for each generated regex.

This is easier to understand than forcing the user through a single type dropdown when the new feature is explicitly about combining criteria.

### Backward-Compatible Composer Behavior

The composer does not need to fully parse existing Bitwarden entry data in the first implementation. It can initially remain a generator-only tool.

For the first version:

- Generate only the new schema.
- Keep the help text updated with both old and new examples.
- Preserve old runtime compatibility in `AutoTypeViewModel`.

## Example Rules

### Browser Login With Process Constraint

```json
{
  "Title": "^.*Gmail.*$",
  "Process": "^chrome$",
  "Sequence": "{USERNAME}{TAB}{PASSWORD}{ENTER}"
}
```

### Browser Login Excluding Incognito

```json
{
  "Title": "^.*Gmail.*$",
  "Process": "^chrome$",
  "ExcludeTitle": ".*Incognito.*",
  "Sequence": "{USERNAME}{TAB}{PASSWORD}{ENTER}"
}
```

### Desktop App With Title, Process, And Class

```json
{
  "Title": "^Sign in$",
  "Process": "^MyApp$",
  "Class": "^HwndWrapper\[MyApp;.*\]$",
  "Sequence": "{USERNAME}{TAB}{PASSWORD}{ENTER}"
}
```

### Legacy Rule Still Supported

```json
{
  "Target": "^.*notepad.*$",
  "Type": "Title",
  "Sequence": "{USERNAME}{TAB}{PASSWORD}{ENTER}"
}
```

## Suggested Implementation Steps

### Step 1: Extend The Data Model

Update `AutoTypeCustomField` to include:

- `Title`
- `Process`
- `Class`
- `ExcludeTitle`
- `ExcludeProcess`
- `ExcludeClass`

Keep `Target` and `Type` untouched.

### Step 2: Replace The Single-Regex Cache

In `AutoTypeViewModel`:

- Replace `CompiledRegex` with a compiled matcher object.
- Add helper methods to compile optional regex fields.
- Skip invalid rules individually instead of failing the entire refresh.

### Step 3: Centralize Match Evaluation

Add a helper such as:

```csharp
private static bool IsMatch(CachedAutoTypeEntry entry, string windowTitle, string processName, string windowClassName)
```

This should encapsulate new-format and legacy-format behavior so the hotkey handler remains simple.

### Step 4: Update The Composer UI

In `TargetFinderSequenceComposerControl.xaml` and `.xaml.cs`:

- Remove the single type dropdown.
- Add the new include and exclude text boxes.
- Update serialization to emit the new schema.
- Add quick-fill buttons based on the discovered current window.

### Step 5: Update Help Text

In `Constants.cs`:

- Update the “How to add AutoType sequence to Bitwarden” help text.
- Add new JSON examples showing combined matching and exclusions.

### Step 6: Add Regression Coverage

Add tests for:

- Legacy title matching.
- Legacy null-type fallback to title.
- Combined include matching.
- Exclude matching.
- Mixed include and exclude combinations.
- Invalid regex isolation.

## Files Expected To Change In Implementation

Primary files:

- `Bitwarden.AutoType/Bitwarden.AutoType.Desktop/ViewModels/AutoTypeViewModel.cs`
- `Bitwarden.AutoType/Bitwarden.AutoType.Desktop/Views/TargetFinderSequenceComposerControl.xaml`
- `Bitwarden.AutoType/Bitwarden.AutoType.Desktop/Views/TargetFinderSequenceComposerControl.xaml.cs`
- `Bitwarden.AutoType/Bitwarden.AutoType.Desktop/Constants.cs`

Possible supporting files:

- `Bitwarden.AutoType/Bitwarden.AutoType.Desktop/Helpers/EnumHelper.cs`
- Any test project added or expanded for matching logic coverage

## Recommendation

Implement this in two phases:

1. Runtime support first.
2. Composer UI second.

Reasoning:

- Runtime compatibility is the security-sensitive part and should be stabilized first.
- Once runtime support exists, the UI can safely generate the new schema.
- This reduces the chance of generating payloads the runtime cannot yet interpret.

## Outcome

This design preserves all existing user data, supports more precise matching, and gives a clean path to a better authoring experience without forcing a disruptive migration.