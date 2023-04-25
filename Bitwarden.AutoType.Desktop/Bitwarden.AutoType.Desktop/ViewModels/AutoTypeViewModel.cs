using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Bitwarden.AutoType.Desktop.Services;
using Bitwarden.AutoType.Desktop.Views;
using Bitwarden.AutoType.Desktop.Windows;
using Bitwarden.AutoType.Desktop.Windows.Native;
using Bitwarden.Core.Crypto;
using Bitwarden.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace Bitwarden.AutoType.Desktop;

public class AutoTypeCustomField
{
    [JsonIgnore]
    public string? Name { get; set; }

    [JsonIgnore]
    public string? UserName { get; set; }

    public string? Target { get; set; }
    public string? Sequence { get; set; }
}

[INotifyPropertyChanged]
public partial class AutoTypeViewModel : IDisposable
{
    private readonly ILogger<AutoTypeViewModel> _logger;
    private readonly HotkeyService _hotkeyService;
    private readonly AutoTypeService _autoTypeService;
    private readonly BitwardenService _bitwardenService;
    private Dictionary<AutoTypeCustomField, Cipher>? _regexLookup;

    #region Bound Properties

    [ObservableProperty]
    private bool _isAutoTypeEnabled = false;

    partial void OnIsAutoTypeEnabledChanged(bool value)
    {
    }

    #endregion Bound Properties

    public AutoTypeViewModel(ILogger<AutoTypeViewModel> logger,
        HotkeyService hotkeyService,
        AutoTypeService autoTypeService,
        BitwardenService bitwardenService)
    {
        _logger = logger;
        _hotkeyService = hotkeyService;
        _autoTypeService = autoTypeService;
        _bitwardenService = bitwardenService;
        InitializeRegexList();
        _hotkeyService.RegisterOnHotKey(OnHotKeyHandler);
    }

    #region Database Management

    private void InitializeRegexList()
    {
        try
        {
            OnDatabaseUpdated(_bitwardenService.GetDatabase());
        }
        catch (Exception e)
        {
            _logger.Log(LogLevel.Error, $"{nameof(AutoTypeViewModel)}.{nameof(InitializeRegexList)}() Exception:'{e.Message}'");
        }

        _bitwardenService.RegisterOnDatabaseUpdated(OnDatabaseUpdated);
    }

    private void OnDatabaseUpdated(SyncResponse syncResponse)
    {
        Dictionary<AutoTypeCustomField, Cipher> expressions = new();

        var key = "AutoType:Custom";

        var decryptionKey = _bitwardenService.GetDecryptionKey();
        var serializerOptions = new JsonSerializerOptions() { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

        if (syncResponse.Ciphers != null)
        {
            // Iterate through ciphers and check for custom fields with the specified title name
            foreach (Cipher cipher in syncResponse.Ciphers)
            {
                if (cipher.Fields != null && cipher.Fields.Count() > 0)
                {
                    foreach (Field1 field in cipher.Fields)
                    {
                        var name = BitwardenCrypto.DecryptEntry(field.Name!, decryptionKey!, true);

                        if (name is not null && name.Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            var value = BitwardenCrypto.DecryptEntry(field.Value!, decryptionKey!, true);

                            if (value is not null
                                && JsonSerializer.Deserialize<AutoTypeCustomField>(value, serializerOptions)
                                is AutoTypeCustomField autoTypeCustomField)
                            {
                                autoTypeCustomField.UserName = BitwardenCrypto.DecryptEntry(cipher.Login!.Username!, decryptionKey!, true);
                                autoTypeCustomField.Name = BitwardenCrypto.DecryptEntry(cipher.Name!, decryptionKey!, true);
                                expressions.Add(autoTypeCustomField, cipher);
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

        _regexLookup = expressions;
    }

    #endregion Database Management

    #region OnHotKey pressed

    private void OnHotKeyHandler(WindowsHotKey windowsHotKey)
    {
        var (currentHandle, currentProcess) = GetForegroundProcess();

        if (currentProcess != null)
        {
            string windowTitle = GetWindowTitle(currentHandle);
            //string processName = currentProcess.ProcessName;

            var matchedRegex = _regexLookup!
                .Where(r => (new Regex(r.Key.Target!, RegexOptions.IgnoreCase)).IsMatch(windowTitle))
                .AsEnumerable()
                //.ToList()
                ;

            if (matchedRegex.Any())
            {
                if (matchedRegex.Count() == 1)
                {
                    ExecuteMatchHandler(matchedRegex.First(), currentHandle);
                }
                else if (matchedRegex.Count() > 1)
                {
                    ShowPopup(matchedRegex, currentHandle);
                }
            }
        }
    }

    private void ShowPopup(IEnumerable<KeyValuePair<AutoTypeCustomField, Cipher>> matchedRegex, IntPtr handle)
    {
        // Show the MatchSelectionWindow to let the user select the appropriate match
        var matchSelectionWindow = new MatchSelectionWindow(matchedRegex);

        var userResult = matchSelectionWindow.ShowDialog();

        if (userResult == true)
        {
            ExecuteMatchHandler(matchSelectionWindow.SelectedMatch!, handle);
        }
    }

    private async void ExecuteMatchHandler(KeyValuePair<AutoTypeCustomField, Cipher>? match, IntPtr handle)
    {
        var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        _ = WindowsDLLs.SetForegroundWindow(handle);

        try
        {
            await Task.Delay(TimeSpan.FromMilliseconds(300), tokenSource.Token); // in testing must sleep
            await ExecuteMatchFunctionAsync(match, tokenSource.Token);
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning($"{nameof(AutoTypeViewModel)}.{nameof(OnHotKeyHandler)}() {nameof(TaskCanceledException)}");
        }
        catch(Exception e)
        {
            _logger.LogError(e, $"{nameof(AutoTypeViewModel)}.{nameof(OnHotKeyHandler)}() Exception:'{e.Message}'");
        }
        finally
        {
            tokenSource?.Dispose();
        }

    }

    private async Task ExecuteMatchFunctionAsync(KeyValuePair<AutoTypeCustomField, Cipher>? match, CancellationToken token = default)
    {
        if (match is KeyValuePair<AutoTypeCustomField, Cipher> actualMatch)
        {
            var decryptionKey = _bitwardenService.GetDecryptionKey();

            Func<string, string?> func = (s) =>
            {
                return BitwardenCrypto.DecryptEntry(s, decryptionKey!, true);
            };

            await _autoTypeService.TypeSequenceAsync(actualMatch, func, token);
        }
    }

    #endregion OnHotKey pressed

    #region Helpers

    private static (IntPtr, Process) GetForegroundProcess()
    {
        IntPtr hWnd = WindowsDLLs.GetForegroundWindow();
        _ = WindowsDLLs.GetWindowThreadProcessId(hWnd, out uint processId);
        return (hWnd, Process.GetProcessById((int)processId));
    }

    private string GetWindowTitle(IntPtr hWnd)
    {
        const int maxTitleLength = 256;
        var titleBuilder = new StringBuilder(maxTitleLength);

        if (WindowsDLLs.GetWindowText(hWnd, titleBuilder, maxTitleLength) > 0)
        {
            return titleBuilder.ToString();
        }

        return string.Empty;
    }

    #endregion Helpers

    #region IDisposable

    public void Dispose()
    {
    }

    #endregion IDisposable
}