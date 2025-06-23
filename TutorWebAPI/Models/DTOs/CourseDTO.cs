namespace TutorWebAPI.DTOs
{
    public class CourseDTO
    {
        public int Id { get; set; }
        public string CourseName { get; set; }
        public int TutorId { get; set; }
        public int EnrollmentId { get; set; }
        public string Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal Fee { get; set; }
        public string? Subject { get; set; }
        public int MaxStudents { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? TutorName { get; set; }
        public List<ScheduleDTO> Schedule { get; set; } = new List<ScheduleDTO>();
    }
}
