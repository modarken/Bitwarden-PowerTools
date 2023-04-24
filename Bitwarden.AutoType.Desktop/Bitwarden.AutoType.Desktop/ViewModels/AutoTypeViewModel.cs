using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
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

            if (matchedRegex.Count() == 1)
            {
                ExecuteMatchFunction(matchedRegex.First());
            }
            else if (matchedRegex.Count() > 1)
            {
                ShowPopup(matchedRegex, currentHandle);
            }
        }
    }

    private static (IntPtr, Process) GetForegroundProcess()
    {
        IntPtr hWnd = WindowsDLLs.GetForegroundWindow();
        _ = WindowsDLLs.GetWindowThreadProcessId(hWnd, out uint processId);
        return (hWnd, Process.GetProcessById((int)processId));
    }

    private void ExecuteMatchFunction(KeyValuePair<AutoTypeCustomField, Cipher>? match)
    {
        if (match is KeyValuePair<AutoTypeCustomField, Cipher> actualMatch)
        {
            var decryptionKey = _bitwardenService.GetDecryptionKey();

            Func<string, string?> func = (s) =>
            {
                return BitwardenCrypto.DecryptEntry(s, decryptionKey!, true);
            };

            _autoTypeService.TypeSequence(actualMatch, func);
        }
    }

    private void ShowPopup(IEnumerable<KeyValuePair<AutoTypeCustomField, Cipher>> matchedRegex, IntPtr handle)
    {
        // Show the MatchSelectionWindow to let the user select the appropriate match
        var matchSelectionWindow = new MatchSelectionWindow(matchedRegex);

        var userResult = matchSelectionWindow.ShowDialog();

        if (userResult == true)
        {
            _ = WindowsDLLs.SetForegroundWindow(handle);

            System.Threading.Thread.Sleep(400);

            // Restore the focus to the original foreground window
            ExecuteMatchFunction(matchSelectionWindow.SelectedMatch!);
        }
    }

    #region IDisposable

    public void Dispose()
    {
    }

    #endregion IDisposable
}