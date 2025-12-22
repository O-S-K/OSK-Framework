using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using System.IO;

namespace OSK
{
    public class FileSecurity  
    {

        public static byte[] Encrypt(byte[] data, string key)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(key.PadRight(32).Substring(0, 32));
                aes.IV = new byte[16];  

                using (ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    using (CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                    {
                        cryptoStream.Write(data, 0, data.Length);
                    }
                    return memoryStream.ToArray();
                }
            }
        }

        public static byte[] Decrypt(byte[] encryptedData, string key)
        {
            try
            {
                using (Aes aes = Aes.Create())
                {
                    aes.Key = Encoding.UTF8.GetBytes(key.PadRight(32).Substring(0, 32));

                    // Đọc IV từ encryptedData (16 byte đầu tiên)
                    byte[] iv = new byte[16];
                    Array.Copy(encryptedData, 0, iv, 0, iv.Length);
                    aes.IV = iv;

                    using (ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                    using (MemoryStream inputMemoryStream = new MemoryStream(encryptedData, iv.Length, encryptedData.Length - iv.Length))
                    using (MemoryStream outputMemoryStream = new MemoryStream())
                    {
                        using (CryptoStream cryptoStream = new CryptoStream(inputMemoryStream, decryptor, CryptoStreamMode.Read))
                        {
                            cryptoStream.CopyTo(outputMemoryStream);
                        }
                        return outputMemoryStream.ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                OSK.MyLogger.LogError($"[Decryption Error]: {ex.Message}");
                return null;
            }
        }
        
        public static string Encrypt(string plainText, string Key)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(Key);
                aes.IV = new byte[16]; // Initialization vector (IV) set to 0s for simplicity

                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                using (var ms = new System.IO.MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        using (var writer = new System.IO.StreamWriter(cs))
                        {
                            writer.Write(plainText);
                        }
                    }

                    return System.Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        public static string Decrypt(string cipherText , string Key)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(Key);
                aes.IV = new byte[16]; // Initialization vector (IV) set to 0s for simplicity

                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using (var ms = new System.IO.MemoryStream(System.Convert.FromBase64String(cipherText)))
                {
                    using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    {
                        using (var reader = new System.IO.StreamReader(cs))
                        {
                            return reader.ReadToEnd();
                        }
                    }
                }
            }
        }
        public static string CalculateMD5Hash(string input)
        {
            var md5 = MD5.Create();
            byte[] inputBytes = Encoding.ASCII.GetBytes(input);
            byte[] hash = md5.ComputeHash(inputBytes);
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("x2"));
            }
            MyLogger.Log("CalculateMD5Hash" +  sb);
            return sb.ToString();
        }
    }
}