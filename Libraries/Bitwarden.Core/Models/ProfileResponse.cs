using System.Text.Json.Serialization;

namespace Bitwarden.Core.Models;

// [JsonSerializable(typeof(ProfileResponse))]

//public class ProfileResponse
//{
//    public string? Culture { get; set; }
//    public string? Email { get; set; }
//    public bool EmailVerified { get; set; }
//    public bool ForcePasswordReset { get; set; }
//    public string? Id { get; set; }
//    public string? Key { get; set; }
//    public object? MasterPasswordHint { get; set; }
//    public string? Name { get; set; }
//    public string? Object { get; set; }
//    public Organization[]? Organizations { get; set; }
//    public bool Premium { get; set; }
//    public string? PrivateKey { get; set; }
//    public object[]? ProviderOrganizations { get; set; }
//    public object[]? Providers { get; set; }
//    public string? SecurityStamp { get; set; }
//    public bool TwoFactorEnabled { get; set; }
//    public int _Status { get; set; }
//}

public class ProfileResponse
{
    public string? Culture { get; set; }
    [JsonPropertyName("email")]
    public string? Email { get; set; }
    public bool EmailVerified { get; set; }
    public bool ForcePasswordReset { get; set; }
    public string? Id { get; set; }
    public string? Key { get; set; }
    public object? MasterPasswordHint { get; set; }
    public string? Name { get; set; }
    public string? Object { get; set; }
    public Organization[]? Organizations { get; set; }
    public bool Premium { get; set; }
    public bool PremiumFromOrganization { get; set; } // Added this property
    public string? PrivateKey { get; set; }
    public object[]? ProviderOrganizations { get; set; }
    public object[]? Providers { get; set; }
    public string? SecurityStamp { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public bool UsesKeyConnector { get; set; } // Added this property
    [JsonPropertyName("_status")]
    public int _Status { get; set; }
    public string? AvatarColor { get; set; } // Added this property
    public DateTime? CreationDate { get; set; } // Added this property
}
