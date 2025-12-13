using CarRental.Core.Models;
using CarRental.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarRental.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index(string category = null, decimal? minPrice = null, decimal? maxPrice = null, int page = 1, int pageSize = 10)
        {
            // Get today's date for availability check
            var today = DateTime.Today;

            // Start with base query - vehicles not in maintenance
            var query = _context.Vehicles
                .Include(v => v.Rentals)
                .Where(v => v.IsAvailable);  // Not in maintenance

            // Apply Category Filter
            if (!string.IsNullOrEmpty(category) && Enum.TryParse<VehicleCategory>(category, out var categoryEnum))
            {
                query = query.Where(v => v.Category == categoryEnum);
                ViewBag.SelectedCategory = category;
            }

            // Apply Price Range Filters
            if (minPrice.HasValue && minPrice > 0)
            {
                query = query.Where(v => v.DailyRate >= minPrice.Value);
                ViewBag.MinPrice = minPrice.Value;
            }

            if (maxPrice.HasValue && maxPrice > 0)
            {
                query = query.Where(v => v.DailyRate <= maxPrice.Value);
                ViewBag.MaxPrice = maxPrice.Value;
            }

            // Calculate total items for pagination
            var totalItems = query.Count();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            // Ensure page is within valid range
            page = Math.Max(1, Math.Min(page, totalPages));

            // Apply pagination
            var vehicles = query
                .OrderBy(v => v.Id)  // Order by ID (you can change this)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Check rental status for each vehicle
            foreach (var vehicle in vehicles)
            {
                // Check if vehicle is currently rented (overlaps with today)
                var isRentedToday = vehicle.Rentals.Any(r =>
                    r.StartDate <= today &&
                    r.EndDate >= today);

                // Store in ViewData for the view
                ViewData[$"IsRented_{vehicle.Id}"] = isRentedToday;

                // Get rental end date for display if rented
                if (isRentedToday)
                {
                    var currentRental = vehicle.Rentals
                        .FirstOrDefault(r => r.StartDate <= today && r.EndDate >= today);
                    if (currentRental != null)
                    {
                        ViewData[$"RentalEndDate_{vehicle.Id}"] = currentRental.EndDate;
                    }
                }
            }

            // Pass today's date to view
            ViewBag.Today = today.ToString("yyyy-MM-dd");

            // Pass categories for filter dropdown
            ViewBag.Categories = Enum.GetValues(typeof(VehicleCategory))
                .Cast<VehicleCategory>()
                .ToList();

            // Pass price range info for display
            ViewBag.MinDailyRate = _context.Vehicles.Min(v => v.DailyRate);
            ViewBag.MaxDailyRate = _context.Vehicles.Max(v => v.DailyRate);
            ViewBag.AvgDailyRate = _context.Vehicles.Average(v => v.DailyRate);

            // Pass pagination info to view
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalItems;
            ViewBag.TotalPages = totalPages;
            ViewBag.HasPreviousPage = page > 1;
            ViewBag.HasNextPage = page < totalPages;

            return View(vehicles);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        // Vehicle details page
        public IActionResult Details(int id)
        {
            var vehicle = _context.Vehicles
                .Include(v => v.Rentals)
                .FirstOrDefault(v => v.Id == id);

            if (vehicle == null)
            {
                return NotFound();
            }

            // Calculate next available date if vehicle is rented
            var today = DateTime.Today;
            var currentRental = vehicle.Rentals
                .FirstOrDefault(r => r.StartDate <= today && r.EndDate >= today);

            if (currentRental != null)
            {
                ViewBag.NextAvailableDate = currentRental.EndDate.AddDays(1);
                ViewBag.IsCurrentlyRented = true;
            }
            else
            {
                ViewBag.NextAvailableDate = today;
                ViewBag.IsCurrentlyRented = false;
            }

            return View(vehicle);
        }
    }
}