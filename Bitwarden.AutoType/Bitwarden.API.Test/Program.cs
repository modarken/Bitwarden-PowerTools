// See https://aka.ms/new-console-template for more information

#pragma warning disable

using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Bitwarden.Core.API;
using Bitwarden.Core.Crypto;
using Bitwarden.Core.Models;
using Bitwarden.Utilities;

Console.WriteLine("Hello, World!");

// TODO: Consider adding revision date check (GET /bitwarden/api/accounts/revision-date)
// TODO: Consider getting ciphers instead of sync (GET /bitwarden/api/ciphers)
// Test1();

var config = LoadConfig();
var tokenResponse = TestViaAPIKey(config);
// var tokenResponse = TestViaPasswordAndTOTP(config);
// var tokenResponse = TestViaRefreshToken(config);
// TestUseAccessToken(config, tokenResponse);
TestUseAccessTokenPasswordFromConfig(config, tokenResponse);

void Test1()
{
    // https://bitwarden.com/crypto.html

    var userName = "nobody@example.com";
    var masterPassword = "p4ssw0rd";
    var iterations = 5000;

    // used as the client side main secret. When hashed it can be sent to server for login
    // it can also be used to decrypt the vault (server side) protectedSymmetricKey which is generated on the server and used to (d)encrypt ciphers.
    var masterKey = BitwardenCrypto.DeriveMasterKey(masterPassword, userName, iterations);
    var masterKeyBase64 = Convert.ToBase64String(masterKey);
    var masterPasswordHash1 = BitwardenCrypto.DeriveMasterPasswordHash(masterPassword, userName, iterations);
    var masterPasswordHash2 = BitwardenCrypto.DeriveMasterPasswordHashFromMasterKey(masterKey, Encoding.ASCII.GetBytes(masterPassword));

    var stretchedmasterkey = BitwardenCrypto.StretchKey(masterKey);
    var stretchedmasterkeyBase64 = Convert.ToBase64String(stretchedmasterkey);

    // TODO: Replace with your actual protectedSymmetricKey from the server response
    var protectedSymmetricKey = "2.EXAMPLE_ENCRYPTED_KEY==|EXAMPLE_DATA|EXAMPLE_MAC=";

    // TODO: Replace with your actual encrypted cipher string
    var cipherString = "2.EXAMPLE_ENCRYPTED_DATA==|EXAMPLE_CIPHER|EXAMPLE_MAC=";

    var unprotectedSymmetricKey0 = BitwardenCrypto.DecryptEncryptionKey(protectedSymmetricKey, masterKey);
    var unprotectedSymmetricKey0Base64 = Convert.ToBase64String(unprotectedSymmetricKey0);
    var plainText0 = BitwardenCrypto.DecryptEntry(cipherString, unprotectedSymmetricKey0, true);

    var unprotectedSymmetricKey1 = BitwardenCrypto.DecryptEncryptionKey("2.EXAMPLE_KEY2==|EXAMPLE_DATA2|EXAMPLE_MAC2=", masterKey);
    var unprotectedSymmetricKey1Base64 = Convert.ToBase64String(unprotectedSymmetricKey1);
    var plainText1 = BitwardenCrypto.DecryptEntry("2.EXAMPLE_CIPHER2==|EXAMPLE_DATA2|EXAMPLE_MAC2=", unprotectedSymmetricKey1, true);
}

BitwardenClientConfiguration LoadConfig()
{
    var serializerOptions = new JsonSerializerOptions() { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
    var dataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), DesktopConstants.DefaultDataFolderName);
    var fileName = "client.settings.json";
    var fullPath = Path.Combine(dataPath, fileName);
    var json = File.ReadAllText(fullPath, Encoding.UTF8);
    BitwardenClientConfiguration? bitwardenClientConfiguration;
    if (JsonSerializer.Deserialize<BitwardenClientConfiguration>(json, serializerOptions) is BitwardenClientConfiguration t)
    {
        //bitwardenClientConfiguration = t.BitwardenClientConfiguration;
    }
    else
    {
        throw new Exception("Unable to load instance from file");
    }
    return t;
}

