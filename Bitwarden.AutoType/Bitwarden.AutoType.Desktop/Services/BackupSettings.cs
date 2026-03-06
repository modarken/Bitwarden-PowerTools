using System;
using System.Text.Json.Serialization;
using Bitwarden.Utilities;

namespace Bitwarden.AutoType.Desktop.Services;

/// <summary>
/// Configuration settings for backup functionality.
/// </summary>
public class BackupSettings
{
    /// <summary>
    /// The configured backup folder path. If null or empty, uses the default location.
    /// </summary>
    public string? ConfiguredBackupFolder { get; set; }

    /// <summary>
    /// Whether scheduled backups are enabled.
    /// </summary>
    public bool ScheduledBackupEnabled { get; set; } = false;

    /// <summary>
    /// Cron expression for scheduled backups.
    /// Examples:
    /// - "0 0 * * *" = Daily at midnight
    /// - "0 0 * * 0" = Weekly on Sunday at midnight
    /// - "0 0 1 * *" = Monthly on the 1st at midnight
    /// - "0 */6 * * *" = Every 6 hours
    /// </summary>
    public string CronSchedule { get; set; } = "0 0 * * *"; // Default: daily at midnight

    /// <summary>
    /// Password for scheduled backups. If null, scheduled backups won't run.
    /// This is stored encrypted using DPAPI.
    /// </summary>
    [JsonConverter(typeof(ProtectedDataConverter))]
    public string? ScheduledBackupPassword { get; set; }

    /// <summary>
    /// Number of backups to keep. Older backups are automatically deleted.
    /// Set to 0 to disable automatic deletion.
    /// </summary>
    public int RetentionCount { get; set; } = 10;

    /// <summary>
    /// Number of days to keep backups. Backups older than this are deleted.
    /// Set to 0 to disable age-based deletion (use count only).
    /// </summary>
    public int RetentionDays { get; set; } = 30;

    /// <summary>
    /// Whether to show a notification when a scheduled backup completes.
    /// </summary>
    public bool ShowBackupNotifications { get; set; } = true;

    /// <summary>
    /// The UTC time of the last successful backup.
    /// </summary>
    public DateTime? LastBackupTime { get; set; }

    /// <summary>
    /// Gets the effective backup folder, falling back to default if not configured.
    /// </summary>
    public string GetEffectiveBackupFolder()
    {
        if (!string.IsNullOrWhiteSpace(ConfiguredBackupFolder))
        {
            return ConfiguredBackupFolder;
        }

        return System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Bitwarden.AutoType",
            "Backups");
    }

    /// <summary>
    /// Validates the settings.
    /// </summary>
    public bool IsScheduledBackupConfigured()
    {
        return ScheduledBackupEnabled 
            && !string.IsNullOrWhiteSpace(ScheduledBackupPassword)
            && !string.IsNullOrWhiteSpace(CronSchedule);
    }
}

/// <summary>
/// Common cron schedule presets.
/// </summary>
public static class CronPresets
{
    public const string EveryHour = "0 * * * *";
    public const string Every6Hours = "0 */6 * * *";
    public const string Every12Hours = "0 */12 * * *";
    public const string DailyAtMidnight = "0 0 * * *";
    public const string DailyAt6AM = "0 6 * * *";
    public const string DailyAtNoon = "0 12 * * *";
    public const string WeeklySunday = "0 0 * * 0";
    public const string WeeklyMonday = "0 0 * * 1";
    public const string MonthlyFirst = "0 0 1 * *";

    public static string GetDescription(string cronExpression)
    {
        return cronExpression switch
        {
            EveryHour => "Every hour",
            Every6Hours => "Every 6 hours",
            Every12Hours => "Every 12 hours",
            DailyAtMidnight => "Daily at midnight",
            DailyAt6AM => "Daily at 6:00 AM",
            DailyAtNoon => "Daily at noon",
            WeeklySunday => "Weekly on Sunday",
            WeeklyMonday => "Weekly on Monday",
            MonthlyFirst => "Monthly on the 1st",
            _ => $"Custom: {cronExpression}"
        };
    }
}
