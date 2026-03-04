using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Bitwarden.AutoType.Desktop.Helpers;
using Bitwarden.AutoType.Desktop.Models;
using Bitwarden.AutoType.Desktop.Services;
using Bitwarden.AutoType.Desktop.Views;
using Bitwarden.AutoType.Desktop.Windows;
using Bitwarden.AutoType.Desktop.Windows.Native;
using Bitwarden.Core.Crypto;
using Bitwarden.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using static Bitwarden.AutoType.Desktop.Windows.Native.WindowsDLLs;

namespace Bitwarden.AutoType.Desktop;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TargetTypes
{
    Title,
    Process,
    Class
}

public class AutoTypeCustomField
{
    [JsonIgnore]
    public string? Name { get; set; }

    [JsonIgnore]
    public string? UserName { get; set; }

    public string? Target { get; set; }
    public TargetTypes? Type { get; set; }
    public string? Sequence { get; set; }
}

/// <summary>
/// Holds an AutoTypeCustomField with its pre-compiled Regex for efficient matching.
/// </summary>
public record CachedAutoTypeEntry(AutoTypeCustomField Field, Regex CompiledRegex, Cipher Cipher);

[INotifyPropertyChanged]
public partial class AutoTypeViewModel : IDisposable
{
    private readonly ILogger<AutoTypeViewModel> _logger;
    private readonly HotkeyService _hotkeyService;
    private readonly BitwardenService _bitwardenService;
    private readonly AutoTypeSettings _autoTypeSettings;
    private readonly Action<AutoTypeSettings> _save;
    private readonly StateController<AutoTypeConfigurationStates> _state;
    private List<CachedAutoTypeEntry>? _regexLookup;

    #region Bound

    [ObservableProperty]
    private bool _isPinned;

    [RelayCommand]
    public void TogglePin()
    {
        IsPinned = !IsPinned;
    }

    [ObservableProperty]
    private bool _isRunningAsAdmin;

    [ObservableProperty]
    private bool _showElevationWarning;

    [ObservableProperty]
    private string? _elevationWarningMessage;

    [RelayCommand]
    public void RestartAsAdmin()
    {
        ElevationHelper.RestartAsAdministrator();
    }

    [ObservableProperty]
    private ObservableCollection<CipherDisplay>? _filteredCiphers;

    [ObservableProperty]
    private string? _filterSearchText;

    void OnFilterSearchTextChanged()
    {
        OnFilterSearchTextChanged(FilterSearchText);
    }

    partial void OnFilterSearchTextChanged(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            if (Ciphers is null)
            {
                FilteredCiphers = null;
                return;
            }

            FilteredCiphers = new ObservableCollection<CipherDisplay>(Ciphers);
        }
        else
        {
            if (Ciphers is null)
            {
                FilteredCiphers = null;
                return;
            }

            FilteredCiphers = new ObservableCollection<CipherDisplay>(
                Ciphers.Where(c => c?.Name is not null && c.Name.Contains(value, StringComparison.OrdinalIgnoreCase)));
        }

