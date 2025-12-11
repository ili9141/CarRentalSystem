using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CarRental.Data
{
    public static class DatabaseConfig
    {
        public static string GetConnectionString()
        {
            // Using your SSMS connection from the screenshot
            return "Server=localhost\\SQLEXPRESS;Database=CarRentalDB;Trusted_Connection=True;TrustServerCertificate=True;";
        }

        public static void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(GetConnectionString()));
        }
    }
}