using System;
using System.Security.Cryptography;
using System.Text;

public class Crypto
{
    public static string EncryptString(string sourceString, string password, string salt)
    {
        using var rijndael = Aes.Create();
        if (rijndael == null) throw new Exception("rijndael is null");
        GenerateKeyFromPassword(password, rijndael.KeySize, out var key, rijndael.BlockSize, out var iv, salt);
        rijndael.Key = key;
        rijndael.IV = iv;

        var strBytes = Encoding.UTF8.GetBytes(sourceString);
        using var encryptor = rijndael.CreateEncryptor();
        var encBytes = encryptor.TransformFinalBlock(strBytes, 0, strBytes.Length);
        return Convert.ToBase64String(encBytes);
    }

    public static string DecryptString(string sourceString, string password, string salt)
    {
        try
        {
            using var rijndael = Aes.Create();
            if (rijndael == null) throw new Exception("rijndael is null");
            GenerateKeyFromPassword(password, rijndael.KeySize, out var key, rijndael.BlockSize, out var iv, salt);
            rijndael.Key = key;
            rijndael.IV = iv;

            var strBytes = Convert.FromBase64String(sourceString);
            using var decryptor = rijndael.CreateDecryptor();
            var decBytes = decryptor.TransformFinalBlock(strBytes, 0, strBytes.Length);
            return Encoding.UTF8.GetString(decBytes);
        }
        catch
        {
            throw new Exception("incorrect password");
        }
    }

    private static void GenerateKeyFromPassword(string password,
        int keySize, out byte[] key, int blockSize, out byte[] iv, string salt)
    {
        var saltBytes = Encoding.UTF8.GetBytes(salt);
        var deriveBytes = new Rfc2898DeriveBytes(password, saltBytes);
        deriveBytes.IterationCount = 1000;
        key = deriveBytes.GetBytes(keySize / 8);
        iv = deriveBytes.GetBytes(blockSize / 8);
    }
}