using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.Utils
{
    public static class PasswordHasher
    {
        private static string GenerateSalt()
        {
            return Guid.NewGuid().ToString("N");
        }

        public static string HashPassword(string password)
        {
            var salt = GenerateSalt();
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(password + salt);
                var hash = sha256.ComputeHash(bytes);
                return $"{Convert.ToBase64String(hash)}:{salt}";
            }
        }

        

        public static bool VerifyPassword(string password, string hashedPasswordWithSalt)
        {
            var parts = hashedPasswordWithSalt.Split(':');
            if (parts.Length != 2)
                return false;

            var hash = parts[0];
            var salt = parts[1];

            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(password + salt);
                var hashOfInput = sha256.ComputeHash(bytes);
                var hashOfInputBase64 = Convert.ToBase64String(hashOfInput);
                return StringComparer.OrdinalIgnoreCase.Compare(hashOfInputBase64, hash) == 0;
            }
        }
    }
}
