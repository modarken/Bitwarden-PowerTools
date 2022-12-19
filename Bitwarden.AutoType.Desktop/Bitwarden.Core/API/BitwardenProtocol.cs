using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Bitwarden.Core.Models;

namespace Bitwarden.Core.API
{
    public static class BitwardenProtocol
    {
        public static async Task<PreLoginResponse?> GetPreLogin(string baseAddesss, string email)
        {
            var content = $$"""
                {
                    "email": "{{email}}"
                }
                """;

            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "identity/accounts/prelogin")
            { Content = new StringContent(content, Encoding.UTF8, "application/json") };
            using var httpClient = new HttpClient() { BaseAddress = new System.Uri(baseAddesss) };

            var response = await httpClient.SendAsync(request).ConfigureAwait(false);
            var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            // response.EnsureSuccessStatusCode();

            if (response.IsSuccessStatusCode)
            {
                return JsonSerializer.Deserialize(responseString, BitwardenModelsContext.Default.PreLoginResponse);
            }
            return null;
        }

        public static async Task<TokenResponse?> GetGetLoginAccessToken(string baseAddesss, string cliendID, string clientSecret, string deviceName, string deviceIdentifier)
        {
            ;
            var content = new Dictionary<string, string>()
            {
                {"grant_type", "client_credentials"},
                {"scope", "api"},
                {"client_id", cliendID},
                {"client_secret", clientSecret},
                {"device_name", deviceName},
                {"device_identifier", deviceIdentifier},
            };

            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "identity/connect/token")
            { Content = new FormUrlEncodedContent(content) };
            using var httpClient = new HttpClient() { BaseAddress = new System.Uri(baseAddesss) };

            var response = await httpClient.SendAsync(request).ConfigureAwait(false);
            var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            // response.EnsureSuccessStatusCode();

            if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                return JsonSerializer.Deserialize(responseString, BitwardenModelsContext.Default.TokenResponse);
            }
            return null;
        }

        public static async Task<TokenResponse?> GetGetLoginAccessToken(string baseAddesss, string username, string password, string clientID,
            string deviceType, string deviceIdentifier, string deviceName)
        {
            var content = new Dictionary<string, string>()
            {
                {"grant_type", "password"},
                {"scope", "api offline_access"},
                {"username", username},
                {"password", password},
                {"client_id", clientID},
                {"device_type", deviceType},
                {"device_identifier", deviceIdentifier},
                {"device_name", deviceName},
                {"devicePushToken", ""}
            };

            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "identity/connect/token")
            { Content = new FormUrlEncodedContent(content) };
            using var httpClient = new HttpClient() { BaseAddress = new System.Uri(baseAddesss) };

            var response = await httpClient.SendAsync(request).ConfigureAwait(false);
            var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            // response.EnsureSuccessStatusCode();

            if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                return JsonSerializer.Deserialize(responseString, BitwardenModelsContext.Default.TokenResponse);
            }

            return null;
        }

        public static async Task<TokenResponse?> GetGetLoginAccessToken(string baseAddesss, string username, string password, string clientID, string deviceType
            , string deviceIdentifier, string deviceName, string twoFactorToken)
        {
            var content = new Dictionary<string, string>()
            {
                {"grant_type", "password"},
                {"scope", "api offline_access"},
                {"username", username},
                {"password", password},
                {"client_id", clientID},
                {"device_type", deviceType},
                {"device_identifier", deviceIdentifier},
                {"device_name", deviceName},
                {"devicePushToken", ""},
                {"twoFactorToken", twoFactorToken},
                {"twoFactorProvider", "0"},
                {"twoFactorRemember", "1"}
            };

            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "identity/connect/token")
            { Content = new FormUrlEncodedContent(content) };
            using var httpClient = new HttpClient() { BaseAddress = new System.Uri(baseAddesss) };

            var response = await httpClient.SendAsync(request).ConfigureAwait(false);
            var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            // response.EnsureSuccessStatusCode();

            if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                return JsonSerializer.Deserialize(responseString, BitwardenModelsContext.Default.TokenResponse);
            }

            return null;
        }

        public static async Task<ProfileResponse?> GetProfile(string baseAddesss, string? bearerToken)
        {
            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "api/accounts/profile");
            using var httpClient = new HttpClient() { BaseAddress = new System.Uri(baseAddesss) };
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

            //httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {bearerToken}");
            //httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            //httpClient.DefaultRequestHeaders.Add("Content-Type", "application/json");
            //httpClient.DefaultRequestHeaders.Add("Content-Length", content.Length.ToString());

            var response = await httpClient.SendAsync(request).ConfigureAwait(false);
            var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            // response.EnsureSuccessStatusCode();

            if (response.IsSuccessStatusCode)
            {
                return JsonSerializer.Deserialize(responseString, BitwardenModelsContext.Default.ProfileResponse);
            }

            return null;
        }

        public static async Task<SyncResponse?> GetSync(string baseAddesss, string? bearerToken)
        {
            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "api/sync");
            using var httpClient = new HttpClient() { BaseAddress = new System.Uri(baseAddesss) };
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

            //httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {bearerToken}");
            //httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            //httpClient.DefaultRequestHeaders.Add("Content-Type", "application/json");
            //httpClient.DefaultRequestHeaders.Add("Content-Length", content.Length.ToString());

            var response = await httpClient.SendAsync(request).ConfigureAwait(false);
            var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            // response.EnsureSuccessStatusCode();

            if (response.IsSuccessStatusCode)
            {
                return JsonSerializer.Deserialize(responseString, BitwardenModelsContext.Default.SyncResponse);
            }

            return null;
        }
    }
}