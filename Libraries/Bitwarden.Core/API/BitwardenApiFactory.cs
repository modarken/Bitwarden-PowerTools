using System.Text.Json;
using Refit;

namespace Bitwarden.Core.API;

/// <summary>
/// Factory for creating Refit-based IBitwardenApi instances.
/// Handles dynamic base URL configuration and SSL certificate bypass for self-hosted servers.
/// </summary>
public static class BitwardenApiFactory
{
    private static readonly object _lock = new();
    private static string? _currentBaseAddress;
    private static IBitwardenApi? _cachedApi;

    /// <summary>
    /// Gets or creates an IBitwardenApi instance for the specified base address.
    /// The instance is cached and reused if the base address hasn't changed.
    /// </summary>
    /// <param name="baseAddress">The base URL of the Bitwarden server (e.g., "https://vault.bitwarden.com")</param>
    /// <returns>A configured IBitwardenApi instance</returns>
    public static IBitwardenApi GetApi(string baseAddress)
    {
        lock (_lock)
        {
            if (_currentBaseAddress != baseAddress || _cachedApi == null)
            {
                _currentBaseAddress = baseAddress;
                _cachedApi = CreateApi(baseAddress);
            }
            return _cachedApi;
        }
    }

    /// <summary>
    /// Creates a new IBitwardenApi instance with the specified base address.
    /// Configures SSL bypass for self-hosted servers with self-signed certificates.
    /// </summary>
    private static IBitwardenApi CreateApi(string baseAddress)
    {
        // Create handler with SSL certificate bypass for self-hosted servers
        // This is necessary because many self-hosted Bitwarden instances use self-signed certificates
        var handler = new HttpClientHandler
        {
            ClientCertificateOptions = ClientCertificateOption.Manual,
            ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, certChain, policyErrors) => true
        };

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(baseAddress)
        };

        var settings = new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            })
        };

        return RestService.For<IBitwardenApi>(httpClient, settings);
    }

    /// <summary>
    /// Clears the cached API instance, forcing a new one to be created on next request.
    /// </summary>
    public static void ClearCache()
    {
        lock (_lock)
        {
            _currentBaseAddress = null;
            _cachedApi = null;
        }
    }
}
