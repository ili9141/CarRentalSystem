using CarRental.Core.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace CarRental.Data
{
    public static class SeedData
    {
        public static void Initialize(ApplicationDbContext context)
        {
            // Ensure database is created
            context.Database.EnsureCreated();

            // Add default admin if none exists
            if (!context.AdminUsers.Any())
            {
                context.AdminUsers.Add(new AdminUser
                {
                    Username = "admin",
                    Email = "admin@carrental.com",
                    PasswordHash = HashPassword("admin123"),
                    CreatedAt = DateTime.Now
                });

                // Add some test vehicles
                context.Vehicles.AddRange(
                    new Vehicle
                    {
                        Brand = "Toyota",
                        Model = "Camry",
                        Year = 2023,
                        Category = VehicleCategory.Sedan,
                        LicensePlate = "TOY-001",
                        DailyRate = 49.99m,
                        IsAvailable = true,
                        LastMaintenanceDate = DateTime.Now.AddMonths(-2),
                        Kilometers = 15000,
                        ImagePath = ""
                    },
                    new Vehicle
                    {
                        Brand = "Honda",
                        Model = "CR-V",
                        Year = 2022,
                        Category = VehicleCategory.SUV,
                        LicensePlate = "HON-002",
                        DailyRate = 59.99m,
                        IsAvailable = true,
                        LastMaintenanceDate = DateTime.Now.AddMonths(-1),
                        Kilometers = 20000,
                        ImagePath = ""
                    }
                );

                context.SaveChanges();
            }
        }

        private static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
            }
        }
    }
}