        OnPropertyChanged(nameof(FilteredCiphers));
    }

    [ObservableProperty]
    private ObservableCollection<CipherDisplay>? _ciphers;

    [ObservableProperty]
    private bool _isAutoTypeEnabled = false;

    partial void OnIsAutoTypeEnabledChanged(bool value)
    {
        if (value)
        {
            _hotkeyService.Enable();
        }
        else
        {
            _hotkeyService.Disable();
        }
        _autoTypeSettings.AutoTypeOnOff = IsAutoTypeEnabled;
        _save(_autoTypeSettings);
    }

    [RelayCommand]
    public async Task Refresh()
    {
        try
        {
            await _bitwardenService.RefreshLocalDatabaseAsync();

        }
        catch (Exception e)
        {
            _logger.Log(LogLevel.Error, $"{nameof(AutoTypeViewModel)}.{nameof(Refresh)}() Exception:'{e.Message}'");
        }
    }

    #endregion Bound

    public AutoTypeViewModel(ILogger<AutoTypeViewModel> logger,
        HotkeyService hotkeyService,
        BitwardenService bitwardenService,
        AutoTypeSettings autoTypeSettings,
        Action<AutoTypeSettings> save,
        StateController<AutoTypeConfigurationStates> state)
    {
        _logger = logger;
        _hotkeyService = hotkeyService;
        _bitwardenService = bitwardenService;
        _autoTypeSettings = autoTypeSettings;
        _save = save;
        _state = state;
        
        // Check elevation status
        IsRunningAsAdmin = ElevationHelper.IsRunningAsAdministrator();
        
        if (_autoTypeSettings.AutoTypeOnOff is null)
        {
            IsAutoTypeEnabled = true;
        }
        else
        {
            IsAutoTypeEnabled = _autoTypeSettings.AutoTypeOnOff.Value;
        }

        Task.Run(() => InitializeRegexListAsync()); // Run the method asynchronously
        _hotkeyService.RegisterOnHotKeyAction(OnHotKeyHandler);
    }

    #region Database Management

    private async Task InitializeRegexListAsync()
    {
        if (_state.GetState() == AutoTypeConfigurationStates.Configured)
        {
            try
            {
                OnDatabaseUpdated(await _bitwardenService.GetDatabase());
            }
            catch (Exception e)
            {
                _logger.Log(LogLevel.Error, $"{nameof(AutoTypeViewModel)}.{nameof(InitializeRegexListAsync)}() Exception:'{e.Message}'");
            }
        }
        else
        {
            _logger.Log(LogLevel.Warning, $"{nameof(AutoTypeViewModel)}.{nameof(InitializeRegexListAsync)}() Not configured.");
        }

        _bitwardenService.RegisterOnDatabaseUpdated(OnDatabaseUpdated);
    }

    private void OnDatabaseUpdated(SyncResponse syncResponse)
    {
        List<CachedAutoTypeEntry> expressions = new();

        var decryptionKey = _bitwardenService.GetEncryptionKey();

        if (decryptionKey is null)
        {
            throw new Exception("Unable to get decryption key from configuration");
        }

        var serializerOptions = new JsonSerializerOptions() { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
        var ciphers = new List<CipherDisplay>();

        if (syncResponse.Ciphers != null && decryptionKey != null)
        {
            // Iterate through ciphers and check for custom fields with the specified title name
            foreach (Cipher cipher in syncResponse.Ciphers)
            {
                if (cipher is null)
                {
                    continue;
                }

                if (cipher.Fields != null && cipher.Fields.Any())
                {
                    foreach (Field1 field in cipher.Fields)
                    {
                        var name = BitwardenCrypto.DecryptEntry(field.Name!, decryptionKey!, true);

                        if (name is not null && name.Equals(Constants.BitwardenCustomFieldName, StringComparison.OrdinalIgnoreCase))
                        {
                            var value = BitwardenCrypto.DecryptEntry(field.Value!, decryptionKey!, true);

                            if (value is not null
                                && JsonSerializer.Deserialize<AutoTypeCustomField>(value, serializerOptions)
                                is AutoTypeCustomField autoTypeCustomField)
                            {
                                string? cipherName = null;
                                if (cipher?.Name is string)
                                {
                                    cipherName = BitwardenCrypto.DecryptEntry(cipher!.Name, decryptionKey!, true);
                                }

                                string? userName = null;
                                if (cipher?.Login?.Username is not null)
                                {
                                    userName = BitwardenCrypto.DecryptEntry(cipher.Login!.Username!, decryptionKey!, true);
                                }

                                autoTypeCustomField.Name = cipherName;
                                autoTypeCustomField.UserName = userName;
                                var compiledRegex = new Regex(autoTypeCustomField.Target!, RegexOptions.IgnoreCase | RegexOptions.Compiled);
                                expressions.Add(new CachedAutoTypeEntry(autoTypeCustomField, compiledRegex, cipher!));

                                if (cipherName is not null)
                                {
                                    var id = cipher!.Id;

                                    if (!ciphers.Any(c => c.Id == id))
                                    {
                                        ciphers.Add(new CipherDisplay { Id = id, Name = cipherName, User = userName });
                                    }
                                }
                            }
                            else
                            {
                                throw new Exception("Unable to Deserialize field.Value");
                            }
                        }
                    }
                }
            }
        }

        if (ciphers.Count > 0)
        {
            Ciphers = new ObservableCollection<CipherDisplay>(ciphers.OrderBy(c => c.Name));
        }
        else
        {
            Ciphers = null;
        }

        _regexLookup = expressions;

        OnFilterSearchTextChanged(FilterSearchText);
    }

    #endregion Database Management

    #region OnHotKey pressed

    private void OnHotKeyHandler(WindowsHotKey windowsHotKey)
    {
        if (IsAutoTypeEnabled == false)
        {
            return;
        }

        if (_regexLookup is null)
        {
            return;
        }

        var (currentHandle, currentProcess) = WindowsAPI.GetForegroundProcess();

        if (currentProcess != null)
        {
            string windowTitle = WindowsAPI.GetWindowTitle(currentHandle);
            string processName = currentProcess.ProcessName;
            string windowClassName = WindowsAPI.GetWindowClassName(currentHandle);

            // Check if the target window requires elevation
            if (!IsRunningAsAdmin && ElevationHelper.DoesWindowRequireElevation(currentHandle))
            {
                ShowElevationWarning = true;
                ElevationWarningMessage = $"The window '{windowTitle}' requires administrator privileges to automate.\n\nClick 'Restart as Admin' to enable autotype for protected windows.";
                _logger.LogWarning($"Elevation required for window: {windowTitle} (Process: {processName})");
                return;
            }
            
            // Hide warning if it was showing
            ShowElevationWarning = false;

            var matchedRegex = _regexLookup!
                .Where(r =>
                    (r.Field.Type == TargetTypes.Title && r.CompiledRegex.IsMatch(windowTitle))
                    || (r.Field.Type == TargetTypes.Process && r.CompiledRegex.IsMatch(processName))
                    || (r.Field.Type == TargetTypes.Class && r.CompiledRegex.IsMatch(windowClassName))
                    || (r.Field.Type is null && r.CompiledRegex.IsMatch(windowTitle))) // default to title if no type specified
                .ToList(); // Materialize once to avoid multiple enumerations

            if (matchedRegex.Count == 1)
            {
                ExecuteMatchHandler(matchedRegex[0], currentHandle);
            }
            else if (matchedRegex.Count > 1)
            {
                ShowPopup(matchedRegex, currentHandle);
            }
        }
    }

    private void ShowPopup(List<CachedAutoTypeEntry> matchedRegex, IntPtr handle)
    {
        // Check if a MatchSelectionWindow already exists
        var matchSelectionWindow = Application.Current.Windows.OfType<MatchSelectionWindow>().FirstOrDefault();
        if (matchSelectionWindow != null)
        {
            // If a MatchSelectionWindow already exists, close it
            matchSelectionWindow.Close();
            matchSelectionWindow = null;
        }

        // Create a new instance of MatchSelectionWindow
        matchSelectionWindow = new MatchSelectionWindow(matchedRegex);

        var userResult = matchSelectionWindow.ShowDialog();

        if (userResult == true)
        {
            ExecuteMatchHandler(matchSelectionWindow.SelectedMatch!, handle);
        }
    }

    #region Windows Event Handling

    private async void ExecuteMatchHandler(CachedAutoTypeEntry? match, IntPtr handle)
    {
        var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        // delegate to handle windows events and cancel the token when the window focus changes
        WinEventDelegate windEventHandler = (IntPtr hWinEventHook, uint eventType, IntPtr hWnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime) =>
        {
            if (eventType == WindowsConstants.EVENT_SYSTEM_FOREGROUND)
            {
                // The window focus has changed
                tokenSource.Cancel();
            }
        };

        IntPtr winEventHook = default;

        try
        {
            // Set the handle to the foreground
            _ = SetForegroundWindow(handle);

            // hook into the windows events
            winEventHook = SetWinEventHook(
                WindowsConstants.EVENT_SYSTEM_FOREGROUND,
                WindowsConstants.EVENT_SYSTEM_FOREGROUND, IntPtr.Zero, windEventHandler, 0, 0,
                WindowsConstants.WINEVENT_OUTOFCONTEXT);

            await Task.Delay(TimeSpan.FromMilliseconds(250), tokenSource.Token); // in testing must sleep
            await ExecuteMatchFunctionAsync(match, tokenSource.Token);
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning($"{nameof(AutoTypeViewModel)}.{nameof(OnHotKeyHandler)}() {nameof(TaskCanceledException)}");
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"{nameof(AutoTypeViewModel)}.{nameof(OnHotKeyHandler)}() Exception:'{e.Message}'");
        }
        finally
        {
            tokenSource?.Dispose();
            // Unhook from windows events
            if (winEventHook != IntPtr.Zero)
            {
                UnhookWinEvent(winEventHook);
            }
        }
    }

    #endregion Windows Event Handling

    private async Task ExecuteMatchFunctionAsync(CachedAutoTypeEntry? match, CancellationToken token = default)
    {
        if (match is CachedAutoTypeEntry actualMatch)
        {
            var decryptionKey = _bitwardenService.GetEncryptionKey();

            Func<string, string?> func = (s) =>
            {
                return BitwardenCrypto.DecryptEntry(s, decryptionKey!, true);
            };

            var config = new DefaultKeystrokeConfiguration
            {
                DelayBetweenKeystrokes = TimeSpan.FromMilliseconds(25),
                PressKeyTime = TimeSpan.FromMilliseconds(15)
            };

            BitwardenKeystrokeSequence bitwardenKeystrokeSequence = new(actualMatch.Field.Sequence!, config, actualMatch.Cipher, func);
            await WindowsKeyboard.SendKeystrokesAsync(bitwardenKeystrokeSequence, token); ;
        }
    }

    #endregion OnHotKey pressed

    #region IDisposable

    public void Dispose()
    {
    }

    #endregion IDisposable
}