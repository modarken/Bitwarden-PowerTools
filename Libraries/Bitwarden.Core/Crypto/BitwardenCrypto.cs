using System.Security.Cryptography;
using System.Text;

namespace Bitwarden.Core.Crypto;

public static class BitwardenCrypto
{
    public static byte[] DerriveMasterKey(string masterPassword, string email, int iterations)
    {
        return PBKDF2Hash(Encoding.ASCII.GetBytes(masterPassword), Encoding.ASCII.GetBytes(email.ToLower()), iterations);
    }

    public static string DerriveMasterPasswordHash(string masterPassword, string email, int iterations)
    {
        var masterHash = PBKDF2Hash(Encoding.ASCII.GetBytes(masterPassword), Encoding.ASCII.GetBytes(email.ToLower()), iterations);
        var masterPasswordHash = PBKDF2Hash(masterHash, Encoding.ASCII.GetBytes(masterPassword), 1);
        return Convert.ToBase64String(masterPasswordHash);
    }

    public static string DerriveMasterPasswordHashFromMasterKey(byte[] masterKey, byte[] masterPasswordSalt)
    {
        var masterPasswordHash = PBKDF2Hash(masterKey, masterPasswordSalt, 1);
        return Convert.ToBase64String(masterPasswordHash);
    }

    public static bool MacsEqual(byte[] macKey, byte[] mac1, byte[] mac2)
    {
        using HMACSHA256 hmac = new(macKey);

        byte[] hmac1 = hmac.ComputeHash(mac1);
        byte[] hmac2 = hmac.ComputeHash(mac2);

        return hmac1.SequenceEqual(hmac2);
    }

    public static byte[]? DecryptEncryptionKey(string cipherEncryptionKey, byte[] masterKey)
    {
        // var (version, iv, encryptedKey) = BitwardenCrypto.DecodeCipherString(cipherEncryptionKey);
        var version = Convert.ToInt32(cipherEncryptionKey[0].ToString());
        var items = cipherEncryptionKey[2..].Split("|").Select(e => Convert.FromBase64String(e)).ToArray();
        var cipherIV = items[0];
        var cipherData = items[1];

        // AesCbc256_B64
        if (version == 0)
        {
            return Aes256CBCDecrypt(cipherData, masterKey, cipherIV);
        }

        // AesCbc256_HmacSha256_B64
        if (version == 2)
        {
            var cipherMac = items[2];
            var stetchedMasterKey = StretchKey(masterKey);
            var masterMac = stetchedMasterKey[32..64];
            var hash = new HMACSHA256(masterMac);
            var computedMac = hash.ComputeHash(cipherIV.Concat(cipherData).ToArray());
            if (!MacsEqual(masterMac, computedMac, cipherMac))
            {
                return null;
            }

            return Aes256CBCDecrypt(cipherData, stetchedMasterKey[0..32], cipherIV);
        }

        return new byte[0];
    }

    public static string? DecryptEntry(string entry, byte[] encryptionKey, bool verify = true)
    {
        var items = entry[2..].Split("|").Take(3).Select(e => Convert.FromBase64String(e)).ToArray();
        var entryIV = items[0];
        var entryData = items[1];
        var entryMac = items[2];

        var encEncryptionKey = encryptionKey[0..32];
        var macEncryptionKey = encryptionKey[32..64];

        // VERIFY
        if (verify)
        {
            var hash = new HMACSHA256(macEncryptionKey);
            var computedMac = hash.ComputeHash(entryIV.Concat(entryData).ToArray());
            if (!MacsEqual(macEncryptionKey, computedMac, entryMac))
            {
                return null;
            }
        }

        var decrypted = Aes256CBCDecrypt(entryData, encEncryptionKey, entryIV);
        return Encoding.UTF8.GetString(decrypted);
    }

    public static byte[] Aes256CBCDecrypt(byte[] data, byte[] key, byte[] iv)
    {
        // https://stackoverflow.com/questions/24903575/how-to-return-byte-when-decrypt-using-cryptostream-descryptoserviceprovider
        using Aes aes = Aes.Create();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.IV = iv;
        aes.Key = key;

        using var decryptor = aes.CreateDecryptor();
        using MemoryStream ms = new();
        using CryptoStream cs = new(ms, decryptor, CryptoStreamMode.Write);
        cs.Write(data, 0, data.Length);
        cs.FlushFinalBlock();
        var decrypted = ms.ToArray();
        return decrypted;
    }

    public static byte[] PBKDF2Hash(byte[] password, byte[] salt, int iterations)
    {
        // Generate the hash
        Rfc2898DeriveBytes pbkdf2 = new(password, salt, iterations, new HashAlgorithmName("SHA256"));
        return pbkdf2.GetBytes(32); //20 bytes length is 160 bits
    }

    public static string ByteArrayToString(this byte[] ba)
    {
        return BitConverter.ToString(ba).Replace("-", "");
    }

    public static byte[] StretchKey(byte[] masterKey)
    {
        var buffer = new byte[64];
        var encBuffer = HKDFExpand(masterKey, "enc");
        var macBuffer = HKDFExpand(masterKey, "mac");
        Array.Copy(encBuffer, 0, buffer, 0, encBuffer.Length);
        Array.Copy(macBuffer, 0, buffer, 32, macBuffer.Length);
        return buffer;
    }

    public static byte[] HKDFExpand(byte[] key, string info)
    {
        var infoBytes = Encoding.ASCII.GetBytes(info);
        var buffer = new byte[infoBytes.Length + 1];
        Array.Copy(infoBytes, buffer, infoBytes.Length);
        buffer[buffer.Length - 1] = 1;
        using var hmac = new HMACSHA256(key);
        return hmac.ComputeHash(buffer);
    }

    public static (int, byte[], byte[]) DecodeCipherString(string encodedKey)
    {
        var split = encodedKey.Split("|");
        var version = int.Parse(split.First()[0].ToString());//.Take(1).ToString();
        var iv = split.First().Split(".").Last();
        var key = split.Skip(1).First();
        return (version, Convert.FromBase64String(iv), Convert.FromBase64String(key));
    }
}