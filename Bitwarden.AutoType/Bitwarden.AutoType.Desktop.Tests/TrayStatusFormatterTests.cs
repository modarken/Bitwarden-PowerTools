using System;
using System.Globalization;
using Bitwarden.AutoType.Desktop.Services;
using Xunit;

namespace Bitwarden.AutoType.Desktop.Tests;

public class TrayStatusFormatterTests
{
    [Fact]
    public void GetStatusTextReflectsAutoTypeState()
    {
        Assert.Equal("Status: Enabled", TrayStatusFormatter.GetStatusText(true));
        Assert.Equal("Status: Disabled", TrayStatusFormatter.GetStatusText(false));
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
}