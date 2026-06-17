using Microsoft.EntityFrameworkCore;
using Serilog;
using Vehicle_Information_System.Models;
using Vehicle_Information_System.Seeders;

namespace Vehicle_Information_System.Services
{
    public class ApplicationDbContext : DbContext
    {
        private readonly IWebHostEnvironment? _environment;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Constructor with IWebHostEnvironment for seeding
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options,
            IWebHostEnvironment environment)
            : base(options)
        {
            _environment = environment;
        }

        public DbSet<MaintenanceRequest> MaintenanceRequests { get; set; }
        public DbSet<Item> Items { get; set; }
        public DbSet<SparePartRequest> SparePartRequests { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<StoreUser> StoreUsers { get; set; }
        public DbSet<AssetUser> AssetUsers { get; set; }
        public DbSet<AccomodationUser> AccomodationUsers { get; set; }
        public DbSet<VehicleAssessment> VehicleAssessments { get; set; }
        public DbSet<Allocation> Allocations { get; set; }
        public DbSet<Driver> Drivers { get; set; }
        public DbSet<Remark> Remarks { get; set; }
        public DbSet<LogBook> LogBooks { get; set; }
        public DbSet<IncidentReport> IncidentReports { get; set; }
        public DbSet<MaintenanceReport> MaintenanceReports { get; set; }
        public DbSet<ActivityLog> ActivityLogs { get; set; }

        // New DbSets for assets
        public DbSet<Asset> Assets { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Seed users
            UserSeeder.SeedUsers(modelBuilder);

            // Configure Email as unique in User table
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(e => e.Email).IsUnique();
            });

