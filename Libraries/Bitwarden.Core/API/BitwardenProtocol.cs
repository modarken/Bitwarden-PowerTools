/*
 * Most useful sites:
 * https://github.com/jcs/rubywarden/blob/master/API.md
 * https://docs.cozy.io/en/cozy-stack/bitwarden/
 * https://bitwarden.com/crypto.html
 * https://bitwarden.com/help/api/
 * https://bitwarden.com/help/vault-management-api/
 * https://bitwarden.com/help/public-api/
 * https://bitwarden.com/help/personal-api-key/
 *
*/

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Bitwarden.Core.Models;
using Refit;

namespace Bitwarden.Core.API;

/// <summary>
/// Static wrapper class for Bitwarden API calls.
/// Now uses Refit under the hood for cleaner HTTP communication.
/// The static methods are preserved for backward compatibility with existing code.
/// </summary>
public static class BitwardenProtocol
{
    #region Refit-based Implementation

    public static async Task<PreLoginResponse?> PostPreLogin(string baseAddress, string email)
    {
        try
        {
            var api = BitwardenApiFactory.GetApi(baseAddress);
            return await api.PostPreLoginAsync(new PreLoginRequest { Email = email }).ConfigureAwait(false);
        }
        catch (ApiException)
        {
            return null;
        }
    }

