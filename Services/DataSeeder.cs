using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using Vehicle_Information_System.Models;
using Vehicle_Information_System.Seeders;

namespace Vehicle_Information_System.Services
{
    public class DataSeeder
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<DataSeeder> _logger;

        public DataSeeder(
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            ILogger<DataSeeder> logger)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
        }

        public async Task SeedAllDataAsync()
        {
            try
            {
                _logger.LogInformation("Starting data seeding process...");

                await SeedUsersAsync();
                await SeedVehicleDataAsync();
                await SeedAssetsAsync();

                _logger.LogInformation("✓ Data seeding completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "✗ Data seeding failed");
                throw;
            }
        }

        private async Task SeedUsersAsync()
        {
            try
            {
                if (!await _context.Users.AnyAsync())
                {
                    _logger.LogInformation("Seeding users...");

                    var users = GetPreconfiguredUsers();
                    await _context.Users.AddRangeAsync(users);
                    await _context.SaveChangesAsync();

                }
                else
                {
                    _logger.LogInformation("Users already exist. Skipping user seeding.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding users");
                throw;
            }
        }

        private async Task SeedVehicleDataAsync()
        {
            try
            {
                if (!await _context.VehicleAssessments.AnyAsync())
                {
                    var excelPath = Path.Combine(_environment.ContentRootPath, "documents", "vehicle_data.xlsx");

                    if (File.Exists(excelPath))
                    {
                        _logger.LogInformation("Seeding vehicles from: {Path}", excelPath);
                        var vehicles = VehicleAssessmentSeedData.GetSeedData(excelPath);

                        if (vehicles != null && vehicles.Any())
                        {
                            foreach (var vehicle in vehicles)
                            {
                                if (vehicle.Id == Guid.Empty)
                                {
                                    vehicle.Id = Guid.NewGuid();
                                }
                               
                            }

                            await _context.VehicleAssessments.AddRangeAsync(vehicles);
                            await _context.SaveChangesAsync();
                            _logger.LogInformation("✓ Seeded {Count} vehicles", vehicles.Count);
                        }
                        else
                        {
                            _logger.LogWarning("No vehicles found in Excel file");
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Vehicle Excel file not found at: {Path}", excelPath);
                    }
                }
                else
                {
                    _logger.LogInformation("Vehicles already exist. Skipping vehicle seeding.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding vehicles");
                throw;
            }
        }

        private async Task SeedAssetsAsync()
        {
            try
            {
                if (!await _context.Assets.AnyAsync())
                {
                    _logger.LogInformation("Seeding assets...");

                    var allAssets = new List<Asset>();

                    // Define all seeders with their file names
                    var seeders = new Dictionary<string, Func<string, List<Asset>>>
                    {
                        { "project.xlsx", ProjectSeeder.GetSeedData },
                        { "land.xlsx", LandSeeder.GetSeedData },
                        { "electrical.xlsx", ElectricalSeeder.GetSeedData }
                    };

                    foreach (var seeder in seeders)
                    {
                        try
                        {
                            var filePath = Path.Combine(_environment.ContentRootPath, "documents", seeder.Key);

                            if (File.Exists(filePath))
                            {
                                _logger.LogInformation("Loading assets from: {Path}", filePath);
                                var assets = seeder.Value(filePath);

                                if (assets != null && assets.Any())
                                {
                                    foreach (var asset in assets)
                                    {
                                        if (asset.Id == Guid.Empty)
                                        {
                                            asset.Id = Guid.NewGuid();
                                        }
                                        asset.CreatedAt = DateTime.UtcNow;

                                        if (string.IsNullOrWhiteSpace(asset.AssetType))
                                        {
                                            asset.AssetType = Path.GetFileNameWithoutExtension(seeder.Key);
                                        }

                                        if (string.IsNullOrWhiteSpace(asset.AssetStatus))
                                        {
                                            asset.AssetStatus = "active";
                                        }
                                    }

                                    allAssets.AddRange(assets);
                                    _logger.LogInformation("Loaded {Count} assets from {File}", assets.Count, seeder.Key);
                                }
                                else
                                {
                                    _logger.LogWarning("No assets loaded from {File}", seeder.Key);
                                }
                            }
                            else
                            {
                                _logger.LogWarning("Excel file not found: {Path}", filePath);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error seeding from {File}: {Error}", seeder.Key, ex.Message);
                        }
                    }

                    if (allAssets.Any())
                    {
                        // Remove duplicates by AssetName (keep first occurrence)
                        //var uniqueAssets = allAssets
                        //    .GroupBy(a => a.AssetName)
                        //    .Select(g => g.First())
                        //    .ToList();

                        await _context.Assets.AddRangeAsync(allAssets);
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("✓ Total assets seeded: {Count}", allAssets.Count);
                    }
                    else
                    {
                        _logger.LogWarning("⚠ No assets were loaded from any Excel files");
                    }
                }
                else
                {
                    _logger.LogInformation("Assets already exist. Skipping asset seeding.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding assets");
                throw;
            }
        }

        private IEnumerable<User> GetPreconfiguredUsers()
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
            };
        }

        private string HashPassword(string password)
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