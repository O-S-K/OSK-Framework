using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace OSK
{
    internal static class Obfuscator
    { 
        public static byte[] Encrypt(byte[] data, string key)
        {
            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(key);

            var iv = new byte[16];
            RandomNumberGenerator.Fill(iv);
            aes.IV = iv;

            using var ms = new MemoryStream();
            ms.Write(iv);

            using var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write);
            cs.Write(data);
            cs.FlushFinalBlock();
            return ms.ToArray();
        }

        public static byte[] Decrypt(byte[] data, string key)
        {
            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(key);

            using var ms = new MemoryStream(data);

            var iv = new byte[16];
            ms.Read(iv);
            aes.IV = iv;

            using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
            using var rs = new MemoryStream();
            cs.CopyTo(rs);

            return rs.ToArray();
        }
        
        

        #region AES Encryption / Decryption

        public static string Encryption(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return "";
            byte[] keyBytes = Encoding.UTF8.GetBytes(IOUtility.encryptKey.Substring(0, 16));
            byte[] iv = keyBytes;

            using (Aes aes = Aes.Create())
            {
                aes.Key = keyBytes;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                byte[] encrypted = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
                return Convert.ToBase64String(encrypted);
            }
        }

        public static string Decryption(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                return "";
            try
            {
                // if the string is not in base64 format, return it as is
                if (!IsBase64String(cipherText))
                    return cipherText;

                byte[] keyBytes = Encoding.UTF8.GetBytes(IOUtility.encryptKey.Substring(0, 16));
                byte[] iv = keyBytes;

                using (Aes aes = Aes.Create())
                {
                    aes.Key = keyBytes;
                    aes.IV = iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                    byte[] cipherBytes = Convert.FromBase64String(cipherText);
                    byte[] decrypted = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
                    return Encoding.UTF8.GetString(decrypted);
                }
            }
            catch (Exception)
            {
                // if decryption fails, return the original cipherText
                return cipherText;
            }
        }
 
        // check string have base64 format
        private static bool IsBase64String(string s)
        {
            Span<byte> buffer = new Span<byte>(new byte[s.Length]);
            return Convert.TryFromBase64String(s, buffer, out _);
        }
        #endregion

        public static async Task<byte[]> EncryptAsync(byte[] data, string key)
        {
            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(key);

            var iv = new byte[16];
            RandomNumberGenerator.Fill(iv);
            aes.IV = iv;

            await using var ms = new MemoryStream();
            await ms.WriteAsync(iv);

            await using var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write);
            await cs.WriteAsync(data);
            cs.FlushFinalBlock();

            return ms.ToArray();
        }

        public static async Task<byte[]> DecryptAsync(byte[] data, string key)
        {
            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(key);

            await using var ms = new MemoryStream(data);

            var iv = new byte[16];
            await ms.ReadAsync(iv);
            aes.IV = iv;

            await using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
            await using var rs = new MemoryStream();
            await cs.CopyToAsync(rs);

            return rs.ToArray();
        }
    }
}