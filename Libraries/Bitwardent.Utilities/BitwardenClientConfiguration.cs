// See https://aka.ms/new-console-template for more information

using System.Text.Json.Serialization;
using Bitwarden.Core;

namespace Bitwarden.Utilities;

public static class StringUtilities
{
    public static bool IsNullOrEmpty(this string? source)
    {
        return string.IsNullOrEmpty(source);
    }
}

public static class BitwardenClientConfigurationExtensions
{
    public static bool Validate(this BitwardenClientConfiguration source)
    {
        if (source is null) { return false; }

        return source.HasSavedSettings()
            && !source.authorization_invalidated
            && !source.encryption_key.IsNullOrEmpty()
            && !source.device_name.IsNullOrEmpty()
            && !source.device_identifier.IsNullOrEmpty()
            && source.HasStoredAuthorizationMaterial();
    }

    public static bool HasSavedSettings(this BitwardenClientConfiguration source)
    {
        if (source is null)
        {
            return false;
        }

        return !source.base_address.IsNullOrEmpty()
            && !source.email.IsNullOrEmpty();
    }

    public static bool HasStoredAuthorizationMaterial(this BitwardenClientConfiguration source)
    {
        if (source is null)
        {
            return false;
        }

        return !source.refresh_token.IsNullOrEmpty()
            || (!source.client_id.IsNullOrEmpty() && !source.client_secret.IsNullOrEmpty());
    }

    public static bool HasStoredKdfMetadata(this BitwardenClientConfiguration source)
    {
        if (source is null)
        {
            return false;
        }

        return source.last_known_kdf.HasValue
            && source.last_known_kdf_iterations.HasValue
            && source.last_known_kdf_iterations.Value > 0;
    }

    public static bool HasUsableAuthorization(this BitwardenClientConfiguration source)
    {
        if (source is null)
        {
            return false;
        }

        return source.HasSavedSettings()
            && !source.authorization_invalidated
            && !source.encryption_key.IsNullOrEmpty()
            && !source.device_name.IsNullOrEmpty()
            && !source.device_identifier.IsNullOrEmpty()
            && source.HasStoredAuthorizationMaterial();
    }
}

public class BitwardenClientConfiguration : IBitwardenClientConfiguration
{
    /// <summary>
    /// Gets or sets the base address of the bitwarden server.
    /// </summary>
    /// <value>
    /// The base address.
    /// </value>
    [JsonConverter(typeof(ProtectedDataConverter))] public string? base_address { get; set; }

    /// <summary>
    /// Gets or sets the email or account that is used to login to the bitwarden server.
    /// </summary>
    /// <value>
    /// The email.
    /// </value>
    [JsonConverter(typeof(ProtectedDataConverter))] public string? email { get; set; }

    /// <summary>
    /// Gets or sets the master key. The master key has several uses.
    /// 1. Used to decrypt the server side protected symmetric key to the generated symmetric key.
    /// Which used to (d)encrypt the data.
    /// 2. Used when logging into the server by calculating the Master Password Hash and sending it to the server.
    /// </summary>
    /// <value>
    /// The master key.
    /// </value>
    // [JsonConverter(typeof(ProtectedDataConverter))] public string? master_key { get; set; }

    /// <summary>
    /// Gets or sets the encryption_key otherwise known as the unprotected symmetric_key used in (d)ecryption of ciphers.
    /// </summary>
    /// <value>
    /// The client encryption key.
    /// </value>
    [JsonConverter(typeof(ProtectedDataConverter))] public string? encryption_key { get; set; }

    /// <summary>
    /// Gets or sets the client identifier used in API ACCESS.
    /// </summary>
    /// <value>
    /// The client identifier.
    /// </value>
    [JsonConverter(typeof(ProtectedDataConverter))] public string? client_id { get; set; }

    /// <summary>
    /// Gets or sets the client secret used in API ACCESS..
    /// </summary>
    /// <value>
    /// The client secret.
    /// </value>
    [JsonConverter(typeof(ProtectedDataConverter))] public string? client_secret { get; set; }

    /// <summary>
    /// Gets or sets the refresh token, used in password access but is permanent alternative to API Access.
    /// </summary>
    /// <value>
    /// The refresh token.
    /// </value>
    [JsonConverter(typeof(ProtectedDataConverter))] public string? refresh_token { get; set; }

    /// <summary>
    /// Gets or sets the authorization method used for the current stored authorization.
    /// </summary>
    public string? authorization_method { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the stored authorization has been invalidated.
    /// </summary>
    public bool authorization_invalidated { get; set; }

    /// <summary>
    /// Gets or sets a human-readable reason for the current authorization invalidation.
    /// </summary>
    public string? authorization_invalidated_reason { get; set; }

    /// <summary>
    /// Gets or sets the last known KDF type observed during successful authorization.
    /// </summary>
    public int? last_known_kdf { get; set; }

    /// <summary>
    /// Gets or sets the last known KDF iteration count observed during successful authorization.
    /// </summary>
    public int? last_known_kdf_iterations { get; set; }

    /// <summary>
    /// Gets or sets the last known KDF memory setting observed during successful authorization.
    /// </summary>
    public int? last_known_kdf_memory { get; set; }

    /// <summary>
    /// Gets or sets the last known KDF parallelism observed during successful authorization.
    /// </summary>
    public int? last_known_kdf_parallelism { get; set; }

    /// <summary>
    /// Gets or sets the name of the device. Optional value to send to server.
    /// </summary>
    /// <value>
    /// The name of the device.
    /// </value>
    [JsonConverter(typeof(ProtectedDataConverter))] public string? device_name { get; set; }

    /// <summary>
    /// Gets or sets the device identifier. Optional value to send to server.
    /// </summary>
    /// <value>
    /// The device identifier.
    /// </value>
    [JsonConverter(typeof(ProtectedDataConverter))] public string? device_identifier { get; set; }

    /// <summary>
    /// Gets or sets whether to allow invalid/self-signed SSL certificates.
    /// Set to true for self-hosted Bitwarden instances with self-signed certificates.
    /// WARNING: This disables SSL certificate validation. Only use with servers you trust.
    /// </summary>
    /// <value>
    /// True to allow invalid certificates, false to enforce validation. Default: true for backward compatibility.
    /// </value>
    public bool AllowInvalidCertificates { get; set; } = true;
}