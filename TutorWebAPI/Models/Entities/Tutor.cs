using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TutorWebAPI.Models
{
    public class Tutor
    {
        [Key]
        public int Id { get; set; }

        [Column("user_id")]
        [ForeignKey("User")]
        public int UserId { get; set; }
        public float? Experience { get; set; } = 0;
        public string Subjects { get; set; } = string.Empty;
        public string Introduction { get; set; } = string.Empty;

        [Column(TypeName = "real")]
        public float Rating { get; set; } = 0;
        public User? User { get; set; }
        public ICollection<Course>? Courses { get; set; }
        public ICollection<Feedback>? Feedbacks { get; set; }
        public ICollection<Contract>? Contracts { get; set; }
        public ICollection<Schedule>? Schedules { get; set; }
    }
}