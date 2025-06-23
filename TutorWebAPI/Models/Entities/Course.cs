using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TutorWebAPI.Models
{
    public class Course
    {
        public int Id { get; set; }

        [Column("tutor_id")]
        public int TutorId { get; set; }

        [Column("course_name")]
        [Required]
        public string CourseName { get; set; }

        [Required]
        public string Description { get; set; }

        [Column("start_date")]
        [Required]
        public DateTime StartDate { get; set; }

        [Column("end_date")]

        [Required]
        public DateTime EndDate { get; set; }
        public string? Subject { get; set; }

        [Required]
        public decimal Fee { get; set; }

        [Column("max_students")]
        [Required]
        public int MaxStudents { get; set; }
        public string Status { get; set; } = "coming";

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("is_deleted")]
        public bool isDeleted { get; set; } = false;
        public Tutor? Tutor { get; set; }
        public ICollection<Enrollment>? Enrollments { get; set; }
        public ICollection<Contract>? Contracts { get; set; }
        public ICollection<Schedule>? Schedules { get; set; }
    }
}