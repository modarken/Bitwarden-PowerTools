using System;
using System.Globalization;
using Bitwarden.AutoType.Desktop.Services;
using Xunit;

namespace Bitwarden.AutoType.Desktop.Tests;

public class TrayStatusFormatterTests
{
    [Fact]
    public void GetVisualStatePrioritizesErrorOverOtherStates()
    {
        Assert.Equal(TrayVisualState.Error, TrayStatusFormatter.GetVisualState(true, true, "Sync failed"));
    }

    [Fact]
    public void GetVisualStateReturnsWarningWhenSetupIsIncomplete()
    {
        Assert.Equal(TrayVisualState.Warning, TrayStatusFormatter.GetVisualState(true, false, null));
    }

    [Fact]
    public void GetStatusTextReflectsVisualState()
    {
        Assert.Equal("Status: Enabled", TrayStatusFormatter.GetStatusText(TrayVisualState.Enabled));
        Assert.Equal("Status: Disabled", TrayStatusFormatter.GetStatusText(TrayVisualState.Disabled));
        Assert.Equal("Status: Setup required", TrayStatusFormatter.GetStatusText(TrayVisualState.Warning));
        Assert.Equal("Status: Error", TrayStatusFormatter.GetStatusText(TrayVisualState.Error));
    }

    [Fact]
    public void GetSyncTextReturnsNeverWhenNoSyncHasBeenRecorded()
    {
        Assert.Equal("Last sync: Never", TrayStatusFormatter.GetSyncText(null, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void GetBackupTextFormatsTimestamp()
    {
        var timestamp = new DateTime(2026, 3, 6, 14, 5, 0, DateTimeKind.Local).ToUniversalTime();

        Assert.Equal("Last backup: 03/06/2026 14:05", TrayStatusFormatter.GetBackupText(timestamp, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void GetIssueTextShowsFriendlyFallback()
    {
        Assert.Equal("Last issue: None", TrayStatusFormatter.GetIssueText(null));
        Assert.Equal("Last issue: Sync failed", TrayStatusFormatter.GetIssueText("Sync failed"));
    }

    [Fact]
    public void GetToggleMenuTextMatchesState()
    {
        Assert.Equal("Disable Auto-Type", TrayStatusFormatter.GetToggleMenuText(true));
        Assert.Equal("Enable Auto-Type", TrayStatusFormatter.GetToggleMenuText(false));
    }

    [Fact]
    public void GetNotifyIconTextReflectsVisualState()
    {
        Assert.Equal("Bitwarden AutoType - Enabled", TrayStatusFormatter.GetNotifyIconText(TrayVisualState.Enabled));
        Assert.Equal("Bitwarden AutoType - Setup Required", TrayStatusFormatter.GetNotifyIconText(TrayVisualState.Warning));
    }
}