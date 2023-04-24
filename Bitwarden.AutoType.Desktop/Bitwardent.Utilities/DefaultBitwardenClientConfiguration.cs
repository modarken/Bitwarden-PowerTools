// See https://aka.ms/new-console-template for more information

namespace Bitwarden.Utilities;

public interface IBitwardenClientConfiguration
{
    string? base_address { get; set; }
    string? client_id { get; set; }
    string? client_secret { get; set; }
    string? device_identifier { get; set; }
    string? device_name { get; set; }
    string? email { get; set; }
    string? master_key { get; set; }
    string? refresh_token { get; set; }
}

public class DefaultBitwardenClientConfiguration : IBitwardenClientConfiguration
{
    public string? base_address { get; set; }
    public string? email { get; set; }
    public string? master_key { get; set; }
    public string? client_id { get; set; }
    public string? client_secret { get; set; }
    public string? refresh_token { get; set; }
    public string? device_name { get; set; }
    public string? device_identifier { get; set; }
}