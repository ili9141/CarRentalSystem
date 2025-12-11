using Microsoft.EntityFrameworkCore;
using CarRental.Core.Models;

namespace CarRental.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<AdminUser> AdminUsers { get; set; }
        public DbSet<Client> Clients { get; set; }
        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<Rental> Rentals { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<MaintenanceAlert> MaintenanceAlerts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure decimal precision for money values
            modelBuilder.Entity<Payment>()
                .Property(p => p.Amount)
                .HasPrecision(18, 2); // 18 digits total, 2 decimal places

            modelBuilder.Entity<Rental>()
                .Property(r => r.TotalPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Vehicle>()
                .Property(v => v.DailyRate)
                .HasPrecision(18, 2);

            // Configure relationships
            modelBuilder.Entity<Rental>()
                .HasOne(r => r.Client)
                .WithMany(c => c.Rentals)
                .HasForeignKey(r => r.ClientId);

            modelBuilder.Entity<Rental>()
                .HasOne(r => r.Vehicle)
                .WithMany(v => v.Rentals)
                .HasForeignKey(r => r.VehicleId);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Rental)
                .WithMany(r => r.Payments)
                .HasForeignKey(p => p.RentalId);

            modelBuilder.Entity<MaintenanceAlert>()
                .HasOne(m => m.Vehicle)
                .WithMany()
                .HasForeignKey(m => m.VehicleId);
        }
    }
}