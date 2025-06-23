using System;
using System.ComponentModel.DataAnnotations;

namespace TutorWebAPI.DTOs
{
    public class ScheduleDTO
    {
        public int Id { get; set; }

        [Required]
        public int TutorId { get; set; }

        [Required]
        public int CourseId { get; set; }

        [Required]
        [Range(1, 7, ErrorMessage = "Day of week must be from Monday(1) to Sunday(7).")]
        public int DayOfWeek { get; set; }

        [Required]
        public TimeSpan StartHour { get; set; }

        [Required]
        public TimeSpan EndHour { get; set; }

        [Required]
        [MaxLength(10, ErrorMessage = "Mode must be 'online' or 'offline'.")]
        public string Mode { get; set; }

        public string Location { get; set; } = string.Empty;
        public string Status { get; set; } = "scheduled";
    }
}
