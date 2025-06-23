namespace TutorWebAPI.Models.DTOs
{
    public class VnPaymentResponseModel
    {
            public bool Success { get; set; }
            public string PaymentMethod { get; set; }
            public string OrderDescription { get; set; }
            public long OrderId { get; set; }
            public string PaymentId { get; set; }
            public string TransactionId { get; set; }
            public string Token { get; set; }
            public string VnPayResponseCode { get; set; }
        }

        public class VnPaymentRequestModel
        {
            public long OrderId { get; set; } //Id enrollment
            public string FullName { get; set; }
            public string Description { get; set; }
            public double Amount { get; set; }
            public DateTime CreatedDate { get; set; }
        }
}
