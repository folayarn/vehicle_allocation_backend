using Microsoft.EntityFrameworkCore;
using Vehicle_Information_System.Models;
using Vehicle_Information_System.SeedData;
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
        public DbSet<VehicleAssessment> VehicleAssessments { get; set; }
        public DbSet<Allocation> Allocations { get; set; }
        public DbSet<Driver> Drivers { get; set; }
        public DbSet<Remark> Remarks { get; set; }
        public DbSet<LogBook> LogBooks { get; set; }
        public DbSet<IncidentReport> IncidentReports { get; set; }
        public DbSet<MaintenanceReport> MaintenanceReports { get; set; }
        public DbSet<ActivityLog> ActivityLogs { get; set; }
       





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

            // Seed vehicle data from Excel
            SeedVehicleData(modelBuilder);
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
    }
}