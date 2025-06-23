using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TutorWebAPI.Models
{
    public class Student
    {
        [Key]
        public int Id { get; set; }

        [Column("user_id")]
        [ForeignKey("User")]
        public int UserId { get; set; }
        public int? Class { get; set; }
        public User? User { get; set; }
        public ICollection<Enrollment>? Enrollments { get; set; }
        public ICollection<Feedback>? Feedbacks { get; set; }
        public ICollection<Contract>? Contracts { get; set; }
    }
}