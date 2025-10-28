using System.Security.Cryptography;
using System.Text;

namespace AuthApp.DataAccess
{
    public static class Helpers
    {
        // Helper to compute deterministic password hashes for seeding.
        public static string ComputeSha256Hash(string raw)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
            return Convert.ToBase64String(bytes);
        }
    }
}
