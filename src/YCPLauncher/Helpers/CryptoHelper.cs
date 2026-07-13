using System;
using System.Text;

namespace YCPLauncher.Helpers;

public static class CryptoHelper
{
    private const string Prefix = "ENC:";
    private static readonly byte[] Key = Encoding.UTF8.GetBytes("YCP_LAUNCHER_SECRET_KEY_2026");

    // Backward-compatible decoder for configuration files written before 1.1.7.
    // New files store endpoint URLs as plain JSON because they are not secrets.
    public static string Decrypt(string encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText)) return encryptedText;
        if (!encryptedText.StartsWith(Prefix)) return encryptedText; // not encrypted

        try
        {
            string base64 = encryptedText.Substring(Prefix.Length);
            byte[] encryptedBytes = Convert.FromBase64String(base64);
            byte[] decryptedBytes = new byte[encryptedBytes.Length];

            for (int i = 0; i < encryptedBytes.Length; i++)
            {
                decryptedBytes[i] = (byte)(encryptedBytes[i] ^ Key[i % Key.Length]);
            }

            return Encoding.UTF8.GetString(decryptedBytes);
        }
        catch
        {
            return encryptedText; // fallback if decryption fails
        }
    }
}
