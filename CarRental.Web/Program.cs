using CarRental.Data;
using CarRental.Web.Services;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;


var builder = WebApplication.CreateBuilder(args);

// ✅ QUESTPDF LICENSE (MUST BE HERE)
QuestPDF.Settings.License = LicenseType.Community;

// FORCE Kestrel to listen on ALL network interfaces
builder.WebHost.UseUrls("http://0.0.0.0:5152");

// Services
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddScoped<RentalService>();

var app = builder.Build();


// Seed the database - SIMPLE DIRECT APPROACH
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    // Ensure database exists
    dbContext.Database.EnsureCreated();

    // Check if admin exists
    if (!dbContext.AdminUsers.Any())
    {
        // Hash password for admin123 using SHA256
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var passwordBytes = System.Text.Encoding.UTF8.GetBytes("admin123");
        var hashBytes = sha256.ComputeHash(passwordBytes);
        var passwordHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

        // Create admin user
        dbContext.AdminUsers.Add(new CarRental.Core.Models.AdminUser
        {
            Username = "admin",
            Email = "admin@carrental.com",
            PasswordHash = passwordHash,
            CreatedAt = DateTime.Now
        });

        // Add sample vehicles
        dbContext.Vehicles.AddRange(
            new CarRental.Core.Models.Vehicle
            {
                Brand = "Toyota",
                Model = "Camry",
                Year = 2023,
                Category = CarRental.Core.Models.VehicleCategory.Sedan,
                LicensePlate = "TOY-123",
                DailyRate = 49.99m,
                IsAvailable = true,
                LastMaintenanceDate = DateTime.Now.AddMonths(-2),
                Kilometers = 12000,
                ImagePath = ""
            },
            new CarRental.Core.Models.Vehicle
            {
                Brand = "Ford",
                Model = "Explorer",
                Year = 2022,
                Category = CarRental.Core.Models.VehicleCategory.SUV,
                LicensePlate = "FRD-456",
                DailyRate = 69.99m,
                IsAvailable = true,
                LastMaintenanceDate = DateTime.Now.AddMonths(-1),
                Kilometers = 18000,
                ImagePath = ""
            },
            new CarRental.Core.Models.Vehicle
            {
                Brand = "Tesla",
                Model = "Model 3",
                Year = 2023,
                Category = CarRental.Core.Models.VehicleCategory.Electric,
                LicensePlate = "TES-789",
                DailyRate = 79.99m,
                IsAvailable = true,
                LastMaintenanceDate = DateTime.Now.AddMonths(-3),
                Kilometers = 8000,
                ImagePath = ""
            }
        );

        dbContext.SaveChanges();
        Console.WriteLine("Database seeded successfully!");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}


app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();


app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");



app.Run();