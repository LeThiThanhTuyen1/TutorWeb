using System.ComponentModel.DataAnnotations.Schema;
using TutorWebAPI.Models.Entities;

namespace TutorWebAPI.Models
{
    public class Enrollment
    {
        public long Id { get; set; }

        [Column("student_id")]
        public int StudentId { get; set; }

        [Column("course_id")]
        public int CourseId { get; set; }
        public string Status { get; set; } = "pending";

        [Column("enrolled_at")]
        public DateTime EnrolledAt { get; set; } = DateTime.Now;

        public Student? Student { get; set; }
        public Course? Course { get; set; }
        public ICollection<Payment> Payments { get; set; }
    }
}