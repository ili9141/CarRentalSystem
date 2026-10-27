# CarRental

A car rental system in .NET: a website where customers book cars, and a desktop
app where the agency runs the fleet. School project, EMSI, 2025.

## The solution

Four projects in `CarRentalSystem.slnx`:

| Project | What it is |
|---|---|
| `CarRental.Core` | The domain: vehicles and their categories, clients, rentals, payments, admin users, maintenance alerts. |
| `CarRental.Data` | Entity Framework Core on SQL Server: the `DbContext`, the migrations, seed data. |
| `CarRental.Web` | ASP.NET Core MVC site for customers: sign up, browse the cars, book one, see your rentals and profile. |
| `CarRental.Admin` | WinForms app for the agency: dashboard, vehicles, clients, rentals, payments, maintenance, admin accounts. |

## A few rules the code enforces

- **No double booking.** A car cannot be rented on dates that overlap an existing
  rental (`RentalService.IsVehicleAvailable`).
- **A car in maintenance cannot be booked**, whatever the dates.
- **Price** is the number of days times the car's daily rate, with a one-day minimum.
- **Maintenance alerts** are raised from the mileage: a new alert every 5,000 km
  past the last one.
- **Admin passwords** are hashed with BCrypt in the desktop app.

## Running it

You need the .NET 10 SDK, SQL Server (Express is enough) and Visual Studio 2022
or later for the WinForms project.

1. Create the database: run `Database/DBsetup.sql` in SQL Server Management
   Studio. `Database/setup.txt` has the detailed steps.
2. Point both apps at your server. The connection string is in
   `CarRental.Web/appsettings.json` and `CarRental.Data/DatabaseConfig.cs`. Both
   default to `localhost\SQLEXPRESS` with Windows authentication.
3. Open `CarRentalSystem.slnx`, then start `CarRental.Web` for the site or
   `CarRental.Admin` for the desktop app.

The site seeds a first admin account and two cars on its first start.

## Branches

`master` is the project as I wrote it. `A-Branch` is a teammate's continuation
(QR codes on rentals, reworked admin screens), kept separate.