            // Configure VehicleAssessment table
            modelBuilder.Entity<VehicleAssessment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Timestamp).IsRequired();
                entity.Property(e => e.RegistrationNumber).HasMaxLength(50);
                entity.Property(e => e.ChassisNumber).HasMaxLength(100);
                entity.Property(e => e.VehicleTypeModel).HasMaxLength(100);
                entity.Property(e => e.EngineNumber).HasMaxLength(50);
                entity.Property(e => e.VehicleLocation).HasMaxLength(200);
                entity.Property(e => e.Command).HasMaxLength(200);
                entity.Property(e => e.Zone).HasMaxLength(10);
                entity.Property(e => e.Comments).HasMaxLength(500);
                entity.Property(e => e.Condition).HasMaxLength(50);
                entity.Property(e => e.Remark).HasMaxLength(100);

                // Add indexes
                entity.HasIndex(e => e.RegistrationNumber);
                entity.HasIndex(e => e.Zone);
                entity.HasIndex(e => e.ChassisNumber);
            });

            // Configure Asset table
            modelBuilder.Entity<Asset>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.SerialNumber).HasMaxLength(100);
                entity.Property(e => e.AssetName).HasMaxLength(500).IsRequired();
                entity.Property(e => e.Remark).HasMaxLength(500);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.Zone).HasMaxLength(50);
                entity.Property(e => e.Command).HasMaxLength(200);
                entity.Property(e => e.RenovationCost).HasColumnType("decimal(18,2)");
                entity.Property(e => e.ConstructionCost).HasColumnType("decimal(18,2)");
                entity.Property(e => e.LastRenovationCost).HasColumnType("decimal(18,2)");
                entity.Property(e => e.AcquisitionCost).HasColumnType("decimal(18,2)");
                entity.Property(e => e.BrandName).HasMaxLength(100);
                entity.Property(e => e.Location).HasMaxLength(500);
                entity.Property(e => e.CurrentPhysicalCondition).HasMaxLength(100);
                entity.Property(e => e.Category).HasMaxLength(100);
                entity.Property(e => e.BuildingType).HasMaxLength(100);
                entity.Property(e => e.Capacity).HasMaxLength(100);
                entity.Property(e => e.AssetStatus).HasMaxLength(30);
                entity.Property(e => e.PhysicalLocation).HasMaxLength(300);
                entity.Property(e => e.Condition).HasMaxLength(30);
                entity.Property(e => e.InsurancePolicyNo).HasMaxLength(100);
                entity.Property(e => e.LitigationStatus).HasMaxLength(200);
                entity.Property(e => e.AvailableDocument);

                // Add indexes
                entity.HasIndex(e => e.AssetName);
                entity.HasIndex(e => e.Zone);
                entity.HasIndex(e => e.Command);
                entity.HasIndex(e => e.SerialNumber);
                entity.HasIndex(e => e.AssetStatus);
            });

            // Seed all asset data
            SeedVehicleData(modelBuilder);
            SeedAllAssets(modelBuilder);
        }

        private void SeedVehicleData(ModelBuilder modelBuilder)
        {
            try
            {
                // Get the Excel file path (adjust as needed)
                string excelPath;

                if (_environment != null)
                {
                    excelPath = Path.Combine(_environment.ContentRootPath, "documents", "vehicle_data.xlsx");
                }
                else
                {
                    // Fallback path for migration time
                    excelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Data", "NCS VEHICLE ASSESSMENT REPORT.xlsx");
                }

                if (File.Exists(excelPath))
                {
                    var vehicles = VehicleAssessmentSeedData.GetSeedData(excelPath);

                    // Use HasData to seed during migration
                    modelBuilder.Entity<VehicleAssessment>().HasData(vehicles);

                    Console.WriteLine($"Prepared {vehicles.Count} vehicles for seeding");
                }
                else
                {
                    Console.WriteLine($"Excel file not found at: {excelPath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error seeding vehicle data");
            }
        }
        private void SeedAllAssets(ModelBuilder modelBuilder)
        {
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
                    string filePath = GetExcelFilePath(seeder.Key);

                    if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                    {
                        var assets = seeder.Value(filePath);
                        if (assets != null && assets.Any())
                        {
                            allAssets.AddRange(assets);
                            Console.WriteLine($"Loaded {assets.Count} assets from {seeder.Key}");
                        }
                        else
                        {
                            Console.WriteLine($"No assets loaded from {seeder.Key}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"File not found: {seeder.Key}");
                        Console.WriteLine($"  Looked at: {filePath ?? "null"}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error seeding {seeder.Key}: {ex.Message}");
                    Console.WriteLine($"  Stack trace: {ex.StackTrace}");
                }
            }

            if (allAssets.Any())
            {
                foreach (var asset in allAssets)
                {
                    asset.Id = Guid.NewGuid();
                    asset.CreatedAt = DateTime.UtcNow;
                }

                modelBuilder.Entity<Asset>().HasData(allAssets);
                Console.WriteLine($"✓ Total assets seeded: {allAssets.Count}");
            }
            else
            {
                Console.WriteLine("⚠ No assets were loaded from any Excel files");
            }
        }

        private string GetExcelFilePath(string fileName)
        {
            // Try to get the content root path from environment
            string contentRootPath = null;

            if (_environment != null && !string.IsNullOrEmpty(_environment.ContentRootPath))
            {
                contentRootPath = _environment.ContentRootPath;
            }

            // If still null, try current directory
            if (string.IsNullOrEmpty(contentRootPath))
            {
                contentRootPath = Directory.GetCurrentDirectory();
            }

            // List of all possible paths to check
            var possiblePaths = new List<string>();

            // Primary location: documents folder in content root
            possiblePaths.Add(Path.Combine(contentRootPath, "documents", fileName));

            // Other common locations
            possiblePaths.Add(Path.Combine(contentRootPath, "Data", fileName));
            possiblePaths.Add(Path.Combine(contentRootPath, "SeedData", fileName));
            possiblePaths.Add(Path.Combine(contentRootPath, "wwwroot", "documents", fileName));

            // Also check from base directory (for when running from different locations)
            possiblePaths.Add(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "documents", fileName));
            possiblePaths.Add(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "documents", fileName));

            // Check all paths and return the first one that exists
            foreach (var path in possiblePaths)
            {
                try
                {
                    var fullPath = Path.GetFullPath(path);
                    if (File.Exists(fullPath))
                    {
                        Log.Debug("Found file at: {Path}", fullPath);
                        return fullPath;
                    }
                }
                catch (Exception ex)
                {
                    Log.Debug("Error checking path {Path}: {Error}", path, ex.Message);
                    // Continue to next path
                }
            }

            // If no file found, return the primary path (caller will handle)
            var defaultPath = Path.Combine(contentRootPath, "documents", fileName);
            Log.Warning("File not found in any location. Default path: {Path}", defaultPath);
            return defaultPath;
        }

        // Helper method to reseed assets at runtime
        public async Task ReseedAssetsAsync()
        {
            // Remove existing assets
            Assets.RemoveRange(Assets);
            await SaveChangesAsync();

            // Reload assets
            var allAssets = new List<Asset>();

            string projectExcelPath = GetExcelFilePath("project.xlsx");
            if (File.Exists(projectExcelPath))
            {
                allAssets.AddRange(ProjectSeeder.GetSeedData(projectExcelPath));
            }

            string landExcelPath = GetExcelFilePath("land.xlsx");
            if (File.Exists(landExcelPath))
            {
                allAssets.AddRange(LandSeeder.GetSeedData(landExcelPath));
            }

            string electricalExcelPath = GetExcelFilePath("electrical.xlsx");
            if (File.Exists(electricalExcelPath))
            {
                allAssets.AddRange(ElectricalSeeder.GetSeedData(electricalExcelPath));
            }

            if (allAssets.Any())
            {
                foreach (var asset in allAssets)
                {
                    asset.Id = Guid.NewGuid();
                    asset.CreatedAt = DateTime.UtcNow;
                    
                }

                await Assets.AddRangeAsync(allAssets);
                await SaveChangesAsync();
                Console.WriteLine($"Reseeded {allAssets.Count} assets");
            }
        }
    }
}