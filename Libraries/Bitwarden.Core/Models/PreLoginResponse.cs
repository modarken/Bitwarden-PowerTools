using System.Text.Json.Serialization;

namespace Bitwarden.Core.Models;

[JsonSerializable(typeof(PreLoginResponse))]
public class PreLoginResponse
{
    public int Kdf { get; set; }
    public int KdfIterations { get; set; }
}