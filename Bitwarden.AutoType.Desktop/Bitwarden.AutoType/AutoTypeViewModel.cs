using System.Text;
using Bitwarden.Core;
using Bitwarden.Core.API;
using Bitwarden.Core.Crypto;
using Bitwarden.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Bitwarden.AutoType;

//internal class Person
//{
//    public string FirstName { get; set; }
//    public string LastName { get; set; }
//}

//[JsonSerializable(typeof(Person))]
//internal partial class MyJsonContext : JsonSerializerContext
//{
//}

[INotifyPropertyChanged]
public partial class AutoTypeViewModel : IDisposable
{
    public AutoTypeViewModel(BitwardenClientConfiguration bitwardenClientConfiguration)
    {
        var baseAddesss = bitwardenClientConfiguration.base_address;
        var email = bitwardenClientConfiguration.email;
        var clientID = bitwardenClientConfiguration.client_id;
        var clientSecret = bitwardenClientConfiguration.client_secret;
        var deviceName = bitwardenClientConfiguration.device_name;
        var deviceIdentifier = bitwardenClientConfiguration.device_identifier;
        var userName = email;
        var password = bitwardenClientConfiguration.master_key;
        var twoFactorToken = "123456";

        var preLogin = BitwardenProtocol.GetPreLogin(baseAddesss, email).GetAwaiter().GetResult();
        var accessToken = BitwardenProtocol.GetGetLoginAccessToken(baseAddesss, clientID, clientSecret, deviceName, deviceIdentifier).GetAwaiter().GetResult();
        var accessToken2 = BitwardenProtocol.GetGetLoginAccessTokenFromPassword(baseAddesss, userName, password, deviceIdentifier, deviceName).GetAwaiter().GetResult();
        var accessToken3 = BitwardenProtocol.GetGetLoginAccessTokenFromPassword(baseAddesss, userName, password, deviceIdentifier, deviceName, twoFactorToken).GetAwaiter().GetResult();
        var bearerToken = accessToken!.access_token;
        ProfileResponse? profile = BitwardenProtocol.GetProfile(baseAddesss, bearerToken).GetAwaiter().GetResult();
        SyncResponse? syncResponse = BitwardenProtocol.GetSync(baseAddesss, bearerToken).GetAwaiter().GetResult();

        // 1. Get data from database
        // 2. Scan all entries for autofill entries
        // 3. Setup WindowWatcher
        // 4. upon ctrl-alt-a find window, find entries, use entry
        // 5. If multiple entires found, bring up select window

        // MasterKey

        // MasterPasswordHash

        // StretchedMasterKey
        //  EncryptionKey
        //  StretchedMasterKey MACKey

        var masterKey = BitwardenCrypto.DerriveMasterKey("p4ssw0rd", "nobody@example.com", 5000);
        var masterKeyB64 = Convert.ToBase64String(masterKey);
        var masterPasswordHash1 = BitwardenCrypto.DerriveMasterPasswordHash("p4ssw0rd", "nobody@example.com", 5000);
        var masterPasswordHash2 = BitwardenCrypto.DerriveMasterPasswordHashFromMasterKey(masterKey, Encoding.ASCII.GetBytes("p4ssw0rd"));

        var stetchedMasterKey = BitwardenCrypto.StretchKey(masterKey);
        var stetchedMasterKeyB64 = Convert.ToBase64String(stetchedMasterKey);

        var encryptionKey1 = BitwardenCrypto.DecryptEncryptionKey("2.uKntnrd31vxE2XptUOEJDw==|Bgtw3NbARqEvLGZhw4b0+oHUbO8s8KvGI7ISRj/HUpZd/pUyYwM03taCIXFgweOuf5TeS0shuya/L1XLpkkB24PPzd/SKVhwMD9E4XT8F6A=|VHOabQCGcZQiL9o5hWaoEp+ZxaHdIYGNPiNNjf6bakE=", masterKey);
        var plainText1 = BitwardenCrypto.DecryptEntry("2.1AALNIzzJor78bXpaQx1yw==|RBcOhPyFjew5J5kAm/a6kA==|TeFpTR+tppU2qFdM6gDUjgaqx57N8MxRzzp+IU/EYZg=", encryptionKey1, true);

        // r5CFRR+n9NQI8a525FY+0BPR0HGOjVJX0cR1KEMnIOo=
        var encryptionKey2 = BitwardenCrypto.DecryptEncryptionKey("0.uRcMe+Mc2nmOet4yWx9BwA==|PGQhpYUlTUq/vBEDj1KOHVMlTIH1eecMl0j80+Zu0VRVfFa7X/MWKdVM6OM/NfSZicFEwaLWqpyBlOrBXhR+trkX/dPRnfwJD2B93hnLNGQ=", masterKey);
        var plainText2 = BitwardenCrypto.DecryptEntry("2.6DmdNKlm3a+9k/5DFg+pTg==|7q1Arwz/ZfKEx+fksV3yo0HMQdypHJvyiix6hzgF3gY=|7lSXqjfq5rD3/3ofNZVpgv1ags696B2XXJryiGjDZvk=", encryptionKey2, true);

        var y = 5;

        var x = """
            dfsdf
            dsfsdf
            sdfsdf

            """;

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
    }

    #region Bound Properties

    [ObservableProperty]
    private bool _isAutoTypeEnabled = false;

    partial void OnIsAutoTypeEnabledChanged(bool value)
    {
    }

    #endregion Bound Properties

    #region IDisposable

    public void Dispose()
    {
    }

    #endregion IDisposable
}

public class BitwardenVaultContext
{
}

// https://bitwarden.com/help/bitwarden-apis/
// https://bitwarden.com/help/vault-management-api/
// https://bitwarden.com/help/cli/#serve

public static class BiwardenVaultAPI
{
}