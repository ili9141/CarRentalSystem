using System;
using System.Collections.Generic;

namespace CarRental.Core.Models
{
    public class Vehicle
    {
        public int Id { get; set; }
        public string Brand { get; set; }
        public string Model { get; set; }
        public int Year { get; set; }
        public VehicleCategory Category { get; set; }
        public string LicensePlate { get; set; }
        public decimal DailyRate { get; set; }
        public bool IsAvailable { get; set; }
        public DateTime LastMaintenanceDate { get; set; }
        public int Kilometers { get; set; }
        public string ImagePath { get; set; }
        public virtual ICollection<Rental> Rentals { get; set; }
    }
}