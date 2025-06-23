namespace TutorWebAPI.DTOs
{
    public class ContractDTO
    {
        public int Id { get; set; }
        public string TutorName { get; set; }
        public string StudentName { get; set; }
        public string CourseName { get; set; }
        public string Terms { get; set; }
        public decimal Fee { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; }
    }
}
