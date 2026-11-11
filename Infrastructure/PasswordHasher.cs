using System;
using System.Security.Cryptography;

namespace WukongWeb.Infrastructure
{
    /// <summary>
    /// PBKDF2-HMAC-SHA256 password hashing (100,000 iterations, 16-byte salt, 32-byte key).
    /// Matches the seed values in database/setup.sql.
    /// </summary>
    public static class PasswordHasher
    {
        private const int Iterations = 100000;
        private const int SaltSize = 16;
        private const int HashSize = 32;

        public static void Create(string password, out string hashBase64, out string saltBase64)
        {
            if (password == null) throw new ArgumentNullException(nameof(password));

            var salt = new byte[SaltSize];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            hashBase64 = Convert.ToBase64String(Derive(password, salt));
            saltBase64 = Convert.ToBase64String(salt);
        }

        public static bool Verify(string password, string hashBase64, string saltBase64)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hashBase64) || string.IsNullOrEmpty(saltBase64))
            {
                return false;
            }

            try
            {
                var expected = Convert.FromBase64String(hashBase64);
                var salt = Convert.FromBase64String(saltBase64);
                var actual = Derive(password, salt);
                return FixedTimeEquals(actual, expected);
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static byte[] Derive(string password, byte[] salt)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256))
            {
                return pbkdf2.GetBytes(HashSize);
            }
        }

        private static bool FixedTimeEquals(byte[] left, byte[] right)
        {
            if (left == null || right == null || left.Length != right.Length)
            {
                return false;
            }

            var difference = 0;
            for (var i = 0; i < left.Length; i++)
            {
                difference |= left[i] ^ right[i];
            }

            return difference == 0;
        }
    }
}
