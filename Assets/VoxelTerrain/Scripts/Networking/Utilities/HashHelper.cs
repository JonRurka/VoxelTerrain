using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using UnityGameServer;

//namespace UnityGameServer
//{
public static class HashHelper
{
    private static List<string> _generatedKeys = new List<string>();

    public static string RandomKey(int length)
    {
        char[] chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890".ToCharArray();
        char[] identifier = new char[length];
        byte[] randomData = new byte[length];
        using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
        {
            rng.GetBytes(randomData);
        }
        //Logger.Log("ranKey 2");
        for (int i = 0; i < identifier.Length; i++)
        {
            int pos = randomData[i] % chars.Length;
            identifier[i] = chars[pos];
        }
        //Logger.Log("ranKey 3");
        string key = new string(identifier);
        if (_generatedKeys.Contains(key))
            key = RandomKey(length);
        return key;
    }

    public static byte[] RandomBytes(int seed, int length)
    {
        Random rand = new Random(seed);
        byte[] data = new byte[length];
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = (byte)rand.Next(0, 256);
        }
        return data;
    }

    public static RSAParameters GenerateRsaParameters()
    {
        RSACryptoServiceProvider RSA = new RSACryptoServiceProvider();
        return RSA.ExportParameters(true);
    }

    public static byte[] RsaEncrypt(string data, byte[] exp, byte[] publicKey)
    {
        return RsaEncrypt(Encoding.UTF8.GetBytes(data), exp, publicKey);
    }

    public static byte[] RsaEncrypt(byte[] data, byte[] exp, byte[] publicKey)
    {
        byte[] encryptedData;
        using (RSACryptoServiceProvider RSA = new RSACryptoServiceProvider())
        {
            RSAParameters RSAKeyInfo = new RSAParameters();
            RSAKeyInfo.Exponent = exp;
            RSAKeyInfo.Modulus = publicKey;
            RSA.ImportParameters(RSAKeyInfo);
            encryptedData = RSA.Encrypt(data, false);
        }
        return encryptedData;
    }

    public static byte[] RsaDecrypt(byte[] value, RSAParameters RSAKeyInfo)
    {
        byte[] decryptedData;
        using (RSACryptoServiceProvider RSA = new RSACryptoServiceProvider())
        {
            RSA.ImportParameters(RSAKeyInfo);
            decryptedData = RSA.Decrypt(value, false);
        }
        return decryptedData;
    }

    public static string MD5Hash(string value)
    {
        StringBuilder sBuilder = new StringBuilder();
        MD5 md5Hash = MD5.Create();
        byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(value));
        for (int i = 0; i < data.Length; i++)
        {
            sBuilder.Append(data[i].ToString("x2"));
        }
        md5Hash.Dispose();
        return sBuilder.ToString();
    }

    public static string SHA256Hash(string value)
    {
        StringBuilder sBuilder = new StringBuilder();
        SHA256 sha256 = SHA256Managed.Create();
        byte[] data = sha256.ComputeHash(Encoding.UTF8.GetBytes(value));
        for (int i = 0; i < data.Length; i++)
        {
            sBuilder.Append(data[i].ToString("x2"));
        }
        sha256.Dispose();
        return sBuilder.ToString();
    }

    public static string HashPasswordFull(string value, string salt)
    {
        return HashPasswordServer(HashPasswordClient(value, salt), salt);
    }

    public static string HashPasswordClient(string value, string salt)
    {
        return MD5Hash(value + salt);
    }

    public static string HashPasswordServer(string value, string salt)
    {
        //return SHA256Hash(salt + value);
        return MD5Hash(MD5Hash(salt) + value);
    }

    public static string Encrypt(string input, string pass)
    {
        return Encrypt(Encoding.UTF8.GetBytes(input), pass);
    }

    public static string Encrypt(byte[] input, string password)
    {
        var aesAlg = NewRijndaelManaged(password);

        var encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
        var msEncrypt = new MemoryStream();
        using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
        using (var swEncrypt = new StreamWriter(csEncrypt))
        {
            swEncrypt.Write(Encoding.UTF8.GetString(input));
        }

        return Convert.ToBase64String(msEncrypt.ToArray());
    }

    public static string Decrypt(string input, string password)
    {
        return Decrypt(Convert.FromBase64String(input), password);
    }

    public static string Decrypt(byte[] input, string password)
    {
        string text;

        var aesAlg = NewRijndaelManaged(password);
        var decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

        using (var msDecrypt = new MemoryStream(input))
        {
            using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
            {
                using (var srDecrypt = new StreamReader(csDecrypt))
                {
                    text = srDecrypt.ReadToEnd();
                }
            }
        }
        return text;
    }

    public static bool IsBase64String(string base64String)
    {
        base64String = base64String.Trim();
        return (base64String.Length % 4 == 0) &&
               Regex.IsMatch(base64String, @"^[a-zA-Z0-9\+/]*={0,3}$", RegexOptions.None);

    }

    private static RijndaelManaged NewRijndaelManaged(string salt)
    {
        if (salt == null)
            throw new ArgumentNullException("salt");
        var saltBytes = Encoding.ASCII.GetBytes(salt);
        var key = new Rfc2898DeriveBytes("", saltBytes);

        var aesAlg = new RijndaelManaged();
        aesAlg.Key = key.GetBytes(aesAlg.KeySize / 8);
        aesAlg.IV = key.GetBytes(aesAlg.BlockSize / 8);

        return aesAlg;
    }

    //internal const string Inputkey = "2b8e9b21-9b29-4d9e-8131-4faa046b5bb0";
}
//}