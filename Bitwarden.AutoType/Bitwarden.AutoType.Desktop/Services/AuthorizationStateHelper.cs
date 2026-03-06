using System;
using Bitwarden.Core.Models;
using Bitwarden.Utilities;

namespace Bitwarden.AutoType.Desktop.Services;

public enum AuthorizationStatus
{
    NotAuthorized,
    Authorized,
    ReauthorizationRequired,
}

public enum AuthorizationInputMode
{
    None,
    ApiKey,
    Password,
}

public sealed record AuthorizationStateSnapshot(
    AuthorizationStatus Status,
    string StatusText,
    string MethodText,
    string DetailText);

public static class AuthorizationMethodKinds
{
    public const string ApiKey = "ApiKey";
    public const string Password = "Password";
}

public static class AuthorizationStateHelper
{
    public static AuthorizationStateSnapshot GetSnapshot(BitwardenClientConfiguration? configuration)
    {
        var status = GetStatus(configuration);
        return new AuthorizationStateSnapshot(
            status,
            GetStatusText(status),
            GetMethodText(configuration),
            GetDetailText(configuration, status));
    }

    public static AuthorizationStatus GetStatus(BitwardenClientConfiguration? configuration)
    {
        if (configuration is null || !configuration.HasSavedSettings())
        {
            return AuthorizationStatus.NotAuthorized;
        }

        if (configuration.authorization_invalidated)
        {
            return AuthorizationStatus.ReauthorizationRequired;
        }

        if (configuration.HasUsableAuthorization())
        {
            return AuthorizationStatus.Authorized;
        }

        return AuthorizationStatus.NotAuthorized;
    }

    public static string? GetStoredAuthorizationMethod(BitwardenClientConfiguration? configuration)
    {
        if (configuration is null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(configuration.authorization_method))
        {
            return configuration.authorization_method;
        }

        if (!configuration.client_id.IsNullOrEmpty() && !configuration.client_secret.IsNullOrEmpty())
        {
            return AuthorizationMethodKinds.ApiKey;
        }

        if (!configuration.refresh_token.IsNullOrEmpty())
        {
            return AuthorizationMethodKinds.Password;
        }

        return null;
    }

    public static bool MatchesKdfMetadata(BitwardenClientConfiguration configuration, PreLoginResponse preLogin)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(preLogin);

        return configuration.last_known_kdf == preLogin.Kdf
            && configuration.last_known_kdf_iterations == preLogin.KdfIterations
            && configuration.last_known_kdf_memory == preLogin.KdfMemory
            && configuration.last_known_kdf_parallelism == preLogin.KdfParallelism;
    }

    public static void ApplyKdfMetadata(BitwardenClientConfiguration configuration, PreLoginResponse preLogin)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(preLogin);

        configuration.last_known_kdf = preLogin.Kdf;
        configuration.last_known_kdf_iterations = preLogin.KdfIterations;
        configuration.last_known_kdf_memory = preLogin.KdfMemory;
        configuration.last_known_kdf_parallelism = preLogin.KdfParallelism;
    }

    public static void ApplySuccessfulAuthorization(
        BitwardenClientConfiguration configuration,
        string method,
        string encryptionKey,
        PreLoginResponse preLogin,
        string? refreshToken = null,
        string? clientId = null,
        string? clientSecret = null)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(method);
        ArgumentException.ThrowIfNullOrWhiteSpace(encryptionKey);
        ArgumentNullException.ThrowIfNull(preLogin);

        configuration.encryption_key = encryptionKey;
        configuration.authorization_method = method;
        configuration.authorization_invalidated = false;
        configuration.authorization_invalidated_reason = null;
        ApplyKdfMetadata(configuration, preLogin);

        if (method == AuthorizationMethodKinds.ApiKey)
        {
            configuration.client_id = clientId;
            configuration.client_secret = clientSecret;
            configuration.refresh_token = null;
            return;
        }

        configuration.refresh_token = refreshToken;
        configuration.client_id = null;
        configuration.client_secret = null;
    }

    public static void ClearStoredAuthorization(BitwardenClientConfiguration configuration, bool clearApiCredentials)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        configuration.encryption_key = null;
        configuration.refresh_token = null;

        if (clearApiCredentials)
        {
            configuration.client_id = null;
            configuration.client_secret = null;
            configuration.authorization_method = null;
        }

        configuration.authorization_invalidated = false;
        configuration.authorization_invalidated_reason = null;
        ClearKdfMetadata(configuration);
    }

    public static void InvalidateStoredAuthorization(BitwardenClientConfiguration configuration, string reason)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        var method = GetStoredAuthorizationMethod(configuration);

        configuration.encryption_key = null;
        configuration.refresh_token = null;
        configuration.authorization_method = method;
        configuration.authorization_invalidated = true;
        configuration.authorization_invalidated_reason = reason;
        ClearKdfMetadata(configuration);
    }

    private static void ClearKdfMetadata(BitwardenClientConfiguration configuration)
    {
        configuration.last_known_kdf = null;
        configuration.last_known_kdf_iterations = null;
        configuration.last_known_kdf_memory = null;
        configuration.last_known_kdf_parallelism = null;
    }

    private static string GetStatusText(AuthorizationStatus status)
    {
        return status switch
        {
            AuthorizationStatus.Authorized => "Status: Authorized",
            AuthorizationStatus.ReauthorizationRequired => "Status: Re-authorization required",
            _ => "Status: Not authorized",
        };
    }

    private static string GetMethodText(BitwardenClientConfiguration? configuration)
    {
        return GetStoredAuthorizationMethod(configuration) switch
        {
            AuthorizationMethodKinds.ApiKey => "Authorization path: API key + master password",
            AuthorizationMethodKinds.Password => "Authorization path: Master password",
            _ => "Authorization path: Not set",
        };
    }

    private static string GetDetailText(BitwardenClientConfiguration? configuration, AuthorizationStatus status)
    {
        if (configuration is null || !configuration.HasSavedSettings())
        {
            return "Save your account settings before authorizing this device.";
        }

        return status switch
        {
            AuthorizationStatus.Authorized => "Stored authorization is available for sync and auto-type.",
            AuthorizationStatus.ReauthorizationRequired => configuration.authorization_invalidated_reason ?? "Stored authorization is no longer trusted. Re-authorize to continue.",
            _ when configuration.HasStoredAuthorizationMaterial() => "Stored authorization is incomplete. Authorize again to continue.",
            _ => "This device is not authorized yet.",
        };
    }
}