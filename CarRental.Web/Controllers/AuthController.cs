using CarRental.Core.Models;
using CarRental.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace CarRental.Web.Controllers
{
    public class AuthController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuthController> _logger;

        public AuthController(ApplicationDbContext context, ILogger<AuthController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: /Auth/Login
        public IActionResult Login()
        {
            return View();
        }

        // POST: /Auth/Login
        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            _logger.LogInformation($"Login attempt for: {email}");

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                TempData["Error"] = "Please enter both email and password.";
                return View();
            }

            var client = _context.Clients.FirstOrDefault(c => c.Email == email);

            if (client == null || string.IsNullOrEmpty(client.PasswordHash))
            {
                TempData["Error"] = "Invalid email or password.";
                return View();
            }

            if (HashPassword(password) == client.PasswordHash)
            {
                HttpContext.Session.SetInt32("ClientId", client.Id);
                HttpContext.Session.SetString("ClientName", $"{client.FirstName} {client.LastName}");
                return RedirectToAction("Index", "Home");
            }

            TempData["Error"] = "Invalid email or password.";
            return View();
        }

        // GET: /Auth/Register
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Auth/Register - SIMPLIFIED
        [HttpPost]
        public IActionResult Register(
            [Bind("FirstName,LastName,Email,Phone,Address,LicenseNumber")] Client client,
            string password,
            string confirmPassword)
        {
            _logger.LogInformation($"Registration attempt for: {client.Email}");

            // Basic validation
            if (password != confirmPassword)
            {
                TempData["Error"] = "Passwords do not match.";
                return View(client);
            }

            if (password.Length < 6)
            {
                TempData["Error"] = "Password must be at least 6 characters.";
                return View(client);
            }

            // Check required fields
            if (string.IsNullOrEmpty(client.FirstName) ||
                string.IsNullOrEmpty(client.LastName) ||
                string.IsNullOrEmpty(client.Email) ||
                string.IsNullOrEmpty(client.Phone) ||
                string.IsNullOrEmpty(client.Address) ||
                string.IsNullOrEmpty(client.LicenseNumber))
            {
                TempData["Error"] = "All fields are required.";
                return View(client);
            }

            // Check if email exists
            if (_context.Clients.Any(c => c.Email == client.Email))
            {
                TempData["Error"] = "Email already registered.";
                return View(client);
            }

            try
            {
                // Set password and created date
                client.PasswordHash = HashPassword(password);
                client.CreatedAt = DateTime.Now;

                // Save to database
                _context.Clients.Add(client);
                _context.SaveChanges();

                _logger.LogInformation($"Client registered successfully: {client.Email}, ID: {client.Id}");

                // Auto-login
                HttpContext.Session.SetInt32("ClientId", client.Id);
                HttpContext.Session.SetString("ClientName", $"{client.FirstName} {client.LastName}");

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error registering client: {ex.Message}");
                TempData["Error"] = "Error creating account. Please try again.";
                return View(client);
            }
        }

        // GET: /Auth/Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
            }
        }
    }
}