using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Bitwarden.AutoType.Desktop.Services;
using Cronos;
using MahApps.Metro.Controls;

namespace Bitwarden.AutoType.Desktop.Views;

/// <summary>
/// Dialog for configuring backup settings.
/// </summary>
public partial class BackupSettingsDialog : MetroWindow
{
    private readonly BackupSettings _settings;
    private readonly Action<BackupSettings> _saveSettings;

    public BackupSettingsDialog(BackupSettings settings, Action<BackupSettings> saveSettings)
    {
        InitializeComponent();
        
        _settings = settings;
        _saveSettings = saveSettings;

        LoadSettings();
        SetupEventHandlers();
    }

    private void LoadSettings()
    {
        // Backup folder
        BackupFolderTextBox.Text = _settings.ConfiguredBackupFolder ?? "";
        var defaultFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Bitwarden.AutoType",
            "Backups");
        DefaultFolderText.Text = $"Default: {defaultFolder}";

        // Scheduled backups
        EnableScheduledCheckBox.IsChecked = _settings.ScheduledBackupEnabled;
        ScheduleComboBox.Text = _settings.CronSchedule;
        ScheduledPasswordBox.Password = _settings.ScheduledBackupPassword ?? "";
        UpdateSchedulePanel();
        UpdateScheduleDescription();

        // Retention
        RetentionCountUpDown.Value = _settings.RetentionCount;
        RetentionDaysUpDown.Value = _settings.RetentionDays;

        // Notifications
        ShowNotificationsCheckBox.IsChecked = _settings.ShowBackupNotifications;

        // Last backup time
        if (_settings.LastBackupTime.HasValue)
        {
            LastBackupText.Text = $"Last backup: {_settings.LastBackupTime.Value.ToLocalTime():g}";
        }
        else
        {
            LastBackupText.Text = "Last backup: Never";
        }
    }

    private void SetupEventHandlers()
    {
        EnableScheduledCheckBox.Checked += (s, e) => UpdateSchedulePanel();
        EnableScheduledCheckBox.Unchecked += (s, e) => UpdateSchedulePanel();
        ScheduleComboBox.SelectionChanged += (s, e) => UpdateScheduleDescription();
        ScheduleComboBox.LostFocus += (s, e) => UpdateScheduleDescription();
    }

    private void UpdateSchedulePanel()
    {
        SchedulePanel.IsEnabled = EnableScheduledCheckBox.IsChecked == true;
        SchedulePanel.Opacity = EnableScheduledCheckBox.IsChecked == true ? 1.0 : 0.5;
    }

    private void UpdateScheduleDescription()
    {
        var cronText = ScheduleComboBox.Text?.Trim() ?? "";
        
        // Check if it's a predefined schedule
        if (ScheduleComboBox.SelectedItem is ComboBoxItem item && item.Tag is string description)
        {
            ScheduleDescriptionText.Text = $"({description})";
            return;
        }

        // Try to parse and describe the cron expression
        try
        {
            var cron = CronExpression.Parse(cronText);
            var next = cron.GetNextOccurrence(DateTime.UtcNow, TimeZoneInfo.Local);
            if (next.HasValue)
            {
                ScheduleDescriptionText.Text = $"Next run: {next.Value.ToLocalTime():g}";
            }
            else
            {
                ScheduleDescriptionText.Text = "(Valid expression)";
            }
        }
        catch
        {
            if (!string.IsNullOrEmpty(cronText))
            {
                ScheduleDescriptionText.Text = "(Invalid cron expression)";
            }
            else
            {
                ScheduleDescriptionText.Text = "";
            }
        }
    }

    private void BrowseFolder_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Select backup folder",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = true
        };

        if (!string.IsNullOrEmpty(BackupFolderTextBox.Text) && Directory.Exists(BackupFolderTextBox.Text))
        {
            dialog.SelectedPath = BackupFolderTextBox.Text;
        }

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            BackupFolderTextBox.Text = dialog.SelectedPath;
        }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        // Validate cron expression if scheduled backups are enabled
        if (EnableScheduledCheckBox.IsChecked == true)
        {
            var cronText = ScheduleComboBox.Text?.Trim() ?? "";
            
            try
            {
                CronExpression.Parse(cronText);
            }
            catch
            {
                MessageBox.Show(
                    "Invalid cron expression. Please enter a valid cron schedule.",
                    "Invalid Schedule",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(ScheduledPasswordBox.Password))
            {
                MessageBox.Show(
                    "A password is required for scheduled backups.",
                    "Password Required",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }
        }

        // Update settings
        _settings.ConfiguredBackupFolder = string.IsNullOrWhiteSpace(BackupFolderTextBox.Text) 
            ? null 
            : BackupFolderTextBox.Text.Trim();
        
        _settings.ScheduledBackupEnabled = EnableScheduledCheckBox.IsChecked == true;
        _settings.CronSchedule = ScheduleComboBox.Text?.Trim() ?? "0 0 * * *";
        _settings.ScheduledBackupPassword = string.IsNullOrWhiteSpace(ScheduledPasswordBox.Password)
            ? null
            : ScheduledPasswordBox.Password;
        
        _settings.RetentionCount = (int)(RetentionCountUpDown.Value ?? 10);
        _settings.RetentionDays = (int)(RetentionDaysUpDown.Value ?? 30);
        
        _settings.ShowBackupNotifications = ShowNotificationsCheckBox.IsChecked == true;

        // Save to file
        _saveSettings(_settings);

        MessageBox.Show(
            "Backup settings saved successfully.\n\nNote: If you enabled or changed scheduled backups, restart the application for changes to take effect.",
            "Settings Saved",
            MessageBoxButton.OK,
            MessageBoxImage.Information);

        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
