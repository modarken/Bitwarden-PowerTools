using System.Linq;
using System.Security.Cryptography;

namespace System.Text.Json.Serialization;

// ENCRYPTION SCHEME
// <Protected>{base64}

public class ProtectedDataConverter : JsonConverter<string?>
{
    public static readonly string Key = "<Protected>";
    public static readonly byte[] Entropy = new byte[] { 77, 54, 8, 44, 1 };

    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (value is null ||
            value.Length < Key.Length ||
            !value.StartsWith(Key))
        {
            return value;
        }

        var base64 = String.Concat(value.Skip(Key.Length));
        var binary = Convert.FromBase64String(base64);
        var data = ProtectedData.Unprotect(binary, Entropy, DataProtectionScope.CurrentUser);
        return Encoding.ASCII.GetString(data);
    }

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
    {
        if (writer is null) return;
        var plainText = value!.ToString();
        if (plainText.Length == 0)
        {
            writer.WriteStringValue(string.Empty);
            return;
        }

        var plainBytes = Encoding.ASCII.GetBytes(plainText);
        var encryptedBytes = ProtectedData.Protect(plainBytes, Entropy, DataProtectionScope.CurrentUser);
        var encryptedBytesBase64 = $"{Key}{Convert.ToBase64String(encryptedBytes)}";
        writer.WriteStringValue(encryptedBytesBase64);
    }
}