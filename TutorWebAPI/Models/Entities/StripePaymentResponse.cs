namespace TutorWebAPI.Models.Entities
{
    public class StripePaymentResponse
    {
        public bool Success { get; set; }
        public string SessionId { get; set; }
        public string PaymentIntentId { get; set; }
        public string Status { get; set; }
        public string ErrorMessage { get; set; }
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>(); 
    }
}
