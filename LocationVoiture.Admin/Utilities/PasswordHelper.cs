using System.Security.Cryptography;
using System.Text;

namespace LocationVoiture.Admin.Utilities
{
    public static class PasswordHelper
    {
        // Méthode pour hacher (SHA256)
        public static string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        // Méthode pour vérifier
        public static bool VerifyPassword(string inputPassword, string storedHash)
        {
            string inputHash = HashPassword(inputPassword);
            return inputHash == storedHash;
        }
    }
}