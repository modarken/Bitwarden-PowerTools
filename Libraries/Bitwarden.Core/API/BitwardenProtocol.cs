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

namespace Bitwarden.Core.API;

public static class BitwardenProtocol
{
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

                //_httpClient = new HttpClient()
                //{
                //    BaseAddress = new System.Uri(baseAddress)
                //};
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
        // using var httpClient = new HttpClient() { BaseAddress = new System.Uri(baseAddress) };

        // _httpClient.BaseAddress = new System.Uri(baseAddress);
        // var response = await _httpClient.SendAsync(request).ConfigureAwait(false);
        var response = await SendRequestAsync(baseAddress, request).ConfigureAwait(false);

        if (response.IsSuccessStatusCode == false)
        {
            return null;
        }

        var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        // response.EnsureSuccessStatusCode();

        if (response.IsSuccessStatusCode)
        {
            return JsonSerializer.Deserialize(responseString, BitwardenModelsContext.Default.PreLoginResponse);
        }
        return null;
    }

    public static async Task<TokenResponse?> PostAccessTokenFromAPIKey(string baseAddress, string clientID, string clientSecret, string deviceName = "", string deviceIdentifier = "")
    {
        ;
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
        // using var httpClient = new HttpClient() { BaseAddress = new System.Uri(baseAddress) };

        // _httpClient.BaseAddress = new System.Uri(baseAddress);
        // var response = await _httpClient.SendAsync(request).ConfigureAwait(false);
        var response = await SendRequestAsync(baseAddress, request).ConfigureAwait(false);

        if (response.IsSuccessStatusCode == false)
        {
            return null;
        }

        var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        // response.EnsureSuccessStatusCode();

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
        // using var httpClient = new HttpClient() { BaseAddress = new System.Uri(baseAddress) };

        // _httpClient.BaseAddress = new System.Uri(baseAddress);
        // var response = await _httpClient.SendAsync(request).ConfigureAwait(false);
        var response = await SendRequestAsync(baseAddress, request).ConfigureAwait(false);

        if (response.IsSuccessStatusCode == false)
        {
            return null;
        }

        var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        // response.EnsureSuccessStatusCode();

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
        // using var httpClient = new HttpClient() { BaseAddress = new System.Uri(baseAddress) };

        // _httpClient.BaseAddress = new System.Uri(baseAddress);
        // var response = await _httpClient.SendAsync(request).ConfigureAwait(false);
        var response = await SendRequestAsync(baseAddress, request).ConfigureAwait(false);

        if (response.IsSuccessStatusCode == false)
        {
            return null;
        }

        var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        // response.EnsureSuccessStatusCode();

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
        // using var httpClient = new HttpClient() { BaseAddress = new System.Uri(baseAddress) };

        // _httpClient.BaseAddress = new System.Uri(baseAddress);
        // var response = await _httpClient.SendAsync(request).ConfigureAwait(false);
        var response = await SendRequestAsync(baseAddress, request).ConfigureAwait(false);

        if (response.IsSuccessStatusCode == false)
        {
            return null;
        }

        var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        // response.EnsureSuccessStatusCode();

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
        // using var httpClient = new HttpClient() { BaseAddress = new System.Uri(baseAddress) };

        // _httpClient.BaseAddress = new System.Uri(baseAddress);
        // var response = await _httpClient.SendAsync(request).ConfigureAwait(false);
        var response = await SendRequestAsync(baseAddress, request).ConfigureAwait(false);

        if (response.IsSuccessStatusCode == false)
        {
            return null;
        }

        var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        if (response.IsSuccessStatusCode)
        {
            return responseString;
        }

        return null;
    }

    public static async Task<ProfileResponse?> GetProfile(string baseAddress, string? bearerToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, "api/accounts/profile");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        // using var httpClient = new HttpClient() { BaseAddress = new System.Uri(baseAddress) };

        // _httpClient.BaseAddress = new System.Uri(baseAddress);
        // var response = await _httpClient.SendAsync(request).ConfigureAwait(false);
        var response = await SendRequestAsync(baseAddress, request).ConfigureAwait(false);

        if (response.IsSuccessStatusCode == false)
        {
            return null;
        }

        var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        if (response.IsSuccessStatusCode)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            // return JsonSerializer.Deserialize(responseString, BitwardenModelsContext.Default.ProfileResponse);
            return JsonSerializer.Deserialize<ProfileResponse>(responseString, options);
        }

        return null;
    }

    public static async Task<SyncResponse?> GetSync(string baseAddress, string? bearerToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, "api/sync");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        // using var httpClient = new HttpClient() { BaseAddress = new System.Uri(baseAddress) };

        // _httpClient.BaseAddress = new System.Uri(baseAddress);
        // var response = await _httpClient.SendAsync(request).ConfigureAwait(false);
        var response = await SendRequestAsync(baseAddress, request).ConfigureAwait(false);

        if (response.IsSuccessStatusCode == false)
        {
            return null;
        }

        var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        if (response.IsSuccessStatusCode)
        {
            // return JsonSerializer.Deserialize(responseString, BitwardenModelsContext.Default.SyncResponse);
            return JsonSerializer.Deserialize<SyncResponse>(responseString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        return null;
    }
}