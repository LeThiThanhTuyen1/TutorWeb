using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TutorWebAPI.Models
{
    public class Schedule
    {
        [Key]
        public int Id { get; set; }

        [Column("tutor_id")]
        public int TutorId { get; set; }

        [Column("course_id")]
        public int CourseId { get; set; }

        [Column("day_of_week")]
        public int DayOfWeek { get; set; }

        [Column("start_hour")]
        public TimeSpan StartHour { get; set; }

        [Column("end_hour")]
        public TimeSpan EndHour { get; set; }
        public string Location { get; set; }
        public string Mode { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string Status { get; set; }
        public Tutor? Tutor { get; set; }
        public Course? Course { get; set; }
    }
}