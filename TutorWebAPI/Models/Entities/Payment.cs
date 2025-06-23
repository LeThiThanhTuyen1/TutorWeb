namespace TutorWebAPI.Models.Entities
{
    public class Payment
    {
        public int Id { get; set; }
        public long EnrollmentId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } //VNPAY or Stripe
        public string TransactionId { get; set; } 
        public string? OrderId { get; set; } 
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? VnPayResponseCode { get; set; } 
        public Enrollment Enrollment { get; set; }
    }
}