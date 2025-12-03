using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using Bitwarden.Core.Models;
using Microsoft.Extensions.Logging;

namespace Bitwarden.AutoType.Desktop.Services;

/// <summary>
/// Known backup locations.
/// </summary>
public enum BackupLocation
{
    Configured,     // User-configured folder from settings
    Default,        // %LocalAppData%\Bitwarden.AutoType\Backups
    Documents,      // Documents folder
    OneDrive,       // OneDrive folder
    Custom          // User-selected folder
}

/// <summary>
/// Service for creating encrypted backups of the Bitwarden vault data.
/// Uses AES-256-GCM encryption with PBKDF2 key derivation.
/// </summary>
public class BackupService
{
    private readonly ILogger<BackupService> _logger;
    private readonly BitwardenService _bitwardenService;
    private readonly BackupSettings _backupSettings;
    private readonly string _defaultBackupFolder;

    private const int SaltSize = 16;
    private const int KeySize = 32; // 256 bits
    private const int NonceSize = 12; // 96 bits for AES-GCM
    private const int TagSize = 16; // 128 bits
    private const int Iterations = 100000;
    private const string BackupExtension = ".bwbackup";
    private const string DecryptedExtension = ".json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public BackupService(ILogger<BackupService> logger, BitwardenService bitwardenService, BackupSettings backupSettings)
    {
        _logger = logger;
        _bitwardenService = bitwardenService;
        _backupSettings = backupSettings;

        // Default backup location
        _defaultBackupFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Bitwarden.AutoType",
            "Backups");

