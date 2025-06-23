namespace TutorWebAPI.DTOs
{
    /// <summary>
/// Data Transfer Object for searching tutors.
/// </summary>
public class TutorSearchDTO
{
    /// <summary>
    /// Subjects the tutor should be able to teach.
    /// </summary>
    public string? Subjects { get; set; } = string.Empty;

    /// <summary>
    /// The location where the tutor should be available to teach (e.g., city or region).
    /// </summary>
    public string? Location { get; set; } = string.Empty;

    /// <summary>
    /// The mode of teaching preferred by the student.
    /// </summary>
    public string? TeachingMode { get; set; } = string.Empty;

    /// <summary>
    /// The minimum fee per session or per hour that the student is willing to pay.
    /// </summary>
    public decimal? MinFee { get; set; }

    /// <summary>
    /// The maximum fee per session or per hour that the student is willing to pay.
    /// </summary>
    public decimal? MaxFee { get; set; }

    /// <summary>
    /// The minimum number of years of teaching experience the tutor should have.
    /// </summary>
    public float? MinExperience { get; set; }

    /// <summary>
    /// The minimum rating the tutor should have (on a scale of 1 to 5).
    /// </summary>
    public double? MinRating { get; set; }
}
}
