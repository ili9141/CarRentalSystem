using System;

namespace CarRental.Core.Models
{
    public class Payment
    {
        public int Id { get; set; }
        public int RentalId { get; set; }
        public virtual Rental Rental { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string PaymentMethod { get; set; }
        public string Status { get; set; }
    }
}