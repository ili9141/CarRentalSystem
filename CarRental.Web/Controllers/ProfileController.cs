using CarRental.Core.Models;
using CarRental.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace CarRental.Web.Controllers
{
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProfileController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Profile (view or edit mode)
        public IActionResult Index(bool edit = false)
        {
            var clientId = HttpContext.Session.GetInt32("ClientId");
            if (clientId == null) return RedirectToAction("Login", "Auth");

            var client = _context.Clients
                .Include(c => c.Rentals)
                .ThenInclude(r => r.Vehicle)
                .FirstOrDefault(c => c.Id == clientId);

            if (client == null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Login", "Auth");
            }

            ViewBag.IsEditMode = edit;
            ViewBag.TotalRentals = client.Rentals?.Count ?? 0;
            ViewBag.ActiveRentals = client.Rentals?.Count(r => r.EndDate >= DateTime.Now) ?? 0;
            ViewBag.TotalSpent = client.Rentals?.Sum(r => r.TotalPrice) ?? 0;

            return View(client);
        }

        // POST: /Profile/Update (update profile)
        [HttpPost]
        public IActionResult Update(Client updatedClient, string currentPassword, string newPassword, string confirmPassword)
        {
            var clientId = HttpContext.Session.GetInt32("ClientId");
            if (clientId == null) return RedirectToAction("Login", "Auth");

            var existingClient = _context.Clients.Find(clientId);
            if (existingClient == null) return RedirectToAction("Login", "Auth");

            try
            {
                bool hasChanges = false;
                string message = "";

                // Update profile info if provided
                if (!string.IsNullOrEmpty(updatedClient.FirstName) && updatedClient.FirstName != existingClient.FirstName)
                {
                    existingClient.FirstName = updatedClient.FirstName;
                    hasChanges = true;
                }

                if (!string.IsNullOrEmpty(updatedClient.LastName) && updatedClient.LastName != existingClient.LastName)
                {
                    existingClient.LastName = updatedClient.LastName;
                    hasChanges = true;
                }

                if (!string.IsNullOrEmpty(updatedClient.Email) && updatedClient.Email != existingClient.Email)
                {
                    existingClient.Email = updatedClient.Email;
                    hasChanges = true;
                }

                if (updatedClient.Phone != existingClient.Phone)
                {
                    existingClient.Phone = updatedClient.Phone;
                    hasChanges = true;
                }

                if (updatedClient.Address != existingClient.Address)
                {
                    existingClient.Address = updatedClient.Address;
                    hasChanges = true;
                }

                if (updatedClient.LicenseNumber != existingClient.LicenseNumber)
                {
                    existingClient.LicenseNumber = updatedClient.LicenseNumber;
                    hasChanges = true;
                }

                // Handle password change
                if (!string.IsNullOrEmpty(newPassword))
                {
                    if (newPassword.Length < 6)
                    {
                        TempData["Error"] = "New password must be at least 6 characters.";
                        return RedirectToAction("Index", new { edit = true });
                    }

                    if (newPassword != confirmPassword)
                    {
                        TempData["Error"] = "New passwords do not match.";
                        return RedirectToAction("Index", new { edit = true });
                    }

                    // If you implement password verification, add it here:
                    // if (!VerifyPassword(currentPassword, existingClient.PasswordHash))
                    // {
                    //     TempData["Error"] = "Current password is incorrect.";
                    //     return RedirectToAction("Index", new { edit = true });
                    // }

                    existingClient.PasswordHash = HashPassword(newPassword);
                    hasChanges = true;
                    message += "Password changed. ";
                }

                if (hasChanges)
                {
                    _context.SaveChanges();

                    // Update session name
                    HttpContext.Session.SetString("ClientName", $"{existingClient.FirstName} {existingClient.LastName}");

                    TempData["Success"] = $"Profile updated successfully! {message}";
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["Info"] = "No changes were made.";
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error updating profile: {ex.Message}";
                return RedirectToAction("Index", new { edit = true });
            }
        }

        // POST: /Profile/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete()
        {
            var clientId = HttpContext.Session.GetInt32("ClientId");
            if (clientId == null) return RedirectToAction("Login", "Auth");

            var client = _context.Clients
                .Include(c => c.Rentals)
                .FirstOrDefault(c => c.Id == clientId);

            if (client == null) return RedirectToAction("Login", "Auth");

            // Check for active rentals
            var activeRentals = client.Rentals?.Any(r => r.EndDate >= DateTime.Now) ?? false;
            if (activeRentals)
            {
                TempData["Error"] = "Cannot delete account with active rentals. Please cancel all active rentals first.";
                return RedirectToAction("Index");
            }

            try
            {
                // Delete client (cascade will delete rentals and payments)
                _context.Clients.Remove(client);
                _context.SaveChanges();

                // Clear session
                HttpContext.Session.Clear();

                TempData["Success"] = "Your account has been deleted successfully.";
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting account: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // Helper method to hash password
        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
            }
        }

        // Helper method to verify password (if you need it)
        private bool VerifyPassword(string password, string storedHash)
        {
            var hashedPassword = HashPassword(password);
            return hashedPassword == storedHash;
        }
    }
}