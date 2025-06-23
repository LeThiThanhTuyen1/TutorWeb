namespace TutorWebAPI.DTOs
{
    public class TutorDTO
    {
        public int Id { get; set; }
        public string TutorName { get; set; } = string.Empty;
        public string Subjects { get; set; } = string.Empty;
        public string Introduction { get; set; } = string.Empty;
        public float Rating { get; set; }
        public string School { get; set; }
        public string Gender { get; set; }
        public string Email { get; set; }
        public float Experience { get; set; }
        public string Location { get; set; } = string.Empty;
        public string ProfileImage { get; set; } = string.Empty;
        public FeeRangeDTO FeeRange { get; set; } = new FeeRangeDTO();
        public List<string> TeachingModes { get; set; } = new List<string>();
        public List<CourseDTO> Courses { get; set; } = new List<CourseDTO>(); 
    }

    public class FeeRangeDTO
    {
        public decimal MinFee { get; set; }
        public decimal MaxFee { get; set; }
    }

    public class TutorStatsDTO
    {
        public int Courses { get; set; }
        public int Students { get; set; }
        public int Hours { get; set; }
        public double Rating { get; set; }
        public int CoursesChange { get; set; }
        public int StudentsChange { get; set; }
        public int HoursChange { get; set; }
    }

    public class BarDataDTO
    {
        public string Name { get; set; }
        public int Students { get; set; }
        public int Hours { get; set; }
    }

    public class UpcomingClassDTO
    {
        public int Id { get; set; }
        public string CourseName { get; set; }
        public string Time { get; set; }
        public int Students { get; set; }
        public string Mode { get; set; }
    }

    public class StudentProgressDTO
    {
        public int Id { get; set; }
        public string Student { get; set; }
        public string Course { get; set; }
        public int Progress { get; set; }
        public string LastUpdated { get; set; }
    }

    public class TutorDashboardResponse
    {
        public TutorStatsDTO Stats { get; set; }
        public List<BarDataDTO> BarData { get; set; }
        public List<UpcomingClassDTO> UpcomingClasses { get; set; }
        public List<StudentProgressDTO> StudentProgress { get; set; }
    }
}
