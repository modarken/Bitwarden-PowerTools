using Bitwarden.Core.Models;
using Refit;

namespace Bitwarden.Core.API;

/// <summary>
/// Refit interface for Bitwarden API endpoints.
/// This provides a clean, declarative way to define HTTP API calls.
/// </summary>
public interface IBitwardenApi
{
    /// <summary>
    /// Gets the pre-login information for an email address, including KDF settings.
    /// </summary>
    [Post("/identity/accounts/prelogin")]
    Task<PreLoginResponse?> PostPreLoginAsync([Body] PreLoginRequest request);

    /// <summary>
    /// Gets an access token using API key credentials (client_id/client_secret).
    /// </summary>
    [Post("/identity/connect/token")]
    Task<ApiResponse<TokenResponse>> PostAccessTokenFromApiKeyAsync([Body(BodySerializationMethod.UrlEncoded)] Dictionary<string, string> form);

    /// <summary>
    /// Gets an access token using a refresh token.
    /// </summary>
    [Post("/identity/connect/token")]
    Task<ApiResponse<TokenResponse>> PostAccessTokenFromRefreshTokenAsync([Body(BodySerializationMethod.UrlEncoded)] Dictionary<string, string> form);

    /// <summary>
    /// Gets an access token using master password hash.
    /// </summary>
    [Post("/identity/connect/token")]
    Task<ApiResponse<TokenResponse>> PostAccessTokenFromPasswordAsync([Body(BodySerializationMethod.UrlEncoded)] Dictionary<string, string> form);

    /// <summary>
    /// Gets the revision date for the account (used to check if sync is needed).
    /// </summary>
    [Get("/api/accounts/revision-date")]
    Task<string?> GetRevisionDateAsync([Header("Authorization")] string bearerToken);

    /// <summary>
    /// Gets the user's profile information.
    /// </summary>
    [Get("/api/accounts/profile")]
    Task<ProfileResponse?> GetProfileAsync([Header("Authorization")] string bearerToken);

    /// <summary>
    /// Gets the full vault sync data including ciphers, folders, and collections.
    /// </summary>
    [Get("/api/sync")]
    Task<SyncResponse?> GetSyncAsync([Header("Authorization")] string bearerToken);
}

/// <summary>
/// Request model for pre-login endpoint.
/// </summary>
public class PreLoginRequest
{
    public string Email { get; set; } = string.Empty;
}
