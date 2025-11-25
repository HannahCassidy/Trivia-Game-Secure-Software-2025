using System.Security.Cryptography;

namespace TriviaApi.Controllers
{
    public static class PasswordHasher
    {
        private const int saltSize   = 16;   // 128 bit
        private const int keySize    = 32;   // 256 bit
        private const int iterations = 100_000;

        public static void createPasswordHash(string password, out byte[] hash, out byte[] salt)
        {
            using var rng = RandomNumberGenerator.Create();
            salt = new byte[saltSize];
            rng.GetBytes(salt);

            using var pbkdf2 = new Rfc2898DeriveBytes(
                password,
                salt,
                iterations,
                HashAlgorithmName.SHA256);

            hash = pbkdf2.GetBytes(keySize);
        }

        public static bool verifyPassword(string password, byte[] storedHash, byte[] storedSalt)
        {
            using var pbkdf2 = new Rfc2898DeriveBytes(
                password,
                storedSalt,
                iterations,
                HashAlgorithmName.SHA256);

            var computed = pbkdf2.GetBytes(keySize);
            return CryptographicOperations.FixedTimeEquals(computed, storedHash);
        }
    }
}
