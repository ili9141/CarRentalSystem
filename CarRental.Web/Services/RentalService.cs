using CarRental.Core.Models;
using CarRental.Data;
using Microsoft.EntityFrameworkCore;

namespace CarRental.Web.Services
{
    public class RentalService
    {
        private readonly ApplicationDbContext _context;

        public RentalService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Calculate rental price
        public decimal CalculateRentalPrice(DateTime startDate, DateTime endDate, decimal dailyRate)
        {
            var days = (endDate - startDate).Days;
            if (days < 1) days = 1; // Minimum 1 day
            return days * dailyRate;
        }

        // Check if vehicle is available for dates
        public bool IsVehicleAvailable(int vehicleId, DateTime startDate, DateTime endDate)
        {
            return !_context.Rentals.Any(r =>
                r.VehicleId == vehicleId &&
                r.StartDate <= endDate &&
                r.EndDate >= startDate);
        }

        // Create a new rental
        public Rental CreateRental(int clientId, int vehicleId, DateTime startDate, DateTime endDate, string paymentMethod = "Credit Card")
        {
            Console.WriteLine($"=== RentalService.CreateRental ===");
            Console.WriteLine($"Client: {clientId}, Vehicle: {vehicleId}");
            Console.WriteLine($"Dates: {startDate.ToShortDateString()} to {endDate.ToShortDateString()}");
            Console.WriteLine($"Payment Method: {paymentMethod}");

            var vehicle = _context.Vehicles.Find(vehicleId);
            if (vehicle == null)
            {
                Console.WriteLine("ERROR: Vehicle not found");
                throw new Exception("Vehicle not found");
            }

            Console.WriteLine($"Vehicle found: {vehicle.Brand} {vehicle.Model}");
            Console.WriteLine($"Vehicle in service: {vehicle.IsAvailable}"); // Renamed from "available" to "in service"

            if (!vehicle.IsAvailable)
            {
                Console.WriteLine("ERROR: Vehicle is not in service (maintenance)");
                throw new Exception("Vehicle is not in service (maintenance)");
            }

            if (!IsVehicleAvailable(vehicleId, startDate, endDate))
            {
                Console.WriteLine("ERROR: Vehicle already booked for dates");
                throw new Exception("Vehicle is already booked for selected dates");
            }

            // Calculate price
            var totalPrice = CalculateRentalPrice(startDate, endDate, vehicle.DailyRate);
            Console.WriteLine($"Total price: ${totalPrice}");

            // Create rental
            var rental = new Rental
            {
                ClientId = clientId,
                VehicleId = vehicleId,
                StartDate = startDate,
                EndDate = endDate,
                TotalPrice = totalPrice,
                CreatedAt = DateTime.Now
            };

            _context.Rentals.Add(rental);
            _context.SaveChanges();
            Console.WriteLine($"Rental saved with ID: {rental.Id}");

            // Create payment record
            var payment = new Payment
            {
                RentalId = rental.Id,
                Amount = totalPrice,
                PaymentDate = DateTime.Now,
                PaymentMethod = paymentMethod,
                Status = "Paid"
            };

            _context.Payments.Add(payment);
            Console.WriteLine("Payment record created");

            // IMPORTANT: DO NOT mark vehicle as unavailable here
            // Availability is now determined by date checks only
            Console.WriteLine($"Vehicle {vehicleId} remains in service (IsAvailable stays true)");

            _context.SaveChanges();
            Console.WriteLine("=== RentalService.CreateRental SUCCESS ===");

            return rental;
        }

        // Update vehicle maintenance status (not availability)
        public void UpdateVehicleMaintenanceStatus(int vehicleId, bool isInService)
        {
            var vehicle = _context.Vehicles.Find(vehicleId);
            if (vehicle != null)
            {
                vehicle.IsAvailable = isInService; // Now means "in service" not "available"
                _context.SaveChanges();
                Console.WriteLine($"Vehicle {vehicleId} maintenance status set to: {isInService}");
            }
        }

        // Get client's rentals
        public List<Rental> GetClientRentals(int clientId)
        {
            return _context.Rentals
                .Include(r => r.Vehicle)
                .Include(r => r.Payments)
                .Where(r => r.ClientId == clientId)
                .OrderByDescending(r => r.CreatedAt)
                .ToList();
        }

        // Check maintenance alerts
        public void CheckMaintenanceAlerts(int vehicleId, int currentKilometers)
        {
            var lastAlert = _context.MaintenanceAlerts
                .Where(m => m.VehicleId == vehicleId && !m.IsCompleted)
                .OrderByDescending(m => m.CreatedAt)
                .FirstOrDefault();

            if (lastAlert == null)
            {
                // First alert
                if (currentKilometers >= 5000)
                {
                    CreateMaintenanceAlert(vehicleId, 5000);
                }
            }
            else if (currentKilometers >= lastAlert.TargetKilometers + 5000)
            {
                CreateMaintenanceAlert(vehicleId, lastAlert.TargetKilometers + 5000);
            }
        }

        private void CreateMaintenanceAlert(int vehicleId, int targetKilometers)
        {
            var alert = new MaintenanceAlert
            {
                VehicleId = vehicleId,
                TargetKilometers = targetKilometers,
                CreatedAt = DateTime.Now,
                IsCompleted = false
            };

            _context.MaintenanceAlerts.Add(alert);

            // Mark vehicle as unavailable for maintenance
            var vehicle = _context.Vehicles.Find(vehicleId);
            if (vehicle != null)
            {
                vehicle.IsAvailable = false; // Means "in maintenance"
                Console.WriteLine($"Maintenance alert: Vehicle {vehicleId} marked as in maintenance");
            }

            _context.SaveChanges();
        }

        // Check if rental can be cancelled (not started yet)
        public bool CanCancelRental(int rentalId)
        {
            var rental = _context.Rentals.Find(rentalId);
            if (rental == null) return false;

            return rental.StartDate > DateTime.Now;
        }

        // Cancel rental
        public bool CancelRental(int rentalId)
        {
            var rental = _context.Rentals
                .Include(r => r.Vehicle)
                .FirstOrDefault(r => r.Id == rentalId);

            if (rental == null) return false;

            if (rental.StartDate <= DateTime.Now)
            {
                Console.WriteLine($"Cannot cancel rental {rentalId} - already started");
                return false;
            }

            // IMPORTANT: DO NOT change vehicle.IsAvailable here
            // Vehicle remains in service (IsAvailable stays true)
            Console.WriteLine($"Vehicle {rental.VehicleId} remains in service after cancellation");

            // Remove rental (cascade will remove payment)
            _context.Rentals.Remove(rental);
            _context.SaveChanges();

            Console.WriteLine($"Rental {rentalId} cancelled successfully");
            return true;
        }

        // New method: Check vehicle availability for specific dates
        public bool CheckVehicleAvailability(int vehicleId, DateTime startDate, DateTime endDate)
        {
            var vehicle = _context.Vehicles.Find(vehicleId);
            if (vehicle == null || !vehicle.IsAvailable) // IsAvailable now means "not in maintenance"
                return false;

            return IsVehicleAvailable(vehicleId, startDate, endDate);
        }
    }
}