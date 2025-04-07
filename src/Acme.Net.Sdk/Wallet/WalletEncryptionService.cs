using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Acme.Net.Sdk.Wallet
{
    /// <summary>
    /// Service responsible for encrypting and decrypting wallet data using password-based encryption.
    /// </summary>
    public class WalletEncryptionService
    {
        private const int Iterations = 10000;
        private const int KeySize = 256;
        private const int SaltSize = 16;
        private const int IvSize = 16;
        
        /// <summary>
        /// Encrypts wallet data with a password.
        /// </summary>
        /// <param name="data">The data to encrypt.</param>
        /// <param name="password">The password to use for encryption.</param>
        /// <returns>The encrypted data.</returns>
        /// <exception cref="ArgumentException">Thrown if data or password is invalid.</exception>
        public byte[] Encrypt(byte[] data, string password)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentException("Data cannot be null or empty", nameof(data));
            
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Password cannot be null or empty", nameof(password));

            // Generate salt and initialization vector
            byte[] salt = GenerateRandomBytes(SaltSize);
            byte[] iv = GenerateRandomBytes(IvSize);
            
            // Derive key from password and salt
            byte[] key = DeriveKey(password, salt);
            
            using var output = new MemoryStream();
            
            // Write salt and IV for decryption
            output.Write(salt, 0, salt.Length);
            output.Write(iv, 0, iv.Length);
            
            // Encrypt the data
            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                
                using var cryptoStream = new CryptoStream(output, aes.CreateEncryptor(), CryptoStreamMode.Write);
                cryptoStream.Write(data, 0, data.Length);
                cryptoStream.FlushFinalBlock();
            }
            
            return output.ToArray();
        }
        
        /// <summary>
        /// Decrypts wallet data with a password.
        /// </summary>
        /// <param name="encryptedData">The data to decrypt.</param>
        /// <param name="password">The password to use for decryption.</param>
        /// <returns>The decrypted data.</returns>
        /// <exception cref="ArgumentException">Thrown if encryptedData or password is invalid.</exception>
        /// <exception cref="CryptographicException">Thrown if decryption fails due to invalid password or corrupted data.</exception>
        public byte[] Decrypt(byte[] encryptedData, string password)
        {
            if (encryptedData == null || encryptedData.Length <= SaltSize + IvSize)
                throw new ArgumentException("Encrypted data is invalid", nameof(encryptedData));
            
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Password cannot be null or empty", nameof(password));

            using var input = new MemoryStream(encryptedData);
            
            // Read salt and IV
            byte[] salt = new byte[SaltSize];
            byte[] iv = new byte[IvSize];
            input.Read(salt, 0, salt.Length);
            input.Read(iv, 0, iv.Length);
            
            // Derive key from password and salt
            byte[] key = DeriveKey(password, salt);
            
            using var output = new MemoryStream();
            
            // Decrypt the data
            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                
                using var cryptoStream = new CryptoStream(input, aes.CreateDecryptor(), CryptoStreamMode.Read);
                cryptoStream.CopyTo(output);
            }
            
            return output.ToArray();
        }
        
        private byte[] DeriveKey(string password, byte[] salt)
        {
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
            return pbkdf2.GetBytes(KeySize / 8);
        }
        
        private byte[] GenerateRandomBytes(int length)
        {
            byte[] bytes = new byte[length];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return bytes;
        }
    }
} 