using System.Text.Json.Serialization;
using Bitwarden.Core;

namespace Bitwarden.AutoType.Desktop.Helpers;

public class BitwardenClientConfiguration : IBitwardenClientConfiguration
{
    [JsonConverter(typeof(ProtectedDataConverter))] public string? base_address { get; set; }
    [JsonConverter(typeof(ProtectedDataConverter))] public string? email { get; set; }
    [JsonConverter(typeof(ProtectedDataConverter))] public string? master_key { get; set; }
    [JsonConverter(typeof(ProtectedDataConverter))] public string? client_id { get; set; }
    [JsonConverter(typeof(ProtectedDataConverter))] public string? client_secret { get; set; }
    [JsonConverter(typeof(ProtectedDataConverter))] public string? refresh_token { get; set; }
    [JsonConverter(typeof(ProtectedDataConverter))] public string? device_name { get; set; }
    [JsonConverter(typeof(ProtectedDataConverter))] public string? device_identifier { get; set; }
}

public class Settings
{
    public BitwardenClientConfiguration? BitwardenClientConfiguration { get; set; } = new BitwardenClientConfiguration();
}