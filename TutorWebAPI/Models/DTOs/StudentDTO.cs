namespace TutorWebAPI.DTOs
{
    public class StudentDTO
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string School { get; set; }
        public string Status { get; set; }
        public DateTime EnrolledAt { get; set; }
        public string Role { get; set; }
        public string Location { get; set; }
        public string ProfileImage { get; set; }
    }

    public class StudentStatsDTO
    {
        public int Courses { get; set; }
        public int CoursesLastMonth { get; set; }
        public int CompletedCourses { get; set; }
        public int CompletedCoursesLastMonth { get; set; }
        public int HoursLearned { get; set; }
        public int HoursLearnedLastMonth { get; set; }
    }

    public class SubjectPieDTO
    {
        public string Name { get; set; }
        public int Value { get; set; }
        public string Color { get; set; }
    }

    public class StudentCourseDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Tutor { get; set; }
        public int Progress { get; set; }
        public string NextLesson { get; set; }
    }
}
