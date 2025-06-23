namespace TutorWebAPI.Models.DTOs
{
    public class BillHistoryDTO
    {
        public int PaymentId { get; set; }
        public long EnrollmentId { get; set; }
        public int CourseId { get; set; }
        public string CourseName { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; }
        public string TransactionId { get; set; }
        public string OrderId { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public string VnPayResponseCode { get; set; }
    }
}