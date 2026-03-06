using Bitwarden.AutoType.Desktop.Services;
using Bitwarden.Core.Models;
using Bitwarden.Utilities;
using Xunit;

namespace Bitwarden.AutoType.Desktop.Tests;

public class AuthorizationStateHelperTests
{
    [Fact]
    public void ApplySuccessfulAuthorizationForApiKeyStoresProtectedState()
    {
        var configuration = CreateBaseConfiguration();
        var preLogin = new PreLoginResponse
        {
            Kdf = 0,
            KdfIterations = 600000,
            KdfMemory = null,
            KdfParallelism = null,
        };

        AuthorizationStateHelper.ApplySuccessfulAuthorization(
            configuration,
            AuthorizationMethodKinds.ApiKey,
            "encrypted-key",
            preLogin,
            clientId: "client-id",
            clientSecret: "client-secret");

        Assert.Equal("encrypted-key", configuration.encryption_key);
        Assert.Equal("client-id", configuration.client_id);
        Assert.Equal("client-secret", configuration.client_secret);
        Assert.Null(configuration.refresh_token);
        Assert.Equal(AuthorizationMethodKinds.ApiKey, configuration.authorization_method);
        Assert.False(configuration.authorization_invalidated);
        Assert.True(configuration.HasStoredKdfMetadata());
    }

    [Fact]
    public void ApplySuccessfulAuthorizationForPasswordStoresRefreshToken()
    {
        var configuration = CreateBaseConfiguration();
        var preLogin = new PreLoginResponse
        {
            Kdf = 0,
            KdfIterations = 600000,
            KdfMemory = null,
            KdfParallelism = null,
        };

        AuthorizationStateHelper.ApplySuccessfulAuthorization(
            configuration,
            AuthorizationMethodKinds.Password,
            "encrypted-key",
            preLogin,
            refreshToken: "refresh-token");

        Assert.Equal("encrypted-key", configuration.encryption_key);
        Assert.Equal("refresh-token", configuration.refresh_token);
        Assert.Null(configuration.client_id);
        Assert.Null(configuration.client_secret);
        Assert.Equal(AuthorizationMethodKinds.Password, configuration.authorization_method);
    }

    [Fact]
    public void InvalidateStoredAuthorizationClearsVaultKeyAndRefreshToken()
    {
        var configuration = CreateBaseConfiguration();
        configuration.encryption_key = "encrypted-key";
        configuration.client_id = "client-id";
        configuration.client_secret = "client-secret";
        configuration.refresh_token = "refresh-token";
        configuration.authorization_method = AuthorizationMethodKinds.ApiKey;
        configuration.last_known_kdf = 0;
        configuration.last_known_kdf_iterations = 600000;

        AuthorizationStateHelper.InvalidateStoredAuthorization(configuration, "KDF changed");

        Assert.Null(configuration.encryption_key);
        Assert.Null(configuration.refresh_token);
        Assert.Equal("client-id", configuration.client_id);
        Assert.Equal("client-secret", configuration.client_secret);
        Assert.True(configuration.authorization_invalidated);
        Assert.Equal("KDF changed", configuration.authorization_invalidated_reason);
        Assert.Null(configuration.last_known_kdf);
        Assert.Null(configuration.last_known_kdf_iterations);
    }

    [Fact]
    public void ClearStoredAuthorizationRemovesAllStoredAuthorizationWhenRequested()
    {
        var configuration = CreateBaseConfiguration();
        configuration.encryption_key = "encrypted-key";
        configuration.client_id = "client-id";
        configuration.client_secret = "client-secret";
        configuration.authorization_method = AuthorizationMethodKinds.ApiKey;
        configuration.authorization_invalidated = true;
        configuration.authorization_invalidated_reason = "Some reason";
        configuration.last_known_kdf = 0;
        configuration.last_known_kdf_iterations = 600000;

        AuthorizationStateHelper.ClearStoredAuthorization(configuration, clearApiCredentials: true);

        Assert.Null(configuration.encryption_key);
        Assert.Null(configuration.client_id);
        Assert.Null(configuration.client_secret);
        Assert.Null(configuration.refresh_token);
        Assert.Null(configuration.authorization_method);
        Assert.False(configuration.authorization_invalidated);
        Assert.Null(configuration.authorization_invalidated_reason);
        Assert.False(configuration.HasStoredKdfMetadata());
    }

    [Fact]
    public void GetSnapshotReportsReauthorizationRequiredWhenInvalidated()
    {
        var configuration = CreateBaseConfiguration();
        configuration.client_id = "client-id";
        configuration.client_secret = "client-secret";
        configuration.authorization_method = AuthorizationMethodKinds.ApiKey;
        configuration.authorization_invalidated = true;
        configuration.authorization_invalidated_reason = "Account security settings changed.";

        var snapshot = AuthorizationStateHelper.GetSnapshot(configuration);

        Assert.Equal(AuthorizationStatus.ReauthorizationRequired, snapshot.Status);
        Assert.Equal("Status: Re-authorization required", snapshot.StatusText);
        Assert.Equal("Authorization path: API key + master password", snapshot.MethodText);
        Assert.Equal("Account security settings changed.", snapshot.DetailText);
    }

    [Fact]
    public void MatchesKdfMetadataReturnsFalseWhenServerSettingsDiffer()
    {
        var configuration = CreateBaseConfiguration();
        configuration.last_known_kdf = 0;
        configuration.last_known_kdf_iterations = 600000;
        configuration.last_known_kdf_memory = null;
        configuration.last_known_kdf_parallelism = null;

        var preLogin = new PreLoginResponse
        {
            Kdf = 0,
            KdfIterations = 700000,
            KdfMemory = null,
            KdfParallelism = null,
        };

        Assert.False(AuthorizationStateHelper.MatchesKdfMetadata(configuration, preLogin));
    }

    private static BitwardenClientConfiguration CreateBaseConfiguration()
    {
        return new BitwardenClientConfiguration
        {
            base_address = "https://vault.example.test",
            email = "user@example.test",
            device_identifier = "device-id",
            device_name = "device-name",
        };
    }
}