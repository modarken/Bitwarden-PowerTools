using Bitwarden.AutoType.Desktop;
using Xunit;

namespace Bitwarden.AutoType.Desktop.Tests;

public class AutoTypeRuleMatcherTests
{
    [Fact]
    public void LegacyTitleRuleMatchesWindowTitle()
    {
        var field = new AutoTypeCustomField
        {
            Target = "^.*Gmail.*$",
            Type = TargetTypes.Title,
            Sequence = "{USERNAME}"
        };

        var matcher = CreateMatcher(field);

        Assert.True(AutoTypeRuleMatcher.IsMatch(field, matcher, "Gmail - Chrome", "chrome", "Chrome_WidgetWin_1"));
    }

    [Fact]
    public void LegacyProcessRuleMatchesProcessName()
    {
        var field = new AutoTypeCustomField
        {
            Target = "^Code$",
            Type = TargetTypes.Process,
            Sequence = "{USERNAME}"
        };

        var matcher = CreateMatcher(field);

        Assert.True(AutoTypeRuleMatcher.IsMatch(field, matcher, "Anything", "Code", "Chrome_WidgetWin_1"));
    }

    [Fact]
    public void LegacyNullTypeDefaultsToTitleMatching()
    {
        var field = new AutoTypeCustomField
        {
            Target = "^Sign in$",
            Type = null,
            Sequence = "{USERNAME}"
        };

        var matcher = CreateMatcher(field);

        Assert.True(AutoTypeRuleMatcher.IsMatch(field, matcher, "Sign in", "MyApp", "SomeClass"));
        Assert.False(AutoTypeRuleMatcher.IsMatch(field, matcher, "Other", "Sign in", "SomeClass"));
    }

    [Fact]
    public void CombinedRulesRequireAllIncludedFieldsToMatch()
    {
        var field = new AutoTypeCustomField
        {
            Title = "^Chrome\\ Legacy\\ Window$",
            Process = "^Code$",
            Class = "^Chrome_RenderWidgetHostHWND$",
            Sequence = "{USERNAME}"
        };

        var matcher = CreateMatcher(field);

        Assert.True(AutoTypeRuleMatcher.IsMatch(field, matcher, "Chrome Legacy Window", "Code", "Chrome_RenderWidgetHostHWND"));
        Assert.False(AutoTypeRuleMatcher.IsMatch(field, matcher, "Chrome Legacy Window", "Code", "OtherClass"));
    }

    [Fact]
    public void ExcludeTitleBlocksMatchingRule()
    {
        var field = new AutoTypeCustomField
        {
            Title = "^.*Gmail.*$",
            Process = "^chrome$",
            ExcludeTitle = ".*Incognito.*",
            Sequence = "{USERNAME}"
        };

        var matcher = CreateMatcher(field);

        Assert.False(AutoTypeRuleMatcher.IsMatch(field, matcher, "Gmail - Incognito", "chrome", "Chrome_WidgetWin_1"));
        Assert.True(AutoTypeRuleMatcher.IsMatch(field, matcher, "Gmail - Chrome", "chrome", "Chrome_WidgetWin_1"));
    }

    [Fact]
    public void ExcludeProcessBlocksMatchingRule()
    {
        var field = new AutoTypeCustomField
        {
            Title = "^Sign in$",
            ExcludeProcess = "^launcher$",
            Sequence = "{USERNAME}"
        };

        var matcher = CreateMatcher(field);

        Assert.False(AutoTypeRuleMatcher.IsMatch(field, matcher, "Sign in", "launcher", "AnyClass"));
        Assert.True(AutoTypeRuleMatcher.IsMatch(field, matcher, "Sign in", "myapp", "AnyClass"));
    }

    [Fact]
    public void ExcludeClassBlocksMatchingRule()
    {
        var field = new AutoTypeCustomField
        {
            Process = "^chrome$",
            ExcludeClass = "^BlockedClass$",
            Sequence = "{USERNAME}"
        };

        var matcher = CreateMatcher(field);

        Assert.False(AutoTypeRuleMatcher.IsMatch(field, matcher, "Any", "chrome", "BlockedClass"));
        Assert.True(AutoTypeRuleMatcher.IsMatch(field, matcher, "Any", "chrome", "AllowedClass"));
    }

    [Fact]
    public void InvalidRegexReturnsFalseFromTryCreateMatcher()
    {
        var field = new AutoTypeCustomField
        {
            Title = "(",
            Sequence = "{USERNAME}"
        };

        var result = AutoTypeRuleMatcher.TryCreateMatcher(field, out var matcher, out var error);

        Assert.False(result);
        Assert.Null(matcher);
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    private static CompiledTargetMatcher CreateMatcher(AutoTypeCustomField field)
    {
        var created = AutoTypeRuleMatcher.TryCreateMatcher(field, out var matcher, out var error);
        Assert.True(created, error);
        return matcher!;
    }
}