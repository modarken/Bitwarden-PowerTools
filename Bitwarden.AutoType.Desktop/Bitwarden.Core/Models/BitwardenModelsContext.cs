using System.Text.Json.Serialization;

namespace Bitwarden.Core.Models;

[JsonSerializable(typeof(PreLoginResponse))]
[JsonSerializable(typeof(TokenResponse))]
[JsonSerializable(typeof(ProfileResponse))]
[JsonSerializable(typeof(SyncResponse))]
public partial class BitwardenModelsContext : JsonSerializerContext
{
}
