
using OtpNet;
using System;

namespace Bitwarden.AutoType.Desktop.Helpers;

public class TotpHelper
{
    public static string GenerateTotpCode(string base32Secret)
    {
        // Decode the base32-encoded TOTP secret
        byte[] secretBytes = Base32Encoding.ToBytes(base32Secret);

        // Create a TOTP generator with the decoded secret
        var totp = new Totp(secretBytes);

        // Generate the TOTP code
        string totpCode = totp.ComputeTotp();

        return totpCode;
    }
}
