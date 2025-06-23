using System.ComponentModel.DataAnnotations.Schema;
using TutorWebAPI.Models.Entities;
using TutorWebAPI.Models;

namespace TutorWebAPI.DTOs
{
    public class EnrollmentRequest
    {
        public int CourseId { get; set; }
    }

    public class EnrollmentDTO
    {
        public long Id { get; set; }
        public int CourseId { get; set; }
        public int UserId { get; set; }
        public string TutorName { get; set; }
        public string CourseName { get; set; }
        public DateTime StartDate { get; set; }
        public string Subject { get; set; }
        public DateTime EndDate { get; set; }
        public decimal Fee { get; set; }
        public string Status { get; set; } = "pending";
        public DateTime EnrolledAt { get; set; } = DateTime.Now;
        public List<ScheduleDTO> Schedule { get; set; } = new List<ScheduleDTO>();
    }
}