TokenResponse TestViaAPIKey(BitwardenClientConfiguration bitwardenClientConfiguration)
{
    var baseAddesss = bitwardenClientConfiguration.base_address;
    var email = bitwardenClientConfiguration.email;

    var clientID = bitwardenClientConfiguration.client_id;
    var clientSecret = bitwardenClientConfiguration.client_secret;

    var deviceName = bitwardenClientConfiguration.device_name;
    var deviceIdentifier = bitwardenClientConfiguration.device_identifier;
    var accessToken = BitwardenProtocol.PostAccessTokenFromAPIKey(baseAddesss, clientID, clientSecret, deviceName, deviceIdentifier).GetAwaiter().GetResult();

    return accessToken;
}

TokenResponse TestViaPasswordAndTOTP(BitwardenClientConfiguration bitwardenClientConfiguration)
{
    var baseAddesss = bitwardenClientConfiguration.base_address;
    var email = bitwardenClientConfiguration.email;

    var clientID = bitwardenClientConfiguration.client_id;
    var clientSecret = bitwardenClientConfiguration.client_secret;

    // var masterkey = bitwardenClientConfiguration.master_key; //password -> PBKDF2 10,000 iterations -> master key
    // var twoFactorToken = "123456";

    var preLogin = BitwardenProtocol.PostPreLogin(baseAddesss, email).GetAwaiter().GetResult();

    Console.WriteLine("Enter master password");

    // We want to get the master password hash, this is whats sent to the server and checked for valid server access to get a token
    var masterPassword = GetPassword().ToUnsecureString();
    var masterKey = BitwardenCrypto.DeriveMasterKey(masterPassword, email, preLogin.KdfIterations);
    var masterKeyBase64 = Convert.ToBase64String(masterKey);
    var masterPasswordHash = BitwardenCrypto.DeriveMasterPasswordHashFromMasterKey(masterKey, Encoding.ASCII.GetBytes(masterPassword));

    // Three methods to get an access token
    // var accessToken = BitwardenProtocol.PostAccessTokenFromAPIKey(baseAddesss, clientID, clientSecret, deviceName, deviceIdentifier).GetAwaiter().GetResult();
    // var accessToken2 = BitwardenProtocol.PostAccessTokenFromMasterPasswordHash(baseAddesss, userName, password, deviceIdentifier, deviceName).GetAwaiter().GetResult();
    // var accessToken3 = BitwardenProtocol.PostAccessTokenFromMasterPasswordHash(baseAddesss, userName, password, deviceIdentifier, deviceName, twoFactorToken).GetAwaiter().GetResult();

    // If TOTP is setup, must use TOTP token
    Console.WriteLine("Enter TOTP password");
    var twoFactorToken = GetPassword().ToUnsecureString();
    var deviceName = bitwardenClientConfiguration.device_name;
    var deviceIdentifier = bitwardenClientConfiguration.device_identifier;
    var accessToken = BitwardenProtocol.PostAccessTokenFromMasterPasswordHash(baseAddesss, email, masterPasswordHash, deviceIdentifier, deviceName, twoFactorToken).GetAwaiter().GetResult();

    return accessToken;
}

TokenResponse TestViaRefreshToken(BitwardenClientConfiguration bitwardenClientConfiguration)
{
    var baseAddesss = bitwardenClientConfiguration.base_address;
    var email = bitwardenClientConfiguration.email;

    var refreshToken = bitwardenClientConfiguration.refresh_token;

    var deviceName = bitwardenClientConfiguration.device_name;
    var deviceIdentifier = bitwardenClientConfiguration.device_identifier;
    var accessToken = BitwardenProtocol.PostAccessTokenFromRefreshToken(baseAddesss, refreshToken, deviceName, deviceIdentifier).GetAwaiter().GetResult();

    return accessToken;
}

