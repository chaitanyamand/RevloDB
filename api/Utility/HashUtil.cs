using System.Security.Cryptography;

namespace RevloDB.Utility
{
    public static class HashUtil
    {
        public static (string HashedPassword, string Salt) HashPassword(string password, int iterations = 100_000, int keySize = 32)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(16);

            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
            byte[] hash = pbkdf2.GetBytes(keySize);

            return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
        }

        public static bool VerifyPassword(string password, string storedHash, string storedSalt, int iterations = 100_000, int keySize = 32)
        {
            byte[] saltBytes = Convert.FromBase64String(storedSalt);
            byte[] expectedHash = Convert.FromBase64String(storedHash);

            using var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, iterations, HashAlgorithmName.SHA256);
            byte[] actualHash = pbkdf2.GetBytes(keySize);

            return CryptographicOperations.FixedTimeEquals(expectedHash, actualHash);
        }
    }
}
