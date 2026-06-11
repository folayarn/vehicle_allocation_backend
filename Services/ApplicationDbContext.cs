using Microsoft.EntityFrameworkCore;
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
           

            // Seed Project/Construction assets
            try
            {
                string projectExcelPath = GetExcelFilePath("project.xlsx");
                if (File.Exists(projectExcelPath))
                {
                    var projectAssets = ProjectSeeder.GetSeedData(projectExcelPath);
                    allAssets.AddRange(projectAssets);
                    Console.WriteLine($"Loaded {projectAssets.Count} project/construction assets");
                }
                else
                {
                    Console.WriteLine($"Project Excel file not found at: {projectExcelPath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error seeding project data: {ex.Message}");
            }

            // Seed Land/Building assets
            try
            {
                string landExcelPath = GetExcelFilePath("land.xlsx");
                if (File.Exists(landExcelPath))
                {
                    var landAssets = LandSeeder.GetSeedData(landExcelPath);
                    allAssets.AddRange(landAssets);
                    Console.WriteLine($"Loaded {landAssets.Count} land/building assets");
                }
                else
                {
                    Console.WriteLine($"Land Excel file not found at: {landExcelPath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error seeding land data: {ex.Message}");
            }

            // Seed Electrical assets
            try
            {
                string electricalExcelPath = GetExcelFilePath("electrical.xlsx");
                if (File.Exists(electricalExcelPath))
                {
                    var electricalAssets = ElectricalSeeder.GetSeedData(electricalExcelPath);
                    allAssets.AddRange(electricalAssets);
                    Console.WriteLine($"Loaded {electricalAssets.Count} electrical assets");
                }
                else
                {
                    Console.WriteLine($"Electrical Excel file not found at: {electricalExcelPath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error seeding electrical data: {ex.Message}");
            }

            // Seed assets if any were loaded
            if (allAssets.Any())
            {
                // Assign GUIDs to each asset
                foreach (var asset in allAssets)
                {
                    asset.Id = Guid.NewGuid();
                    asset.CreatedAt = DateTime.UtcNow;
                    
                }

                modelBuilder.Entity<Asset>().HasData(allAssets);
                Console.WriteLine($"Total assets seeded: {allAssets.Count}");
            }
        }

        private string GetExcelFilePath(string fileName)
        {
            if (_environment != null)
            {
                // Check multiple possible locations
                string wwwrootPath = Path.Combine(_environment.WebRootPath, "documents", fileName);
                string contentRootPath = Path.Combine(_environment.ContentRootPath, "documents", fileName);
                string dataPath = Path.Combine(_environment.ContentRootPath, "Data", fileName);
                string seedDataPath = Path.Combine(_environment.ContentRootPath, "SeedData", fileName);

                if (File.Exists(wwwrootPath)) return wwwrootPath;
                if (File.Exists(contentRootPath)) return contentRootPath;
                if (File.Exists(dataPath)) return dataPath;
                if (File.Exists(seedDataPath)) return seedDataPath;
            }

            // Fallback paths for migration time
            string[] fallbackPaths = {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "documents", fileName),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Data", fileName),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "documents", fileName),
                Path.Combine(Directory.GetCurrentDirectory(), "documents", fileName),
                Path.Combine(Directory.GetCurrentDirectory(), "Data", fileName)
            };

            foreach (var path in fallbackPaths)
            {
                string fullPath = Path.GetFullPath(path);
                if (File.Exists(fullPath))
                    return fullPath;
            }

            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "documents", fileName);
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