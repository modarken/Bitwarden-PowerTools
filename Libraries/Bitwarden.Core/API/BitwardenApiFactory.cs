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
    private static bool _currentAllowInvalidCertificates;
    private static IBitwardenApi? _cachedApi;

    /// <summary>
    /// Gets or creates an IBitwardenApi instance for the specified base address.
    /// The instance is cached and reused if the base address and SSL settings haven't changed.
    /// </summary>
    /// <param name="baseAddress">The base URL of the Bitwarden server (e.g., "https://vault.bitwarden.com")</param>
    /// <param name="allowInvalidCertificates">Whether to allow invalid/self-signed SSL certificates (default: true)</param>
    /// <returns>A configured IBitwardenApi instance</returns>
    public static IBitwardenApi GetApi(string baseAddress, bool allowInvalidCertificates = true)
    {
        lock (_lock)
        {
            if (_currentBaseAddress != baseAddress || _currentAllowInvalidCertificates != allowInvalidCertificates || _cachedApi == null)
            {
                _currentBaseAddress = baseAddress;
                _currentAllowInvalidCertificates = allowInvalidCertificates;
                _cachedApi = CreateApi(baseAddress, allowInvalidCertificates);
            }
            return _cachedApi;
        }
    }

    /// <summary>
    /// Creates a new IBitwardenApi instance with the specified base address.
    /// Optionally configures SSL bypass for self-hosted servers with self-signed certificates.
    /// </summary>
    private static IBitwardenApi CreateApi(string baseAddress, bool allowInvalidCertificates)
    {
        var handler = new HttpClientHandler
        {
            ClientCertificateOptions = ClientCertificateOption.Manual
        };

        if (allowInvalidCertificates)
        {
            // SECURITY NOTE: Certificate validation is bypassed to support self-hosted Bitwarden
            // instances with self-signed certificates. This means SSL/TLS connections won't be
            // fully validated. Only use this with Bitwarden servers you trust.
            handler.ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, certChain, policyErrors) => true;
        }

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
            _currentAllowInvalidCertificates = false;
            _cachedApi = null;
        }
    }
}
