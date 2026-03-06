using System;
using System.Globalization;

namespace Bitwarden.AutoType.Desktop.Services;

public enum TrayVisualState
{
    Enabled,
    Disabled,
    Warning,
    Error,
}

public static class TrayStatusFormatter
{
    public static TrayVisualState GetVisualState(bool isAutoTypeEnabled, bool isConfigured, string? lastIssueSummary)
    {
        if (!string.IsNullOrWhiteSpace(lastIssueSummary))
        {
            return TrayVisualState.Error;
        }

        if (!isConfigured)
        {
            return TrayVisualState.Warning;
        }

        return isAutoTypeEnabled ? TrayVisualState.Enabled : TrayVisualState.Disabled;
    }

    public static string GetStatusText(TrayVisualState visualState)
    {
        return visualState switch
        {
            TrayVisualState.Enabled => "Status: Enabled",
            TrayVisualState.Disabled => "Status: Disabled",
            TrayVisualState.Warning => "Status: Setup required",
            TrayVisualState.Error => "Status: Error",
            _ => "Status: Unknown",
        };
    }

    public static string GetSyncText(DateTimeOffset? lastSyncTimeUtc, IFormatProvider? formatProvider = null)
    {
        return $"Last sync: {FormatTimestamp(lastSyncTimeUtc?.LocalDateTime, formatProvider)}";
    }

    public static string GetBackupText(DateTime? lastBackupTimeUtc, IFormatProvider? formatProvider = null)
    {
        return $"Last backup: {FormatTimestamp(lastBackupTimeUtc?.ToLocalTime(), formatProvider)}";
    }

    public static string GetIssueText(string? lastIssueSummary)
    {
        return string.IsNullOrWhiteSpace(lastIssueSummary)
            ? "Last issue: None"
            : $"Last issue: {lastIssueSummary}";
    }

    public static string GetToggleMenuText(bool isAutoTypeEnabled)
    {
        return isAutoTypeEnabled ? "Disable Auto-Type" : "Enable Auto-Type";
    }

    public static string GetNotifyIconText(TrayVisualState visualState)
    {
        return visualState switch
        {
            TrayVisualState.Enabled => "Bitwarden AutoType - Enabled",
            TrayVisualState.Disabled => "Bitwarden AutoType - Disabled",
            TrayVisualState.Warning => "Bitwarden AutoType - Setup Required",
            TrayVisualState.Error => "Bitwarden AutoType - Error",
            _ => "Bitwarden AutoType",
        };
    }

    private static string FormatTimestamp(DateTime? localTimestamp, IFormatProvider? formatProvider)
    {
        return localTimestamp.HasValue
            ? localTimestamp.Value.ToString("g", formatProvider ?? CultureInfo.CurrentCulture)
            : "Never";
    }
}