void TestUseAccessToken(BitwardenClientConfiguration config, TokenResponse tokenResponse)
{

    if (tokenResponse?.access_token is not null)
    {
        // Now that I have a working access token, run some API Requests such as
        // Profile  https://vault.bitwarden.com/api/accounts/profile
        // Sync     https://vault.bitwarden.com/api/sync
        //return accessToken;

        var bearerToken = tokenResponse!.access_token!;
        ProfileResponse? profile = BitwardenProtocol.GetProfile(config!.base_address!, bearerToken).GetAwaiter().GetResult();

        SyncResponse? syncResponse = BitwardenProtocol.GetSync(config!.base_address!, bearerToken).GetAwaiter().GetResult();

        if (syncResponse != null)
        {
            Console.WriteLine("Enter master password");
            // need to encryption decryption key
            var masterPassword = GetPassword().ToUnsecureString();
            var masterKey = BitwardenCrypto.DeriveMasterKey(masterPassword, config!.email!, tokenResponse.KdfIterations);

            // the encryption key is stored on the server and is not to encrypt/decrypt all of the cipher text.
            var protectedEncyptionKey = tokenResponse.Key;
            var encryptionKey = BitwardenCrypto.DecryptEncryptionKey(protectedEncyptionKey, masterKey);

            foreach (var item in syncResponse!.Ciphers!)
            {
                // need to dec

                var plainText = BitwardenCrypto.DecryptEntry(item.Name, encryptionKey, true);

                Console.WriteLine(plainText);
            }
        }
    }
}

void TestUseAccessTokenPasswordFromConfig(BitwardenClientConfiguration config, TokenResponse tokenResponse)
{

    if (tokenResponse?.access_token is not null)
    {
        // Now that I have a working access token, run some API Requests such as
        // Profile  https://vault.bitwarden.com/api/accounts/profile
        // Sync     https://vault.bitwarden.com/api/sync
        //return accessToken;

        var bearerToken = tokenResponse!.access_token!;
        ProfileResponse? profile = BitwardenProtocol.GetProfile(config!.base_address!, bearerToken).GetAwaiter().GetResult();

        SyncResponse? syncResponse = BitwardenProtocol.GetSync(config!.base_address!, bearerToken).GetAwaiter().GetResult();

        if (syncResponse != null)
        {
            // need to encryption decryption key
            // var masterPassword = GetPassword().ToUnsecureString();
            // var masterKey = BitwardenCrypto.DerriveMasterKey(masterPassword, config!.email!, tokenResponse.KdfIterations);
            // var masterKey =  Convert.FromBase64String(config.master_key);

            // the encryption key is stored on the server and is not to encrypt/decrypt all of the cipher text.
            // var protectedEncyptionKey = tokenResponse.Key;
            // var encryptionKey = BitwardenCrypto.DecryptEncryptionKey(protectedEncyptionKey, masterKey);

            var encryptionKey = Convert.FromBase64String(config.encryption_key);

            foreach (var item in syncResponse!.Ciphers!)
            {
                // need to dec

                var plainText = BitwardenCrypto.DecryptEntry(item.Name, encryptionKey, true);

                Console.WriteLine(plainText);
            }
        }
    }
}

#region Support

static SecureString GetPassword()
{
    var pwd = new SecureString();
    while (true)
    {
        ConsoleKeyInfo i = Console.ReadKey(true);
        if (i.Key == ConsoleKey.Enter)
        {
            break;
        }
        else if (i.Key == ConsoleKey.Backspace)
        {
            if (pwd.Length > 0)
            {
                pwd.RemoveAt(pwd.Length - 1);
                Console.Write("\b \b");
            }
        }
        else if (i.KeyChar != '\u0000') // KeyChar == '\u0000' if the key pressed does not correspond to a printable character, e.g. F1, Pause-Break, etc
        {
            pwd.AppendChar(i.KeyChar);
            Console.Write("*");
        }
    }
    return pwd;
}

public static class DesktopConstants
{
    public static readonly string DefaultDataFolderName = "Bitwarden-PowerTools";
}

internal static class Helpers
{
    public static string ToUnsecureString(this SecureString secureString)
    {
        if (secureString == null) return null;
        var unmanagedString = IntPtr.Zero;
        try
        {
            unmanagedString = Marshal.SecureStringToGlobalAllocUnicode(secureString);
            return Marshal.PtrToStringUni(unmanagedString);
        }
        finally
        {
            Marshal.ZeroFreeGlobalAllocUnicode(unmanagedString);
        }
    }
}

#endregion Support

#pragma warning restore
