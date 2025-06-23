using System.ComponentModel.DataAnnotations.Schema;
using TutorWebAPI.Models;

public class Contract
{
    public int Id { get; set; }

    [Column("tutor_id")]
    public int TutorId { get; set; }

    [Column("student_id")]
    public int StudentId { get; set; }

    [Column("course_id")]
    public int CourseId { get; set; }

    public string? Terms { get; set; }
    public decimal Fee { get; set; }

    [Column("start_date")]
    public DateTime StartDate { get; set; }

    [Column("end_date")]
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = "active";
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [ForeignKey("TutorId")]
    public Tutor? Tutor { get; set; }

    [ForeignKey("StudentId")]
    public Student? Student { get; set; }
    public Course? Course { get; set; }
    public ICollection<Complaint>? Complaints { get; set; }
}
