using CarRental.Core.Models;
using CarRental.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarRental.Web.Controllers
{
    public class RentalController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RentalController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Rental - List client's rentals
        public IActionResult Index()
        {
            var clientId = HttpContext.Session.GetInt32("ClientId");
            if (clientId == null) return RedirectToAction("Login", "Auth");

            var rentals = _context.Rentals
                .Include(r => r.Vehicle)
                .Include(r => r.Payments)
                .Where(r => r.ClientId == clientId)
                .OrderByDescending(r => r.CreatedAt)
                .ToList();

            return View(rentals);
        }

        // GET: /Rental/Create/{vehicleId} - Create rental form
        public IActionResult Create(int vehicleId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var clientId = HttpContext.Session.GetInt32("ClientId");
            if (clientId == null) return RedirectToAction("Login", "Auth");

            var vehicle = _context.Vehicles.Find(vehicleId);
            if (vehicle == null || !vehicle.IsAvailable)
            {
                TempData["Error"] = "Vehicle not available.";
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Vehicle = vehicle;
            ViewBag.ClientId = clientId;

            var model = new Rental
            {
                StartDate = startDate ?? DateTime.Today,
                EndDate = endDate ?? DateTime.Today.AddDays(3),
                VehicleId = vehicleId,
                ClientId = clientId.Value
            };

            return View(model);
        }

        // POST: /Rental/Create
        [HttpPost]
        public IActionResult Create(int VehicleId, int ClientId, DateTime StartDate, DateTime EndDate, string PaymentMethod)
        {
            Console.WriteLine("=== CREATE RENTAL POST ===");
            Console.WriteLine($"VehicleId: {VehicleId}, ClientId: {ClientId}");
            Console.WriteLine($"Start: {StartDate}, End: {EndDate}");
            Console.WriteLine($"Payment Method: {PaymentMethod}");

            var sessionClientId = HttpContext.Session.GetInt32("ClientId");
            if (sessionClientId == null) return RedirectToAction("Login", "Auth");

            // Validate payment method
            var validMethods = new[] { "Cash", "Debit Card", "Credit Card" };
            if (string.IsNullOrEmpty(PaymentMethod) || !validMethods.Contains(PaymentMethod))
            {
                PaymentMethod = "Cash"; // Default
            }

            // Validate dates
            if (EndDate <= StartDate)
            {
                TempData["Error"] = "End date must be after start date.";
                return RedirectToAction("Create", new { vehicleId = VehicleId });
            }

            try
            {
                // Create rental with payment using TRANSACTION
                var createdRental = CreateRentalWithPaymentTransaction(
                    sessionClientId.Value,
                    VehicleId,
                    StartDate,
                    EndDate,
                    PaymentMethod
                );

                Console.WriteLine($"SUCCESS! Rental ID: {createdRental.Id}, Payment created");

                TempData["Success"] = $"Rental created! Total: ${createdRental.TotalPrice:0.00} (Paid via {PaymentMethod})";
                return RedirectToAction("Details", new { id = createdRental.Id });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
                TempData["Error"] = ex.Message;
                return RedirectToAction("Create", new { vehicleId = VehicleId });
            }
        }

        // NEW: Create rental with payment using TRANSACTION
        private Rental CreateRentalWithPaymentTransaction(int clientId, int vehicleId, DateTime startDate, DateTime endDate, string paymentMethod)
        {
            var vehicle = _context.Vehicles.Find(vehicleId);
            if (vehicle == null) throw new Exception("Vehicle not found");

            if (!vehicle.IsAvailable) throw new Exception("Vehicle is not available");

            // Check availability
            var isAvailable = !_context.Rentals.Any(r =>
                r.VehicleId == vehicleId &&
                r.StartDate <= endDate &&
                r.EndDate >= startDate);

            if (!isAvailable) throw new Exception("Vehicle is already booked for selected dates");

            // Calculate price
            var days = (endDate - startDate).Days;
            if (days < 1) days = 1;
            var totalPrice = days * vehicle.DailyRate;

            // Use transaction to ensure BOTH rental AND payment are saved
            using var transaction = _context.Database.BeginTransaction();

            try
            {
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
                _context.SaveChanges(); // Save to get Rental.Id

                Console.WriteLine($"Rental created with ID: {rental.Id}");

                // Create payment with selected method
                var payment = new Payment
                {
                    RentalId = rental.Id,
                    Amount = totalPrice,
                    PaymentDate = DateTime.Now,
                    PaymentMethod = paymentMethod,
                    Status = "Paid"
                };

                _context.Payments.Add(payment);
                _context.SaveChanges();

                Console.WriteLine($"Payment created for Rental {rental.Id}: ${totalPrice} via {paymentMethod}");

                // Commit transaction
                transaction.Commit();
                Console.WriteLine("Transaction committed successfully");

                return rental;
            }
            catch (Exception ex)
            {
                // Rollback transaction on error
                transaction.Rollback();
                Console.WriteLine($"Transaction rolled back due to error: {ex.Message}");
                throw new Exception($"Failed to create rental: {ex.Message}");
            }
        }

        // OLD method - keep for compatibility but mark as obsolete
        private Rental CreateRentalWithPayment(int clientId, int vehicleId, DateTime startDate, DateTime endDate, string paymentMethod)
        {
            Console.WriteLine("WARNING: Using old CreateRentalWithPayment method. Use CreateRentalWithPaymentTransaction instead.");
            return CreateRentalWithPaymentTransaction(clientId, vehicleId, startDate, endDate, paymentMethod);
        }

        // GET: /Rental/Details/{id}
        public IActionResult Details(int id)
        {
            var clientId = HttpContext.Session.GetInt32("ClientId");
            if (clientId == null) return RedirectToAction("Login", "Auth");

            var rental = _context.Rentals
                .Include(r => r.Vehicle)
                .Include(r => r.Client)
                .Include(r => r.Payments)
                .FirstOrDefault(r => r.Id == id && r.ClientId == clientId);

            if (rental == null)
            {
                TempData["Error"] = "Rental not found.";
                return RedirectToAction("Index");
            }

            // Log payment info for debugging
            if (rental.Payments != null && rental.Payments.Any())
            {
                Console.WriteLine($"Rental {rental.Id} has {rental.Payments.Count} payment(s)");
                foreach (var payment in rental.Payments)
                {
                    Console.WriteLine($"  Payment: ${payment.Amount} via {payment.PaymentMethod} - {payment.Status}");
                }
            }
            else
            {
                Console.WriteLine($"WARNING: Rental {rental.Id} has NO payments!");
            }

            return View(rental);
        }

        // GET: /Rental/Cancel/{id}
        public IActionResult Cancel(int id)
        {
            var clientId = HttpContext.Session.GetInt32("ClientId");
            if (clientId == null) return RedirectToAction("Login", "Auth");

            var rental = _context.Rentals
                .Include(r => r.Vehicle)
                .Include(r => r.Payments)
                .FirstOrDefault(r => r.Id == id && r.ClientId == clientId);

            if (rental == null)
            {
                TempData["Error"] = "Rental not found.";
                return RedirectToAction("Index");
            }

            // Allow cancellation if rental hasn't ended yet
            if (rental.EndDate < DateTime.Now)
            {
                TempData["Error"] = "Cannot cancel a completed rental.";
                return RedirectToAction("Details", new { id });
            }

            // Log before cancellation
            Console.WriteLine($"Cancelling rental {rental.Id} with {rental.Payments?.Count ?? 0} payments");

            // Remove rental (cascade will remove payment)
            _context.Rentals.Remove(rental);
            _context.SaveChanges();

            Console.WriteLine($"Rental {rental.Id} cancelled successfully");

            TempData["Success"] = "Rental cancelled successfully.";
            return RedirectToAction("Index");
        }
    }
}