using System;
using System.Text;

namespace YCPLauncher.Helpers;

public static class CryptoHelper
{
    private const string Prefix = "ENC:";
    private static readonly byte[] Key = Encoding.UTF8.GetBytes("YCP_LAUNCHER_SECRET_KEY_2026");

    public static string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return plainText;
        if (plainText.StartsWith(Prefix)) return plainText; // already encrypted

        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
        byte[] encryptedBytes = new byte[plainBytes.Length];

        for (int i = 0; i < plainBytes.Length; i++)
        {
            encryptedBytes[i] = (byte)(plainBytes[i] ^ Key[i % Key.Length]);
        }

        return Prefix + Convert.ToBase64String(encryptedBytes);
    }

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