        // Ensure backup folder exists
        Directory.CreateDirectory(_defaultBackupFolder);
    }

    /// <summary>
    /// Gets the backup settings.
    /// </summary>
    public BackupSettings Settings => _backupSettings;

    /// <summary>
    /// Gets the configured backup folder (or default if not configured).
    /// </summary>
    public string GetConfiguredBackupFolder() => _backupSettings.GetEffectiveBackupFolder();

    /// <summary>
    /// Applies the retention policy to the specified folder, deleting old backups.
    /// </summary>
    /// <param name="folder">The folder to apply the policy to. Uses configured folder if null.</param>
    public void ApplyRetentionPolicy(string? folder = null)
    {
        var targetFolder = folder ?? GetConfiguredBackupFolder();
        var keepCount = _backupSettings.RetentionCount;
        var maxAgeDays = _backupSettings.RetentionDays;

        if (keepCount <= 0 && maxAgeDays <= 0)
        {
            _logger.LogDebug("Retention policy disabled (count={Count}, days={Days})", keepCount, maxAgeDays);
            return;
        }

        var backups = ListBackups(targetFolder);

        // Delete by age first
        if (maxAgeDays > 0)
        {
            var cutoffDate = DateTime.Now.AddDays(-maxAgeDays);
            var oldBackups = backups.Where(b => b.CreatedAt < cutoffDate).ToList();
            
            foreach (var backup in oldBackups)
            {
                try
                {
                    File.Delete(backup.FilePath);
                    _logger.LogInformation("Deleted backup older than {Days} days: {FileName}", maxAgeDays, backup.FileName);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete old backup: {FileName}", backup.FileName);
                }
            }

            // Refresh list after age-based deletion
            backups = ListBackups(targetFolder);
        }

        // Then delete by count
        if (keepCount > 0 && backups.Length > keepCount)
        {
            var toDelete = backups.Skip(keepCount);
            foreach (var backup in toDelete)
            {
                try
                {
                    File.Delete(backup.FilePath);
                    _logger.LogInformation("Deleted backup exceeding retention count of {Count}: {FileName}", keepCount, backup.FileName);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete old backup: {FileName}", backup.FileName);
                }
            }
        }
    }

    /// <summary>
    /// Gets the folder path for a backup location.
    /// </summary>
    public string GetBackupFolderForLocation(BackupLocation location)
    {
        var folder = location switch
        {
            BackupLocation.Configured => _backupSettings.GetEffectiveBackupFolder(),
            BackupLocation.Documents => Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Bitwarden Backups"),
            BackupLocation.OneDrive => GetOneDriveBackupFolder(),
            _ => _defaultBackupFolder
        };

        Directory.CreateDirectory(folder);
        return folder;
    }

    private static string GetOneDriveBackupFolder()
    {
        // Try to find OneDrive folder
        var oneDrive = Environment.GetEnvironmentVariable("OneDrive");
        if (string.IsNullOrEmpty(oneDrive))
        {
            oneDrive = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "OneDrive");
        }

        if (!Directory.Exists(oneDrive))
        {
            throw new DirectoryNotFoundException("OneDrive folder not found. Please ensure OneDrive is installed and configured.");
        }

        return Path.Combine(oneDrive, "Bitwarden Backups");
    }

    /// <summary>
    /// Creates an encrypted backup of the current vault data.
    /// </summary>
    /// <param name="password">Password to encrypt the backup with.</param>
    /// <param name="location">Where to save the backup.</param>
    /// <param name="customFolder">Custom folder path if location is Custom.</param>
    /// <returns>The path to the created backup file.</returns>
    public async Task<string> CreateBackupAsync(string password, BackupLocation location = BackupLocation.Default, string? customFolder = null)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Backup password cannot be empty.", nameof(password));
        }

        var targetFolder = location == BackupLocation.Custom && !string.IsNullOrEmpty(customFolder)
            ? customFolder
            : GetBackupFolderForLocation(location);

        Directory.CreateDirectory(targetFolder);

        _logger.LogInformation("Starting vault backup to {Location}...", location);

        // Get the current vault data
        var syncResponse = await _bitwardenService.GetDatabase();

        // Serialize to JSON
        var json = JsonSerializer.Serialize(syncResponse, JsonOptions);
        var plaintext = Encoding.UTF8.GetBytes(json);

        // Encrypt
        var encryptedData = Encrypt(plaintext, password);

        // Generate filename with timestamp
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        var filename = $"backup-{timestamp}{BackupExtension}";
        var filepath = Path.Combine(targetFolder, filename);

        // Write to file
        await File.WriteAllBytesAsync(filepath, encryptedData);

        _logger.LogInformation("Backup created successfully: {FilePath}", filepath);

        // Prune old backups in default folder only (keep last 10)
        if (location == BackupLocation.Default)
        {
            PruneOldBackups(10);
        }

        return filepath;
    }

    /// <summary>
    /// Creates an encrypted backup of the current vault data to a specific folder.
    /// </summary>
    /// <param name="password">Password to encrypt the backup with.</param>
    /// <param name="targetFolder">The folder to save the backup to.</param>
    /// <returns>The path to the created backup file.</returns>
    public async Task<string> CreateBackupAsync(string password, string targetFolder)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Backup password cannot be empty.", nameof(password));
        }

        if (string.IsNullOrWhiteSpace(targetFolder))
        {
            throw new ArgumentException("Target folder cannot be empty.", nameof(targetFolder));
        }

        Directory.CreateDirectory(targetFolder);

        _logger.LogInformation("Starting vault backup to configured folder: {Folder}", targetFolder);

        // Get the current vault data
        var syncResponse = await _bitwardenService.GetDatabase();

        // Serialize to JSON
        var json = JsonSerializer.Serialize(syncResponse, JsonOptions);
        var plaintext = Encoding.UTF8.GetBytes(json);

        // Encrypt
        var encryptedData = Encrypt(plaintext, password);

        // Generate filename with timestamp
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        var filename = $"backup-{timestamp}{BackupExtension}";
        var filepath = Path.Combine(targetFolder, filename);

        // Write to file
        await File.WriteAllBytesAsync(filepath, encryptedData);

        _logger.LogInformation("Backup created successfully: {FilePath}", filepath);

        // Update last backup time
        _backupSettings.LastBackupTime = DateTime.UtcNow;

        return filepath;
    }

    /// <summary>
    /// Exports the vault as a decrypted JSON file (WARNING: Contains sensitive data in plaintext).
    /// </summary>
    /// <param name="filepath">Where to save the decrypted export.</param>
    /// <returns>The path to the created file.</returns>
    public async Task<string> ExportDecryptedAsync(string filepath)
    {
        _logger.LogWarning("Exporting vault as decrypted JSON - this file contains sensitive data!");

        // Get the current vault data
        var syncResponse = await _bitwardenService.GetDatabase();

        // Serialize to JSON
        var json = JsonSerializer.Serialize(syncResponse, JsonOptions);

        // Write to file
        await File.WriteAllTextAsync(filepath, json, Encoding.UTF8);

        _logger.LogInformation("Decrypted export created: {FilePath}", filepath);

        return filepath;
    }

    /// <summary>
    /// Decrypts a backup file and saves it as a JSON file.
    /// </summary>
    /// <param name="backupFilepath">Path to the encrypted backup file.</param>
    /// <param name="password">Password to decrypt the backup.</param>
    /// <param name="outputFilepath">Where to save the decrypted JSON.</param>
    /// <returns>The path to the created file.</returns>
    public async Task<string> DecryptBackupToFileAsync(string backupFilepath, string password, string outputFilepath)
    {
        _logger.LogWarning("Decrypting backup to JSON - this file contains sensitive data!");

        // Read and decrypt the backup
        var syncResponse = await ReadBackupAsync(backupFilepath, password);

        if (syncResponse == null)
        {
            throw new InvalidOperationException("Backup file contains no data.");
        }

        // Serialize to JSON
        var json = JsonSerializer.Serialize(syncResponse, JsonOptions);

        // Write to file
        await File.WriteAllTextAsync(outputFilepath, json, Encoding.UTF8);

        _logger.LogInformation("Backup decrypted to: {FilePath}", outputFilepath);

        return outputFilepath;
    }

    /// <summary>
    /// Lists all available backup files in the default folder.
    /// </summary>
    public BackupInfo[] ListBackups() => ListBackups(_defaultBackupFolder);

    /// <summary>
    /// Lists all available backup files in the specified folder.
    /// </summary>
    /// <param name="folder">The folder to list backups from.</param>
    public BackupInfo[] ListBackups(string folder)
    {
        if (!Directory.Exists(folder))
        {
            return [];
        }

        return Directory.GetFiles(folder, $"*{BackupExtension}")
            .Select(f => new FileInfo(f))
            .OrderByDescending(f => f.CreationTime)
            .Select(f => new BackupInfo
            {
                FileName = f.Name,
                FilePath = f.FullName,
                CreatedAt = f.CreationTime,
                SizeBytes = f.Length
            })
            .ToArray();
    }

    /// <summary>
    /// Decrypts and reads a backup file (for verification or restore).
    /// </summary>
    /// <param name="filepath">Path to the backup file.</param>
    /// <param name="password">Password to decrypt the backup.</param>
    /// <returns>The decrypted SyncResponse.</returns>
    public async Task<SyncResponse?> ReadBackupAsync(string filepath, string password)
    {
        if (!File.Exists(filepath))
        {
            throw new FileNotFoundException("Backup file not found.", filepath);
        }

        _logger.LogInformation("Reading backup: {FilePath}", filepath);

        var encryptedData = await File.ReadAllBytesAsync(filepath);
        var plaintext = Decrypt(encryptedData, password);
        var json = Encoding.UTF8.GetString(plaintext);

        return JsonSerializer.Deserialize<SyncResponse>(json, JsonOptions);
    }

    /// <summary>
    /// Validates a backup file can be decrypted and contains valid data.
    /// </summary>
    public async Task<BackupValidationResult> ValidateBackupAsync(string filepath, string password)
    {
        try
        {
            var syncResponse = await ReadBackupAsync(filepath, password);
            
            if (syncResponse == null)
            {
                return new BackupValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Backup file contains no data."
                };
            }

            return new BackupValidationResult
            {
                IsValid = true,
                CipherCount = syncResponse.Ciphers?.Count() ?? 0,
                FolderCount = syncResponse.Folders?.Count() ?? 0,
                SyncResponse = syncResponse
            };
        }
        catch (CryptographicException)
        {
            return new BackupValidationResult
            {
                IsValid = false,
                ErrorMessage = "Invalid password or corrupted backup file."
            };
        }
        catch (Exception ex)
        {
            return new BackupValidationResult
            {
                IsValid = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Gets the default backup folder path.
    /// </summary>
    public string GetBackupFolder() => _defaultBackupFolder;

    /// <summary>
    /// Opens a backup folder in Windows Explorer.
    /// </summary>
    public void OpenBackupFolder(BackupLocation location = BackupLocation.Default)
    {
        try
        {
            var folder = GetBackupFolderForLocation(location);
            if (Directory.Exists(folder))
            {
                System.Diagnostics.Process.Start("explorer.exe", folder);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to open backup folder for {Location}", location);
        }
    }

    private void PruneOldBackups(int keepCount)
    {
        var backups = ListBackups();

        if (backups.Length <= keepCount)
        {
            return;
        }

        var toDelete = backups.Skip(keepCount);
        foreach (var backup in toDelete)
        {
            try
            {
                File.Delete(backup.FilePath);
                _logger.LogInformation("Pruned old backup: {FileName}", backup.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete old backup: {FileName}", backup.FileName);
            }
        }
    }

    /// <summary>
    /// Encrypts data using AES-256-GCM with PBKDF2 key derivation.
    /// Format: [salt (16 bytes)][nonce (12 bytes)][tag (16 bytes)][ciphertext]
    /// </summary>
    private static byte[] Encrypt(byte[] plaintext, string password)
    {
        // Generate random salt and nonce
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);

        // Derive key using PBKDF2
        var key = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            KeySize);

        // Encrypt using AES-GCM
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);

        // Combine: salt + nonce + tag + ciphertext
        var result = new byte[SaltSize + NonceSize + TagSize + ciphertext.Length];
        Buffer.BlockCopy(salt, 0, result, 0, SaltSize);
        Buffer.BlockCopy(nonce, 0, result, SaltSize, NonceSize);
        Buffer.BlockCopy(tag, 0, result, SaltSize + NonceSize, TagSize);
        Buffer.BlockCopy(ciphertext, 0, result, SaltSize + NonceSize + TagSize, ciphertext.Length);

        return result;
    }

    /// <summary>
    /// Decrypts data encrypted with the Encrypt method.
    /// </summary>
    private static byte[] Decrypt(byte[] encryptedData, string password)
    {
        if (encryptedData.Length < SaltSize + NonceSize + TagSize)
        {
            throw new CryptographicException("Invalid encrypted data format.");
        }

        // Extract components
        var salt = new byte[SaltSize];
        var nonce = new byte[NonceSize];
        var tag = new byte[TagSize];
        var ciphertext = new byte[encryptedData.Length - SaltSize - NonceSize - TagSize];

        Buffer.BlockCopy(encryptedData, 0, salt, 0, SaltSize);
        Buffer.BlockCopy(encryptedData, SaltSize, nonce, 0, NonceSize);
        Buffer.BlockCopy(encryptedData, SaltSize + NonceSize, tag, 0, TagSize);
        Buffer.BlockCopy(encryptedData, SaltSize + NonceSize + TagSize, ciphertext, 0, ciphertext.Length);

        // Derive key using PBKDF2
        var key = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            KeySize);

        // Decrypt using AES-GCM
        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(key, TagSize);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);

        return plaintext;
    }
}

/// <summary>
/// Information about a backup file.
/// </summary>
public class BackupInfo
{
    public required string FileName { get; init; }
    public required string FilePath { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required long SizeBytes { get; init; }

    public string FormattedSize => SizeBytes switch
    {
        < 1024 => $"{SizeBytes} B",
        < 1024 * 1024 => $"{SizeBytes / 1024.0:F1} KB",
        _ => $"{SizeBytes / (1024.0 * 1024.0):F1} MB"
    };
}

/// <summary>
/// Result of backup validation.
/// </summary>
public class BackupValidationResult
{
    public required bool IsValid { get; init; }
    public string? ErrorMessage { get; init; }
    public int CipherCount { get; init; }
    public int FolderCount { get; init; }
    public SyncResponse? SyncResponse { get; init; }
}
