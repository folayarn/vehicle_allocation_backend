using Vehicle_Information_System.Models;

using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Security.Cryptography;

namespace Vehicle_Information_System.Seeders
{
    public static class UserSeeder
    {
      

        // This is no longer an extension method
        public static void SeedUsers(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().HasData(GetPreconfiguredUsers());
        }

        private static IEnumerable<User> GetPreconfiguredUsers()
        {
            
            return new List<User>
            {
               
                new User
                {
                    UserId = Guid.NewGuid(),
                    Fullname = "Folayan",
                    Rank = "ASCII",
                    AccessLevel = "admin",

                    Zone="A",
                    Email = "folayanshola@gmail.com",
                    Svn ="57644",
                    Password = HashPassword("merlin12"), // Hashing the password
                    Phone = "0813847672",
                },
                
            };
        }

        private static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(password);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }
    }
}