    public static async Task<TokenResponse?> PostAccessTokenFromAPIKey(string baseAddress, string clientID, string clientSecret, string deviceName = "", string deviceIdentifier = "")
    {
        try
        {
            var api = BitwardenApiFactory.GetApi(baseAddress);
            var content = new Dictionary<string, string>()
            {
                {"grant_type", "client_credentials"},
                {"scope", "api"},
                {"client_id", clientID},
                {"client_secret", clientSecret},
                {"device_type", "client"},
                {"device_name", deviceName},
                {"device_identifier", deviceIdentifier},
            };

            var response = await api.PostAccessTokenFromApiKeyAsync(content).ConfigureAwait(false);
            
            // Return content even on BadRequest (for error details like 2FA required)
            if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                return response.Content;
            }
            return null;
        }
        catch (ApiException)
        {
            return null;
        }
    }

    public static async Task<TokenResponse?> PostAccessTokenFromRefreshToken(string baseAddress, string refreshToken, string deviceName = "", string deviceIdentifier = "")
    {
        try
        {
            var api = BitwardenApiFactory.GetApi(baseAddress);
            var content = new Dictionary<string, string>()
            {
                {"grant_type", "refresh_token"},
                {"client_id", "browser"},
                {"refresh_token", refreshToken},
                {"device_name", deviceName},
                {"device_identifier", deviceIdentifier},
            };

            var response = await api.PostAccessTokenFromRefreshTokenAsync(content).ConfigureAwait(false);
            
            if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                return response.Content;
            }
            return null;
        }
        catch (ApiException)
        {
            return null;
        }
    }

    public static async Task<TokenResponse?> PostAccessTokenFromMasterPasswordHash(string baseAddress, string username, string masterPasswordHash, string deviceIdentifier = "",
        string deviceName = "")
    {
        try
        {
            var api = BitwardenApiFactory.GetApi(baseAddress);
            var content = new Dictionary<string, string>()
            {
                {"grant_type", "password"},
                {"scope", "api offline_access"},
                {"username", username},
                {"password", masterPasswordHash},
                {"client_id", "browser"},
                {"device_type", "6"}, // see https://github.com/bitwarden/server/blob/master/src/Core/Enums/DeviceType.cs
                {"device_identifier", deviceIdentifier},
                {"device_name", deviceName},
                {"devicePushToken", ""}
            };

            var response = await api.PostAccessTokenFromPasswordAsync(content).ConfigureAwait(false);
            
            if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                return response.Content;
            }
            return null;
        }
        catch (ApiException)
        {
            return null;
        }
    }

    public static async Task<TokenResponse?> PostAccessTokenFromMasterPasswordHash(string baseAddress, string username, string masterPasswordHash, string twoFactorToken, string deviceIdentifier = "",
        string deviceName = "")
    {
        try
        {
            var api = BitwardenApiFactory.GetApi(baseAddress);
            var content = new Dictionary<string, string>()
            {
                {"grant_type", "password"},
                {"scope", "api offline_access"},
                {"username", username},
                {"password", masterPasswordHash},
                {"client_id", "browser"},
                {"device_type", "6"}, // see https://github.com/bitwarden/server/blob/master/src/Core/Enums/DeviceType.cs
                {"device_identifier", deviceIdentifier},
                {"device_name", deviceName},
                {"devicePushToken", ""},
                {"twoFactorToken", twoFactorToken},
                {"twoFactorProvider", "0"},
                {"twoFactorRemember", "1"}
            };

            var response = await api.PostAccessTokenFromPasswordAsync(content).ConfigureAwait(false);
            
            if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                return response.Content;
            }
            return null;
        }
        catch (ApiException)
        {
            return null;
        }
    }

    public static async Task<string?> GetRevisionDate(string baseAddress, string? bearerToken)
    {
        try
        {
            var api = BitwardenApiFactory.GetApi(baseAddress);
            return await api.GetRevisionDateAsync($"Bearer {bearerToken}").ConfigureAwait(false);
        }
        catch (ApiException)
        {
            return null;
        }
    }

    public static async Task<ProfileResponse?> GetProfile(string baseAddress, string? bearerToken)
    {
        try
        {
            var api = BitwardenApiFactory.GetApi(baseAddress);
            return await api.GetProfileAsync($"Bearer {bearerToken}").ConfigureAwait(false);
        }
        catch (ApiException)
        {
            return null;
        }
    }

    public static async Task<SyncResponse?> GetSync(string baseAddress, string? bearerToken)
    {
        try
        {
            var api = BitwardenApiFactory.GetApi(baseAddress);
            return await api.GetSyncAsync($"Bearer {bearerToken}").ConfigureAwait(false);
        }
        catch (ApiException)
        {
            return null;
        }
    }

    #endregion

    #region Original Implementation (Kept for Reference)
    /*
     * ===================================================================================
     * ORIGINAL IMPLEMENTATION - COMMENTED OUT FOR REFERENCE
     * ===================================================================================
     * 
     * This code represents the original manual HTTP implementation before migrating to Refit.
     * It is kept here for several reasons:
     * 
     * 1. DEBUGGING: If issues arise with the Refit implementation, this code can be 
     *    quickly uncommented to compare behavior or as a fallback.
     * 
     * 2. UNDERSTANDING: New developers can see exactly what HTTP operations are being 
     *    performed, including headers, content types, and request construction.
     * 
     * 3. EDGE CASES: The original code shows how certain edge cases were handled 
     *    (e.g., BadRequest responses containing error details, SSL bypass).
     * 
     * 4. DOCUMENTATION: The commented code serves as documentation of the raw HTTP 
     *    protocol being used to communicate with the Bitwarden API.
     * 
     * To restore the original implementation:
     * 1. Uncomment this section
     * 2. Comment out or remove the Refit-based implementation above
     * 3. Remove the Refit package reference from the .csproj if desired
     * ===================================================================================
     */

    /*
    private static readonly SemaphoreSlim _httpClientSemaphore;
    private static string? _baseAddress;
    private static HttpClient? _httpClient;

    static BitwardenProtocol()
    {
        _httpClientSemaphore = new(1, 1);
        _baseAddress = null;
    }

    public static async Task<HttpResponseMessage> SendRequestAsync(string baseAddress, HttpRequestMessage request)
    {
        await _httpClientSemaphore.WaitAsync();
        try
        {
            if (_baseAddress != baseAddress)
            {
                _baseAddress = baseAddress;

                var handler = new HttpClientHandler();
                handler.ClientCertificateOptions = ClientCertificateOption.Manual;
                handler.ServerCertificateCustomValidationCallback =
                    (httpRequestMessage, cert, cetChain, policyErrors) =>
                    {
                        return true;
                    };

                _httpClient = new HttpClient(handler)
                {
                    BaseAddress = new System.Uri(baseAddress)
                };
            }

            var response = await _httpClient!.SendAsync(request).ConfigureAwait(false);
            return response;
        }
        finally
        {
            _httpClientSemaphore.Release();
        }
    }

    public static async Task<PreLoginResponse?> PostPreLogin(string baseAddress, string email)
    {
        var content =
            $$"""
            {
                "email": "{{email}}"
            }
            """;

        using HttpRequestMessage request = new(HttpMethod.Post, "identity/accounts/prelogin")
        { Content = new StringContent(content, Encoding.UTF8, "application/json") };

        var response = await SendRequestAsync(baseAddress, request).ConfigureAwait(false);

        if (response.IsSuccessStatusCode == false)
        {
            return null;
        }

        var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        return JsonSerializer.Deserialize(responseString, BitwardenModelsContext.Default.PreLoginResponse);
    }

    public static async Task<TokenResponse?> PostAccessTokenFromAPIKey(string baseAddress, string clientID, string clientSecret, string deviceName = "", string deviceIdentifier = "")
    {
        var content = new Dictionary<string, string>()
        {
            {"grant_type", "client_credentials"},
            {"scope", "api"},
            {"client_id", clientID},
            {"client_secret", clientSecret},
            {"device_type", "client"},
            {"device_name", deviceName},
            {"device_identifier", deviceIdentifier},
        };

        using HttpRequestMessage request = new(HttpMethod.Post, "identity/connect/token")
        { Content = new FormUrlEncodedContent(content) };

        var response = await SendRequestAsync(baseAddress, request).ConfigureAwait(false);

        if (response.IsSuccessStatusCode == false)
        {
            return null;
        }

        var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            return JsonSerializer.Deserialize(responseString, BitwardenModelsContext.Default.TokenResponse);
        }
        return null;
    }

    public static async Task<TokenResponse?> PostAccessTokenFromRefreshToken(string baseAddress, string refreshToken, string deviceName = "", string deviceIdentifier = "")
    {
        var content = new Dictionary<string, string>()
        {
            {"grant_type", "refresh_token"},
            {"client_id", "browser"},
            {"refresh_token", refreshToken},
            {"device_name", deviceName},
            {"device_identifier", deviceIdentifier},
        };

        using HttpRequestMessage request = new(HttpMethod.Post, "identity/connect/token")
        { Content = new FormUrlEncodedContent(content) };

        var response = await SendRequestAsync(baseAddress, request).ConfigureAwait(false);

        if (response.IsSuccessStatusCode == false)
        {
            return null;
        }

        var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            return JsonSerializer.Deserialize(responseString, BitwardenModelsContext.Default.TokenResponse);
        }
        return null;
    }

    public static async Task<TokenResponse?> PostAccessTokenFromMasterPasswordHash(string baseAddress, string username, string masterPasswordHash, string deviceIdentifier = "",
        string deviceName = "")
    {
        var content = new Dictionary<string, string>()
        {
            {"grant_type", "password"},
            {"scope", "api offline_access"},
            {"username", username},
            {"password", masterPasswordHash},
            {"client_id", "browser"},
            {"device_type", "6"}, // see https://github.com/bitwarden/server/blob/master/src/Core/Enums/DeviceType.cs
            {"device_identifier", deviceIdentifier},
            {"device_name", deviceName},
            {"devicePushToken", ""}
        };

        using HttpRequestMessage request = new(HttpMethod.Post, "identity/connect/token")
        { Content = new FormUrlEncodedContent(content) };

        var response = await SendRequestAsync(baseAddress, request).ConfigureAwait(false);

        if (response.IsSuccessStatusCode == false)
        {
            return null;
        }

        var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            return JsonSerializer.Deserialize(responseString, BitwardenModelsContext.Default.TokenResponse);
        }

        return null;
    }

    public static async Task<TokenResponse?> PostAccessTokenFromMasterPasswordHash(string baseAddress, string username, string masterPasswordHash, string twoFactorToken, string deviceIdentifier = "",
        string deviceName = "")
    {
        var content = new Dictionary<string, string>()
        {
            {"grant_type", "password"},
            {"scope", "api offline_access"},
            {"username", username},
            {"password", masterPasswordHash},
            {"client_id", "browser"},
            {"device_type", "6"}, // see https://github.com/bitwarden/server/blob/master/src/Core/Enums/DeviceType.cs
            {"device_identifier", deviceIdentifier},
            {"device_name", deviceName},
            {"devicePushToken", ""},
            {"twoFactorToken", twoFactorToken},
            {"twoFactorProvider", "0"},
            {"twoFactorRemember", "1"}
        };

        using HttpRequestMessage request = new(HttpMethod.Post, "identity/connect/token")
        { Content = new FormUrlEncodedContent(content) };

        var response = await SendRequestAsync(baseAddress, request).ConfigureAwait(false);

        if (response.IsSuccessStatusCode == false)
        {
            return null;
        }

        var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            return JsonSerializer.Deserialize(responseString, BitwardenModelsContext.Default.TokenResponse);
        }

        return null;
    }

    public static async Task<string?> GetRevisionDate(string baseAddress, string? bearerToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, "api/accounts/revision-date");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        var response = await SendRequestAsync(baseAddress, request).ConfigureAwait(false);

        if (response.IsSuccessStatusCode == false)
        {
            return null;
        }

        var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        return responseString;
    }

    public static async Task<ProfileResponse?> GetProfile(string baseAddress, string? bearerToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, "api/accounts/profile");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        var response = await SendRequestAsync(baseAddress, request).ConfigureAwait(false);

        if (response.IsSuccessStatusCode == false)
        {
            return null;
        }

        var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        return JsonSerializer.Deserialize<ProfileResponse>(responseString, options);
    }

    public static async Task<SyncResponse?> GetSync(string baseAddress, string? bearerToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, "api/sync");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        var response = await SendRequestAsync(baseAddress, request).ConfigureAwait(false);

        if (response.IsSuccessStatusCode == false)
        {
            return null;
        }

        var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        return JsonSerializer.Deserialize<SyncResponse>(responseString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }
    */

    #endregion
}