using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Bitwarden.AutoType.Desktop.Windows;

namespace Bitwarden.AutoType.Desktop;

public sealed record AutoTypeSequenceValidationResult(bool IsValid, IReadOnlyList<string> Warnings)
{
    public bool HasWarnings => Warnings.Count > 0;

    public string Summary => !IsValid
        ? "Enter a keyboard sequence."
        : Warnings.Count switch
        {
            0 => "Sequence looks valid.",
            1 => Warnings[0],
            _ => $"Sequence looks valid, but {Warnings.Count} token(s) will be typed literally or ignored at runtime."
        };
}

public sealed record AutoTypeRuleTestResult(bool CanTest, bool IsMatch, string Message);

public static class AutoTypeAuthoringValidator
{
    private static readonly Regex TokenRegex = new(@"{.*?}", RegexOptions.Compiled);

    private static readonly HashSet<string> BitwardenPlaceholders = new(StringComparer.OrdinalIgnoreCase)
    {
        "NAME",
        "USERNAME",
        "PASSWORD",
        "URL",
        "NOTES",
        "TOTP"
    };

    private static readonly HashSet<string> SpecialKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "LEFTCURLYBRACE",
        "RIGHTCURLYBRACE",
        "SHIFT",
        "RIGHTSHIFT",
        "LEFTSHIFT",
        "ALT",
        "LEFTALT",
        "RIGHTALT",
        "CONTROL",
        "LEFTCONTROL",
        "RIGHTCONTROL",
        "TAB",
        "LEFTWINDOWS",
        "RIGHTWINDOWS",
        "ENTER",
        "BACK",
        "SPACE",
        "LEFT",
        "DOWN",
        "RIGHT",
        "UP",
        "INSERT",
        "DELETE",
        "HOME",
        "END",
        "PGUP",
        "PGDOWN",
        "CAPSLOCK",
        "ESCAPE",
        "NUMLOCK",
        "PRINTSCREEN",
        "SCROLLLOCK",
        "F1",
        "F2",
        "F3",
        "F4",
        "F5",
        "F6",
        "F7",
        "F8",
        "F9",
        "F10",
        "F11",
        "F12"
    };

    public static string? ValidateRule(AutoTypeCustomField field)
    {
        var hasCombinedInclude = !string.IsNullOrWhiteSpace(field.Title)
            || !string.IsNullOrWhiteSpace(field.Process)
            || !string.IsNullOrWhiteSpace(field.Class);

        var hasLegacyTarget = !string.IsNullOrWhiteSpace(field.Target);

        if (!hasCombinedInclude && !hasLegacyTarget)
        {
            return "Add at least one Include field so the rule can match a window.";
        }

        return null;
    }

    public static string? ValidateRegexPattern(string? pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return null;
        }

        try
        {
            _ = new Regex(pattern, RegexOptions.IgnoreCase);
            return null;
        }
        catch (ArgumentException ex)
        {
            return ex.Message;
        }
    }

    public static AutoTypeSequenceValidationResult ValidateSequence(string? sequence)
    {
        if (string.IsNullOrWhiteSpace(sequence))
        {
            return new AutoTypeSequenceValidationResult(false, []);
        }

        var warnings = TokenRegex
            .Matches(sequence)
            .Select(match => ValidateToken(match.Value))
            .Where(message => !string.IsNullOrWhiteSpace(message))
            .Cast<string>()
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return new AutoTypeSequenceValidationResult(true, warnings);
    }

    public static AutoTypeRuleTestResult TestDetectedWindow(AutoTypeCustomField field, string? windowTitle, string? processName, string? windowClassName)
    {
        var ruleError = ValidateRule(field);
        if (ruleError is not null)
        {
            return new AutoTypeRuleTestResult(false, false, ruleError);
        }

        if (string.IsNullOrWhiteSpace(windowTitle)
            && string.IsNullOrWhiteSpace(processName)
            && string.IsNullOrWhiteSpace(windowClassName))
        {
            return new AutoTypeRuleTestResult(false, false, "Drag the finder over a window to capture a target before testing.");
        }

        if (!AutoTypeRuleMatcher.TryCreateMatcher(field, out var matcher, out var error))
        {
            return new AutoTypeRuleTestResult(false, false, error ?? "Unable to validate this rule.");
        }

        var title = windowTitle ?? string.Empty;
        var process = processName ?? string.Empty;
        var windowClass = windowClassName ?? string.Empty;
        var isMatch = AutoTypeRuleMatcher.IsMatch(field, matcher!, title, process, windowClass);

        return new AutoTypeRuleTestResult(true, isMatch, isMatch
            ? "Detected window matches this rule."
            : BuildFailureMessage(field, matcher!, title, process, windowClass));
    }

    private static string? ValidateToken(string token)
    {
        if (token.Length < 2)
        {
            return null;
        }

        var inner = token[1..^1];
        if (string.IsNullOrWhiteSpace(inner))
        {
            return null;
        }

        if (Int32.TryParse(inner, out _))
        {
            return null;
        }

        if (IsBitwardenPlaceholder(inner) || IsFieldPlaceholder(inner) || IsSpecialKeystroke(inner))
        {
            return null;
        }

        return $"Unknown token {token}; it will be typed literally at runtime.";
    }

    private static bool IsBitwardenPlaceholder(string token)
    {
        return BitwardenPlaceholders.Contains(token);
    }

    private static bool IsFieldPlaceholder(string token)
    {
        var separatorIndex = token.IndexOf(':');
        if (separatorIndex < 0)
        {
            return false;
        }

        var prefix = token[..separatorIndex];
        var fieldName = token[(separatorIndex + 1)..];

        return (prefix.Equals("FIELD", StringComparison.OrdinalIgnoreCase)
                || prefix.Equals("CUSTOM", StringComparison.OrdinalIgnoreCase))
            && !string.IsNullOrWhiteSpace(fieldName);
    }

    private static bool IsSpecialKeystroke(string token)
    {
        var split = token.Split(':', 2, StringSplitOptions.None);
        var keyword = split[0];

        var isRecognizedKeyword = SpecialKeywords.Contains(keyword)
            || (keyword.StartsWith("VK", StringComparison.OrdinalIgnoreCase)
                && Byte.TryParse(keyword[2..], out _));

        if (!isRecognizedKeyword)
        {
            return false;
        }

        if (split.Length == 1)
        {
            return true;
        }

        var modifier = split[1];
        return Int32.TryParse(modifier, out _)
            || modifier.Equals(nameof(EmulatedKeystrokeTypes.Down), StringComparison.OrdinalIgnoreCase)
            || modifier.Equals(nameof(EmulatedKeystrokeTypes.Up), StringComparison.OrdinalIgnoreCase)
            || modifier.Equals(nameof(EmulatedKeystrokeTypes.Press), StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildFailureMessage(AutoTypeCustomField field, CompiledTargetMatcher matcher, string title, string process, string windowClass)
    {
        var details = new List<string>();

        if (!string.IsNullOrWhiteSpace(field.Title))
        {
            details.Add($"Title {(matcher.Title!.IsMatch(title) ? "matched" : "did not match")}");
        }

        if (!string.IsNullOrWhiteSpace(field.Process))
        {
            details.Add($"Process {(matcher.Process!.IsMatch(process) ? "matched" : "did not match")}");
        }

        if (!string.IsNullOrWhiteSpace(field.Class))
        {
            details.Add($"Class {(matcher.Class!.IsMatch(windowClass) ? "matched" : "did not match")}");
        }

        if (!string.IsNullOrWhiteSpace(field.ExcludeTitle) && matcher.ExcludeTitle!.IsMatch(title))
        {
            details.Add("Exclude Title blocked the match");
        }

        if (!string.IsNullOrWhiteSpace(field.ExcludeProcess) && matcher.ExcludeProcess!.IsMatch(process))
        {
            details.Add("Exclude Process blocked the match");
        }

        if (!string.IsNullOrWhiteSpace(field.ExcludeClass) && matcher.ExcludeClass!.IsMatch(windowClass))
        {
            details.Add("Exclude Class blocked the match");
        }

        if (!string.IsNullOrWhiteSpace(field.Target) && matcher.LegacyTarget is not null)
        {
            var legacyTarget = field.Type switch
            {
                TargetTypes.Process => process,
                TargetTypes.Class => windowClass,
                _ => title
            };

            details.Add($"Legacy target {(matcher.LegacyTarget.IsMatch(legacyTarget) ? "matched" : "did not match")}");
        }

        return details.Count == 0
            ? "Detected window did not match this rule."
            : $"Detected window did not match this rule. {string.Join("; ", details)}.";
    }
}