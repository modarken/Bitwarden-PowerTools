using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Windows;
using Bitwarden.AutoType.Desktop.Services;
using Bitwarden.AutoType.Desktop.Views;
using Bitwarden.AutoType.Desktop.Windows;
using Bitwarden.AutoType.Desktop.Windows.Native;
using Bitwarden.Core;
using Bitwarden.Core.Crypto;
using Bitwarden.Core.Models;
using Bitwarden.Utilities;
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

        System.Threading.Thread.Sleep(600);

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
        if (matchSelectionWindow.ShowDialog() == true)
        {
            // Restore the focus to the original foreground window
            _ = WindowsDLLs.SetForegroundWindow(handle);
            ExecuteMatchFunction(matchSelectionWindow.SelectedMatch!);
        }
    }

    #region IDisposable

    public void Dispose()
    {
    }

    #endregion IDisposable
}

#region Example Code to delete later

// save.Invoke(bitwardenClientConfiguration);

//var baseAddesss = bitwardenClientConfiguration.base_address;
//var email = bitwardenClientConfiguration.email;
//var clientID = bitwardenClientConfiguration.client_id;
//var clientSecret = bitwardenClientConfiguration.client_secret;
//var deviceName = bitwardenClientConfiguration.device_name;
//var deviceIdentifier = bitwardenClientConfiguration.device_identifier;
//var userName = email;
//var password = bitwardenClientConfiguration.master_key;
//var twoFactorToken = "123456";

//var preLogin = BitwardenProtocol.PostPreLogin(baseAddesss, email).GetAwaiter().GetResult();
//var accessToken = BitwardenProtocol.PostAccessTokenFromAPIKey(baseAddesss, clientID, clientSecret, deviceName, deviceIdentifier).GetAwaiter().GetResult();
//var accessToken2 = BitwardenProtocol.GetGetLoginAccessTokenFromPassword(baseAddesss, userName, password, deviceIdentifier, deviceName).GetAwaiter().GetResult();
//var accessToken3 = BitwardenProtocol.GetGetLoginAccessTokenFromPassword(baseAddesss, userName, password, deviceIdentifier, deviceName, twoFactorToken).GetAwaiter().GetResult();
//var bearerToken = accessToken!.access_token;
//ProfileResponse? profile = BitwardenProtocol.GetProfile(baseAddesss, bearerToken).GetAwaiter().GetResult();
//SyncResponse? syncResponse = BitwardenProtocol.GetSync(baseAddesss, bearerToken).GetAwaiter().GetResult();

//// 1. Get data from database
//// 2. Scan all entries for autofill entries
//// 3. Setup WindowWatcher
//// 4. upon ctrl-alt-a find window, find entries, use entry
//// 5. If multiple entires found, bring up select window

//// MasterKey

//// MasterPasswordHash

//// StretchedMasterKey
////  EncryptionKey
////  StretchedMasterKey MACKey

//var masterKey = BitwardenCrypto.DerriveMasterKey("p4ssw0rd", "nobody@example.com", 5000);
//var masterKeyB64 = Convert.ToBase64String(masterKey);
//var masterPasswordHash1 = BitwardenCrypto.DerriveMasterPasswordHash("p4ssw0rd", "nobody@example.com", 5000);
//var masterPasswordHash2 = BitwardenCrypto.DerriveMasterPasswordHashFromMasterKey(masterKey, Encoding.ASCII.GetBytes("p4ssw0rd"));

//var stetchedMasterKey = BitwardenCrypto.StretchKey(masterKey);
//var stetchedMasterKeyB64 = Convert.ToBase64String(stetchedMasterKey);

//var encryptionKey1 = BitwardenCrypto.DecryptEncryptionKey("2.uKntnrd31vxE2XptUOEJDw==|Bgtw3NbARqEvLGZhw4b0+oHUbO8s8KvGI7ISRj/HUpZd/pUyYwM03taCIXFgweOuf5TeS0shuya/L1XLpkkB24PPzd/SKVhwMD9E4XT8F6A=|VHOabQCGcZQiL9o5hWaoEp+ZxaHdIYGNPiNNjf6bakE=", masterKey);
//var plainText1 = BitwardenCrypto.DecryptEntry("2.1AALNIzzJor78bXpaQx1yw==|RBcOhPyFjew5J5kAm/a6kA==|TeFpTR+tppU2qFdM6gDUjgaqx57N8MxRzzp+IU/EYZg=", encryptionKey1, true);

//// r5CFRR+n9NQI8a525FY+0BPR0HGOjVJX0cR1KEMnIOo=
//var encryptionKey2 = BitwardenCrypto.DecryptEncryptionKey("0.uRcMe+Mc2nmOet4yWx9BwA==|PGQhpYUlTUq/vBEDj1KOHVMlTIH1eecMl0j80+Zu0VRVfFa7X/MWKdVM6OM/NfSZicFEwaLWqpyBlOrBXhR+trkX/dPRnfwJD2B93hnLNGQ=", masterKey);
//var plainText2 = BitwardenCrypto.DecryptEntry("2.6DmdNKlm3a+9k/5DFg+pTg==|7q1Arwz/ZfKEx+fksV3yo0HMQdypHJvyiix6hzgF3gY=|7lSXqjfq5rD3/3ofNZVpgv1ags696B2XXJryiGjDZvk=", encryptionKey2, true);

//var y = 5;

//var x = """
//    dfsdf
//    dsfsdf
//    sdfsdf

//    """;

//var key = MakePreloginKeyAsync(masterPassword, email);
//var hashedPassword = _cryptoService.HashPasswordAsync(masterPassword, key);

//TokenRequest request = new TokenRequest();
////if (twoFactorToken != null && twoFactorProvider != null)
////{
////    request = new TokenRequest(emailPassword, codeCodeVerifier, twoFactorProvider, twoFactorToken, remember,
////        captchaToken, deviceRequest);
////}
////else if (storedTwoFactorToken != null)
////{
////    request = new TokenRequest(emailPassword, codeCodeVerifier, TwoFactorProviderType.Remember,
////        storedTwoFactorToken, false, captchaToken, deviceRequest);
////}
////else if (authRequestId != null)
////{
////    request = new TokenRequest(emailPassword, null, null, null, false, null, deviceRequest, authRequestId);
////}
////else
////{
////    request = new TokenRequest(emailPassword, codeCodeVerifier, null, null, false, captchaToken, deviceRequest);
////}

//// https://bitwarden.home.mojo.systems/public/identity/connect/token
//request.client
//var requestMessage = new HttpRequestMessage
//{
//    Version = new Version(1, 0),
//    RequestUri = new Uri(string.Concat("https://bitwarden.home.mojo.systems/public/identity", " connect/token")),
//    Method = HttpMethod.Post,
//    Content = new FormUrlEncodedContent(request.ToIdentityToken(ClientType.Mobile.GetString()))
//};

#endregion