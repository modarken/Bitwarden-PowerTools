using Bitwarden.AutoType.Desktop;
using Xunit;

namespace Bitwarden.AutoType.Desktop.Tests;

public class AutoTypeAuthoringValidatorTests
{
    [Fact]
    public void RuleWithoutIncludeFieldsIsRejected()
    {
        var field = new AutoTypeCustomField
        {
            Sequence = "{USERNAME}"
        };

        var error = AutoTypeAuthoringValidator.ValidateRule(field);

        Assert.Equal("Add at least one Include field so the rule can match a window.", error);
    }

    [Fact]
    public void InvalidRegexReturnsHelpfulError()
    {
        var error = AutoTypeAuthoringValidator.ValidateRegexPattern("(");

        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Fact]
    public void ValidSequenceHasNoWarnings()
    {
        var result = AutoTypeAuthoringValidator.ValidateSequence("{USERNAME}{TAB}{PASSWORD}{ENTER}");

        Assert.True(result.IsValid);
        Assert.False(result.HasWarnings);
        Assert.Equal("Sequence looks valid.", result.Summary);
    }

    [Fact]
    public void UnknownSequenceTokenProducesWarning()
    {
        var result = AutoTypeAuthoringValidator.ValidateSequence("{USERNAME}{PASSWRD}{ENTER}");

        Assert.True(result.IsValid);
        Assert.True(result.HasWarnings);
        Assert.Contains(result.Warnings, message => message.Contains("{PASSWRD}"));
    }

    [Fact]
    public void CustomAliasIsAcceptedForFieldPlaceholder()
    {
        var result = AutoTypeAuthoringValidator.ValidateSequence("{CUSTOM:pin}{TAB}{PASSWORD}");

        Assert.True(result.IsValid);
        Assert.False(result.HasWarnings);
    }

    [Fact]
    public void TestDetectedWindowReportsDetailedFailure()
    {
        var field = new AutoTypeCustomField
        {
            Title = "^Sign in$",
            Process = "^chrome$",
            Sequence = "{USERNAME}"
        };

        var result = AutoTypeAuthoringValidator.TestDetectedWindow(field, "Other", "chrome", "Chrome_WidgetWin_1");

        Assert.True(result.CanTest);
        Assert.False(result.IsMatch);
        Assert.Contains("Title did not match", result.Message);
        Assert.Contains("Process matched", result.Message);
    }
}