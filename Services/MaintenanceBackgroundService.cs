using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Vehicle_Information_System.Models;

namespace Vehicle_Information_System.Services
{
    public class MaintenanceBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<MaintenanceBackgroundService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromHours(6); 
        private const int MAINTENANCE_INTERVAL_KM = 6000;
        private const int WARNING_THRESHOLD_KM = 1000;

        public MaintenanceBackgroundService(
            IServiceProvider services,
            ILogger<MaintenanceBackgroundService> logger)
        {
            _services = services;
            _logger = logger;
            _logger.LogWarning("MaintenanceBackgroundService CONSTRUCTOR CALLED");
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogWarning("MaintenanceBackgroundService STARTING");
            return base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogWarning("MaintenanceBackgroundService EXECUTE ASYNC STARTED");

            // Wait for 10 seconds to ensure the app is fully initialized
            await Task.Delay(10000, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Starting maintenance check cycle at {Time}", DateTime.Now);
                    await PerformMaintenanceCheck(stoppingToken);
                    _logger.LogInformation("Maintenance check completed. Waiting {Interval} before next check", _checkInterval);
                    await Task.Delay(_checkInterval, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during maintenance check cycle");
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                }
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogWarning("MaintenanceBackgroundService STOPPING");
            return base.StopAsync(cancellationToken);
        }

        private async Task PerformMaintenanceCheck(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting maintenance status check at {Time}", DateTime.Now);

            // Create a new scope to get fresh DbContext
            using (var scope = _services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                try
                {
                    // Check if database is available
                    if (!await context.Database.CanConnectAsync(stoppingToken))
                    {
                        _logger.LogError("Cannot connect to database");
                        return;
                    }

                    // Get all vehicles
                    var vehicleList = await context.VehicleAssessments.ToListAsync(stoppingToken);
                    _logger.LogInformation("Found {Count} vehicles to check", vehicleList.Count);

                    if (vehicleList.Count == 0)
                    {
                        _logger.LogWarning("No vehicles found in database");
                        return;
                    }

                    int updatedCount = 0;
                    int dueCount = 0;
                    int overdueCount = 0;

                    foreach (var vehicle in vehicleList)
                    {
                        try
                        {
                            _logger.LogDebug("Processing vehicle: {ChassisNumber}", vehicle.ChassisNumber);

                            // Get the latest logbook entry with mileage
                            var latestLogBook = await context.LogBooks
                                .Where(l => l.VehicleId == vehicle.Id && l.MileageAfter != null)
                                .OrderByDescending(l => l.Created)
                                .FirstOrDefaultAsync(stoppingToken);

                            // Get current mileage
                            decimal currentMileage = vehicle.CurrentMileage ?? vehicle.InitialMileage ?? 0;

                            // Try to get mileage from logbook if available
                            if (latestLogBook != null && latestLogBook.MileageAfter !=null)
                            {
                                
                                    currentMileage = latestLogBook.MileageAfter;
                                    vehicle.CurrentMileage = currentMileage;
                                    _logger.LogDebug("Vehicle {ChassisNumber} current mileage: {Mileage} (from logbook)",
                                        vehicle.ChassisNumber, currentMileage);
                                
                            }
                            else
                            {
                                _logger.LogDebug("Vehicle {ChassisNumber} current mileage: {Mileage} (from stored value)",
                                    vehicle.ChassisNumber, currentMileage);
                            }

                            // Get the latest maintenance report
                            var latestMaintenance = await context.MaintenanceReports
                                .Where(m => m.VehicleId == vehicle.Id)
                                .OrderByDescending(m => m.Created)
                                .FirstOrDefaultAsync(stoppingToken);

                            // Use the most recent maintenance mileage
                            decimal lastMaintenanceMileage = vehicle.LastMaintenanceMileage ?? vehicle.InitialMileage ?? 0;

                            // Check if we have a maintenance report with mileage
                            if (latestMaintenance != null )
                            {
                                lastMaintenanceMileage = latestMaintenance.LastMaintenanceMileage;
                                vehicle.LastMaintenanceMileage = lastMaintenanceMileage;
                                vehicle.LastMaintenanceDate = latestMaintenance.Created;
                                _logger.LogDebug("Vehicle {ChassisNumber} last maintenance mileage: {Mileage} (from report)",
                                    vehicle.ChassisNumber, lastMaintenanceMileage);
                            }
                            else if (vehicle.LastMaintenanceMileage.HasValue)
                            {
                                lastMaintenanceMileage = vehicle.LastMaintenanceMileage.Value;
                                _logger.LogDebug("Vehicle {ChassisNumber} last maintenance mileage: {Mileage} (from vehicle record)",
                                    vehicle.ChassisNumber, lastMaintenanceMileage);
                            }
                            else
                            {
                                _logger.LogDebug("Vehicle {ChassisNumber} has no maintenance record", vehicle.ChassisNumber);
                            }

                            // Calculate km since last maintenance
                            decimal kmSinceLastMaintenance = currentMileage - lastMaintenanceMileage;
                            int kmRemaining = (int)Math.Max(0, MAINTENANCE_INTERVAL_KM - kmSinceLastMaintenance);

                            vehicle.MaintenanceDueInKm = kmRemaining;
                            string oldStatus = vehicle.MaintenanceStatus;

                            // Determine maintenance status
                            if (lastMaintenanceMileage > 0 || vehicle.InitialMileage > 0)
                            {
                                if (kmSinceLastMaintenance >= MAINTENANCE_INTERVAL_KM)
                                {
                                    var overdueKm = kmSinceLastMaintenance - MAINTENANCE_INTERVAL_KM;
                                    vehicle.MaintenanceStatus = "Overdue";
                                    vehicle.RecommendedAction = $"CRITICAL: Vehicle has exceeded the {MAINTENANCE_INTERVAL_KM}km maintenance interval by {overdueKm:F0}km. IMMEDIATE maintenance required!";
                                    overdueCount++;
                                    _logger.LogWarning("Vehicle {ChassisNumber} is OVERDUE by {OverdueKm}km",
                                        vehicle.ChassisNumber, overdueKm);
                                }
                                else if (kmSinceLastMaintenance >= MAINTENANCE_INTERVAL_KM - WARNING_THRESHOLD_KM)
                                {
                                    vehicle.MaintenanceStatus = "Due";
                                    vehicle.RecommendedAction = $"WARNING: Maintenance due in {kmRemaining}km. {(MAINTENANCE_INTERVAL_KM - kmSinceLastMaintenance):F0}km driven since last service.";
                                    dueCount++;
                                    _logger.LogWarning("Vehicle {ChassisNumber} is DUE for maintenance in {KmRemaining}km",
                                        vehicle.ChassisNumber, kmRemaining);
                                }
                                else
                                {
                                    vehicle.MaintenanceStatus = "OK";
                                    vehicle.RecommendedAction = $"Vehicle is in good condition. Next maintenance due in {kmRemaining}km.";
                                }
                            }
                            else
                            {
                                vehicle.MaintenanceStatus = "OK";
                                vehicle.RecommendedAction = $"No maintenance record found. Please record initial maintenance mileage.";
                                _logger.LogInformation("Vehicle {ChassisNumber} has no maintenance record", vehicle.ChassisNumber);
                            }

                            // Log status change
                            if (oldStatus != vehicle.MaintenanceStatus)
                            {
                                _logger.LogWarning(
                                    "Vehicle {ChassisNumber} status changed: {OldStatus} -> {NewStatus} | KM since last maintenance: {KmSince:F0}km",
                                    vehicle.ChassisNumber ?? vehicle.Id.ToString(),
                                    oldStatus ?? "None",
                                    vehicle.MaintenanceStatus,
                                    kmSinceLastMaintenance);
                            }

                            vehicle.UpdatedAt = DateTime.UtcNow;
                            updatedCount++;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing vehicle {VehicleId}", vehicle.Id);
                        }
                    }

                    // Save all changes
                    if (updatedCount > 0)
                    {
                        await context.SaveChangesAsync(stoppingToken);
                        _logger.LogInformation("Saved {UpdatedCount} vehicle updates", updatedCount);
                    }

                    // Log summary
                    _logger.LogInformation(
                        "MAINTENANCE CHECK SUMMARY - Total: {Total}, Updated: {Updated}, Due: {Due}, Overdue: {Overdue}, OK: {Ok}",
                        vehicleList.Count,
                        updatedCount,
                        dueCount,
                        overdueCount,
                        vehicleList.Count - (dueCount + overdueCount));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in maintenance check process");
                    throw;
                }
            }
        }
    }
}