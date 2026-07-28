using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using Vehicle_Information_System.Models;
using Vehicle_Information_System.Services;

namespace Vehicle_Information_System.Seeders
{
    public static class UserSeeder
    {
        // Keep this for ModelBuilder usage (if you want to keep it)
        public static void SeedUsers(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().HasData(GetPreconfiguredUsers());
        }

        // Add this new method for DbContext usage
        public static async Task SeedUsersAsync(ApplicationDbContext context)
        {
            // Check if users already exist
            if (!await context.Users.AnyAsync())
            {
                var users = GetPreconfiguredUsers();
                await context.Users.AddRangeAsync(users);
                await context.SaveChangesAsync();
                Console.WriteLine($"✓ Seeded {users.Count()} users");
            }
            else
            {
                Console.WriteLine("Users already exist. Skipping user seeding.");
            }
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
                    Zone = "A",
                    Email = "folayanshola@gmail.com",
                    Svn = "57644",
                    Password = HashPassword("merlin12"),
                    Phone = "0813847672",
                },
                // Add more users here if needed
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