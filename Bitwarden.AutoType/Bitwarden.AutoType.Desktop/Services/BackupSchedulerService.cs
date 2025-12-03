using System;
using System.Threading;
using System.Threading.Tasks;
using Bitwarden.AutoType.Desktop.Helpers;
using Cronos;
using Microsoft.Extensions.Logging;

namespace Bitwarden.AutoType.Desktop.Services;

/// <summary>
/// Background service that runs scheduled backups based on cron configuration.
/// </summary>
public class BackupSchedulerService : WPFBackgroundService
{
    private readonly ILogger<BackupSchedulerService> _logger;
    private readonly BackupService _backupService;
    private readonly BackupSettings _backupSettings;
    private readonly Action<BackupSettings> _saveSettings;
    private CronExpression? _cronExpression;
    private DateTime? _nextRunTime;

    public BackupSchedulerService(
        ILogger<BackupSchedulerService> logger,
        BackupService backupService,
        BackupSettings backupSettings,
        Action<BackupSettings> saveSettings)
    {
        _logger = logger;
        _backupService = backupService;
        _backupSettings = backupSettings;
        _saveSettings = saveSettings;
    }

    /// <summary>
    /// Gets the next scheduled backup time.
    /// </summary>
    public DateTime? NextRunTime => _nextRunTime;

    /// <summary>
    /// Gets whether scheduled backups are currently enabled and configured.
    /// </summary>
    public bool IsEnabled => _backupSettings.IsScheduledBackupConfigured();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Backup scheduler service starting...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!_backupSettings.IsScheduledBackupConfigured())
                {
                    _nextRunTime = null;
                    _logger.LogTrace("Scheduled backups not configured, waiting 60 seconds...");
                    await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
                    continue;
                }

                // Parse cron expression
                if (!TryParseCronExpression())
                {
                    _logger.LogWarning("Invalid cron expression: {CronSchedule}", _backupSettings.CronSchedule);
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                    continue;
                }

                // Calculate next run time
                var now = DateTime.UtcNow;
                var nextOccurrence = _cronExpression!.GetNextOccurrence(now, TimeZoneInfo.Local);

                if (nextOccurrence == null)
                {
                    _logger.LogWarning("Could not calculate next backup time");
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                    continue;
                }

                _nextRunTime = nextOccurrence.Value;
                var delay = nextOccurrence.Value - now;

                _logger.LogInformation("Next scheduled backup at {NextRun} (in {Delay})", 
                    _nextRunTime.Value.ToString("g"), 
                    delay.ToString(@"hh\:mm\:ss"));

                // Wait until next run time (check every minute in case settings change)
                while (delay > TimeSpan.Zero && !stoppingToken.IsCancellationRequested)
                {
                    var waitTime = delay > TimeSpan.FromMinutes(1) 
                        ? TimeSpan.FromMinutes(1) 
                        : delay;

                    await Task.Delay(waitTime, stoppingToken);

                    // Re-check if settings changed
                    if (!_backupSettings.IsScheduledBackupConfigured())
                    {
                        _logger.LogInformation("Scheduled backups were disabled");
                        break;
                    }

                    // Recalculate delay
                    now = DateTime.UtcNow;
                    delay = nextOccurrence.Value - now;
                }

                // Check if we should run the backup
                if (delay <= TimeSpan.Zero && _backupSettings.IsScheduledBackupConfigured())
                {
                    await RunScheduledBackupAsync(stoppingToken);
                }
            }
            catch (TaskCanceledException)
            {
                _logger.LogInformation("Backup scheduler service stopping...");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in backup scheduler: {Message}", ex.Message);
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }

    private bool TryParseCronExpression()
    {
        try
        {
            _cronExpression = CronExpression.Parse(_backupSettings.CronSchedule);
            return true;
        }
        catch
        {
            _cronExpression = null;
            return false;
        }
    }

    private async Task RunScheduledBackupAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Running scheduled backup...");

        try
        {
            var password = _backupSettings.ScheduledBackupPassword;
            if (string.IsNullOrEmpty(password))
            {
                _logger.LogWarning("Scheduled backup password not configured");
                return;
            }

            // Create backup to configured folder
            var filepath = await _backupService.CreateBackupAsync(
                password, 
                BackupLocation.Default, // Uses configured folder via settings
                null);

            _logger.LogInformation("Scheduled backup completed: {FilePath}", filepath);

            // Apply retention policy
            ApplyRetentionPolicy();

            // Show notification if enabled
            if (_backupSettings.ShowBackupNotifications)
            {
                ShowBackupNotification(filepath, true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Scheduled backup failed: {Message}", ex.Message);

            if (_backupSettings.ShowBackupNotifications)
            {
                ShowBackupNotification(ex.Message, false);
            }
        }
    }

    private void ApplyRetentionPolicy()
    {
        try
        {
            var backups = _backupService.ListBackups();
            var now = DateTime.Now;
            var deletedCount = 0;

            for (int i = 0; i < backups.Length; i++)
            {
                var backup = backups[i];
                var shouldDelete = false;
                var reason = "";

                // Check count-based retention
                if (_backupSettings.RetentionCount > 0 && i >= _backupSettings.RetentionCount)
                {
                    shouldDelete = true;
                    reason = $"exceeds retention count ({_backupSettings.RetentionCount})";
                }

                // Check age-based retention
                if (_backupSettings.RetentionDays > 0)
                {
                    var age = now - backup.CreatedAt;
                    if (age.TotalDays > _backupSettings.RetentionDays)
                    {
                        shouldDelete = true;
                        reason = $"older than {_backupSettings.RetentionDays} days";
                    }
                }

                if (shouldDelete)
                {
                    try
                    {
                        System.IO.File.Delete(backup.FilePath);
                        deletedCount++;
                        _logger.LogInformation("Deleted old backup {FileName}: {Reason}", backup.FileName, reason);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete old backup: {FileName}", backup.FileName);
                    }
                }
            }

            if (deletedCount > 0)
            {
                _logger.LogInformation("Retention policy applied: deleted {Count} old backups", deletedCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying retention policy: {Message}", ex.Message);
        }
    }

    private void ShowBackupNotification(string message, bool success)
    {
        // This would integrate with NotifyIconService to show a balloon notification
        // For now, just log it
        if (success)
        {
            _logger.LogInformation("Backup notification: {Message}", message);
        }
        else
        {
            _logger.LogWarning("Backup notification (error): {Message}", message);
        }
    }

    public override void Dispose()
    {
        base.Dispose();
    }
}
