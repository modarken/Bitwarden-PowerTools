namespace Bitwarden.Core
{
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
}