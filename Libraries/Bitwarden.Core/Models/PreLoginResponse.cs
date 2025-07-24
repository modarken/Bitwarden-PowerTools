using System.Text.Json.Serialization;

namespace Bitwarden.Core.Models;

[JsonSerializable(typeof(PreLoginResponse))]
public class PreLoginResponse
{
    [JsonPropertyName("kdf")]
    public int Kdf { get; set; }

    [JsonPropertyName("kdfIterations")]
    public int KdfIterations { get; set; }

    [JsonPropertyName("kdfMemory")]
    public int? KdfMemory { get; set; }

    [JsonPropertyName("kdfParallelism")]
    public int? KdfParallelism { get; set; }
}