using System;
using System.Globalization;

namespace Bitwarden.AutoType.Desktop.Services;

public static class TrayStatusFormatter
{
    public static string GetStatusText(bool isAutoTypeEnabled)
    {
        return $"Status: {(isAutoTypeEnabled ? "Enabled" : "Disabled")}";
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

    public static string GetNotifyIconText(bool isAutoTypeEnabled)
    {
        return isAutoTypeEnabled
            ? "Bitwarden AutoType - Enabled"
            : "Bitwarden AutoType - Disabled";
    }

    private static string FormatTimestamp(DateTime? localTimestamp, IFormatProvider? formatProvider)
    {
        return localTimestamp.HasValue
            ? localTimestamp.Value.ToString("g", formatProvider ?? CultureInfo.CurrentCulture)
            : "Never";
    }
}