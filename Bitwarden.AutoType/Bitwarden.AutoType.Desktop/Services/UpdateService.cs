using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.Logging;
using Velopack;
using Velopack.Sources;

namespace Bitwarden.AutoType.Desktop.Services;

/// <summary>
/// Service for checking and applying application updates via Velopack.
/// Updates are fetched from GitHub Releases.
/// </summary>
public class UpdateService
{
    private readonly ILogger<UpdateService> _logger;
    private readonly UpdateManager _updateManager;
    private UpdateInfo? _pendingUpdate;
    private bool _isCheckingForUpdates;
    private bool _isDownloading;

    /// <summary>
    /// GitHub repository URL for update source.
    /// </summary>
    public const string GitHubRepoUrl = "https://github.com/modarken/Bitwarden-PowerTools";

    /// <summary>
    /// Gets the current application version.
    /// </summary>
    public static Version CurrentVersion => Assembly.GetExecutingAssembly().GetName().Version 
        ?? new Version(1, 0, 0);

    /// <summary>
    /// Gets the current version as a display string.
    /// </summary>
    public static string CurrentVersionString => $"v{CurrentVersion.Major}.{CurrentVersion.Minor}.{CurrentVersion.Build}";

    /// <summary>
    /// Indicates whether an update check is in progress.
    /// </summary>
    public bool IsCheckingForUpdates => _isCheckingForUpdates;

    /// <summary>
    /// Indicates whether an update is being downloaded.
    /// </summary>
    public bool IsDownloading => _isDownloading;

    /// <summary>
    /// Indicates whether an update is available and ready to install.
    /// </summary>
    public bool IsUpdateAvailable => _pendingUpdate != null;

    /// <summary>
    /// Gets information about the pending update, if any.
    /// </summary>
    public UpdateInfo? PendingUpdate => _pendingUpdate;

    /// <summary>
    /// Indicates whether the app is running in a Velopack-installed context.
    /// When false, updates are not available (e.g., running from Visual Studio).
    /// </summary>
    public bool IsInstalled => _updateManager.IsInstalled;

    /// <summary>
    /// Event raised when update status changes (checking, available, downloading, etc.)
    /// </summary>
    public event EventHandler<UpdateStatusChangedEventArgs>? StatusChanged;

    public UpdateService(ILogger<UpdateService> logger)
    {
        _logger = logger;

        // Configure update source from GitHub releases
        // Set prerelease to false for stable releases only
        var source = new GithubSource(GitHubRepoUrl, accessToken: null, prerelease: false);
        _updateManager = new UpdateManager(source);

        if (!_updateManager.IsInstalled)
        {
            _logger.LogInformation("Running in development mode - updates disabled");
        }
    }

