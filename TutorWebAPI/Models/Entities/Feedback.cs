using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TutorWebAPI.Models
{
    public class Feedback
    {
        public int Id { get; set; }

        [Column("student_id")]
        public int StudentId { get; set; }

        [Column("tutor_id")]
        public int TutorId { get; set; }
        public int Rating { get; set; }

        [Required]
        public string? Comment { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public Student? Student { get; set; }
        public Tutor? Tutor { get; set; }
    }
}
