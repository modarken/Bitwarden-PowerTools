using System.Text.Json.Serialization;
using Bitwarden.Core;

namespace Bitwarden.AutoType.Desktop.Helpers;

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
    [JsonConverter(typeof(ProtectedDataConverter))] public string? master_key { get; set; }
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
}

public class Settings
{
    public BitwardenClientConfiguration? BitwardenClientConfiguration { get; set; } = new BitwardenClientConfiguration();
}