    /// <summary>
    /// Checks for available updates.
    /// </summary>
    /// <param name="silent">If true, doesn't show "no updates" message.</param>
    /// <returns>True if an update is available.</returns>
    public async Task<bool> CheckForUpdatesAsync(bool silent = false)
    {
        // Skip update check if not installed via Velopack
        if (!_updateManager.IsInstalled)
        {
            _logger.LogDebug("Skipping update check - not installed via Velopack");
            
            if (!silent)
            {
                MessageBox.Show(
                    "Updates are not available in development mode.\n\n" +
                    "To test updates, build and install using:\n" +
                    ".\\scripts\\Build-Release.ps1 -Install",
                    "Development Mode",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            
            return false;
        }

        if (_isCheckingForUpdates)
        {
            _logger.LogDebug("Update check already in progress");
            return false;
        }

        try
        {
            _isCheckingForUpdates = true;
            OnStatusChanged(UpdateStatus.Checking, "Checking for updates...");

            _logger.LogInformation("Checking for updates from {Repo}", GitHubRepoUrl);

            var updateInfo = await _updateManager.CheckForUpdatesAsync();

            if (updateInfo == null)
            {
                _logger.LogInformation("No updates available. Current version: {Version}", CurrentVersionString);
                _pendingUpdate = null;
                OnStatusChanged(UpdateStatus.NoUpdate, "You're running the latest version.");

                if (!silent)
                {
                    MessageBox.Show(
                        $"You're running the latest version.\n\nCurrent version: {CurrentVersionString}",
                        "No Updates Available",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }

                return false;
            }

            _pendingUpdate = updateInfo;
            var newVersion = updateInfo.TargetFullRelease.Version;
            _logger.LogInformation("Update available: {CurrentVersion} → {NewVersion}", 
                CurrentVersionString, newVersion);

            OnStatusChanged(UpdateStatus.Available, $"Update available: v{newVersion}");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check for updates: {Message}", ex.Message);
            OnStatusChanged(UpdateStatus.Error, $"Update check failed: {ex.Message}");

            if (!silent)
            {
                MessageBox.Show(
                    $"Failed to check for updates:\n\n{ex.Message}",
                    "Update Check Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }

            return false;
        }
        finally
        {
            _isCheckingForUpdates = false;
        }
    }

    /// <summary>
    /// Downloads the pending update.
    /// </summary>
    /// <param name="progress">Optional progress callback (0-100).</param>
    /// <returns>True if download succeeded.</returns>
    public async Task<bool> DownloadUpdateAsync(Action<int>? progress = null)
    {
        if (_pendingUpdate == null)
        {
            _logger.LogWarning("No pending update to download");
            return false;
        }

        if (_isDownloading)
        {
            _logger.LogDebug("Download already in progress");
            return false;
        }

        try
        {
            _isDownloading = true;
            OnStatusChanged(UpdateStatus.Downloading, "Downloading update...");

            _logger.LogInformation("Downloading update: v{Version}", 
                _pendingUpdate.TargetFullRelease.Version);

            await _updateManager.DownloadUpdatesAsync(
                _pendingUpdate,
                p => progress?.Invoke(p));

            _logger.LogInformation("Update downloaded successfully");
            OnStatusChanged(UpdateStatus.ReadyToInstall, "Update ready to install.");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download update: {Message}", ex.Message);
            OnStatusChanged(UpdateStatus.Error, $"Download failed: {ex.Message}");
            return false;
        }
        finally
        {
            _isDownloading = false;
        }
    }

    /// <summary>
    /// Applies the downloaded update and restarts the application.
    /// </summary>
    public void ApplyUpdateAndRestart()
    {
        if (_pendingUpdate == null)
        {
            _logger.LogWarning("No pending update to apply");
            return;
        }

        try
        {
            _logger.LogInformation("Applying update and restarting...");
            OnStatusChanged(UpdateStatus.Installing, "Installing update...");

            _updateManager.ApplyUpdatesAndRestart(_pendingUpdate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply update: {Message}", ex.Message);
            OnStatusChanged(UpdateStatus.Error, $"Install failed: {ex.Message}");

            MessageBox.Show(
                $"Failed to install update:\n\n{ex.Message}",
                "Update Failed",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Applies the downloaded update on next application start (no immediate restart).
    /// </summary>
    public void ApplyUpdateOnExit()
    {
        if (_pendingUpdate == null)
        {
            _logger.LogWarning("No pending update to apply");
            return;
        }

        try
        {
            _logger.LogInformation("Update will be applied on next start");
            _updateManager.ApplyUpdatesAndExit(_pendingUpdate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to schedule update: {Message}", ex.Message);
        }
    }

    /// <summary>
    /// Checks for updates, downloads if available, and prompts user to install.
    /// </summary>
    /// <param name="silent">If true, only prompts when update is available.</param>
    public async Task CheckDownloadAndPromptAsync(bool silent = false)
    {
        var hasUpdate = await CheckForUpdatesAsync(silent);
        
        if (!hasUpdate || _pendingUpdate == null)
        {
            return;
        }

        var newVersion = _pendingUpdate.TargetFullRelease.Version;

        // Prompt user
        var result = MessageBox.Show(
            $"A new version is available!\n\n" +
            $"Current version: {CurrentVersionString}\n" +
            $"New version: v{newVersion}\n\n" +
            $"Would you like to download and install the update now?",
            "Update Available",
            MessageBoxButton.YesNo,
            MessageBoxImage.Information);

        if (result != MessageBoxResult.Yes)
        {
            _logger.LogInformation("User declined update");
            return;
        }

        // Download
        var downloaded = await DownloadUpdateAsync();
        if (!downloaded)
        {
            return;
        }

        // Prompt for restart
        var restartResult = MessageBox.Show(
            "Update downloaded successfully!\n\n" +
            "The application needs to restart to complete the update.\n\n" +
            "Restart now?",
            "Update Ready",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (restartResult == MessageBoxResult.Yes)
        {
            ApplyUpdateAndRestart();
        }
        else
        {
            MessageBox.Show(
                "The update will be installed when you next restart the application.",
                "Update Pending",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }

    private void OnStatusChanged(UpdateStatus status, string message)
    {
        StatusChanged?.Invoke(this, new UpdateStatusChangedEventArgs(status, message));
    }
}

/// <summary>
/// Update status states.
/// </summary>
public enum UpdateStatus
{
    Idle,
    Checking,
    NoUpdate,
    Available,
    Downloading,
    ReadyToInstall,
    Installing,
    Error
}

/// <summary>
/// Event args for update status changes.
/// </summary>
public class UpdateStatusChangedEventArgs : EventArgs
{
    public UpdateStatus Status { get; }
    public string Message { get; }

    public UpdateStatusChangedEventArgs(UpdateStatus status, string message)
    {
        Status = status;
        Message = message;
    }
}
