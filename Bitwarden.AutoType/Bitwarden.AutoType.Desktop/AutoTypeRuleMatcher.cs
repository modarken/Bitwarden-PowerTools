using System;
using System.Text.RegularExpressions;

namespace Bitwarden.AutoType.Desktop;

public static class AutoTypeRuleMatcher
{
    private const RegexOptions CompiledRegexOptions = RegexOptions.IgnoreCase | RegexOptions.Compiled;

    public static bool TryCreateMatcher(AutoTypeCustomField field, out CompiledTargetMatcher? matcher, out string? error)
    {
        try
        {
            matcher = new CompiledTargetMatcher(
                CompileOptionalRegex(field.Title),
                CompileOptionalRegex(field.Process),
                CompileOptionalRegex(field.Class),
                CompileOptionalRegex(field.ExcludeTitle),
                CompileOptionalRegex(field.ExcludeProcess),
                CompileOptionalRegex(field.ExcludeClass),
                CompileOptionalRegex(field.Target),
                field.Type);

            error = null;
            return true;
        }
        catch (Exception ex)
        {
            matcher = null;
            error = ex.Message;
            return false;
        }
    }

    public static bool IsMatch(CachedAutoTypeEntry entry, string windowTitle, string processName, string windowClassName)
    {
        return IsMatch(entry.Field, entry.Matcher, windowTitle, processName, windowClassName);
    }

    public static bool IsMatch(AutoTypeCustomField field, CompiledTargetMatcher matcher, string windowTitle, string processName, string windowClassName)
    {
        return UsesCombinedRules(field)
            ? IsCombinedMatch(matcher, windowTitle, processName, windowClassName)
            : IsLegacyMatch(matcher, windowTitle, processName, windowClassName);
    }

    public static bool UsesCombinedRules(AutoTypeCustomField field)
    {
        return !string.IsNullOrWhiteSpace(field.Title)
            || !string.IsNullOrWhiteSpace(field.Process)
            || !string.IsNullOrWhiteSpace(field.Class);
    }

    private static Regex? CompileOptionalRegex(string? pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return null;
        }

        return new Regex(pattern, CompiledRegexOptions);
    }

    private static bool IsCombinedMatch(CompiledTargetMatcher matcher, string windowTitle, string processName, string windowClassName)
    {
        if (!MatchesOptional(matcher.Title, windowTitle)
            || !MatchesOptional(matcher.Process, processName)
            || !MatchesOptional(matcher.Class, windowClassName))
        {
            return false;
        }

        return !MatchesWhenPresent(matcher.ExcludeTitle, windowTitle)
            && !MatchesWhenPresent(matcher.ExcludeProcess, processName)
            && !MatchesWhenPresent(matcher.ExcludeClass, windowClassName);
    }

    private static bool IsLegacyMatch(CompiledTargetMatcher matcher, string windowTitle, string processName, string windowClassName)
    {
        if (matcher.LegacyTarget is null)
        {
            return false;
        }

        return matcher.LegacyType switch
        {
            TargetTypes.Process => matcher.LegacyTarget.IsMatch(processName),
            TargetTypes.Class => matcher.LegacyTarget.IsMatch(windowClassName),
            _ => matcher.LegacyTarget.IsMatch(windowTitle)
        };
    }

    private static bool MatchesOptional(Regex? regex, string input)
    {
        return regex is null || regex.IsMatch(input);
    }

    private static bool MatchesWhenPresent(Regex? regex, string input)
    {
        return regex is not null && regex.IsMatch(input);
    }
}