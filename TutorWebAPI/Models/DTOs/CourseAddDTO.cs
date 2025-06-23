public class CourseDto
{
    public int Id { get; set; }
    public int TutorId { get; set; }
    public string CourseName { get; set; }
    public string Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal Fee { get; set; }
    public string? Subject { get; set; }
    public int MaxStudents { get; set; }
    public string Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? UserId { get; set; }
    public string? TutorName { get; set; }
}

public class TutorDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public float Experience { get; set; }
    public string Subjects { get; set; }
    public string Introduction { get; set; }
    public float Rating { get; set; }

    // Instead of Courses collection, just include count if needed
    public int CourseCount { get; set; }
}