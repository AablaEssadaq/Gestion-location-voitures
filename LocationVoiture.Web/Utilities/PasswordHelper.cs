using System.Security.Cryptography;
using System.Text;

namespace LocationVoiture.Web.Utilities
{
    public static class PasswordHelper
    {
        // Méthode pour hacher le mot de passe (SHA256)
        public static string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                // Convertir la chaîne en tableau d'octets
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));

                // Convertir le tableau d'octets en chaîne hexadécimale
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        // Vérifie si un mot de passe en clair correspond au hash stocké
        public static bool VerifyPassword(string inputPassword, string storedHash)
        {
            // On hache le mot de passe saisi et on compare avec celui en base
            string inputHash = HashPassword(inputPassword);

            // Comparaison simple de chaînes (sensible à la casse)
            return inputHash == storedHash;
        }
    }
}