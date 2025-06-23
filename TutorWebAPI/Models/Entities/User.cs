using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using TutorWebAPI.Models;

public class User
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; }

    [Required, EmailAddress(ErrorMessage = "Invalid Format Email"), MaxLength(100)]
    public string Email { get; set; }

    [MaxLength(10, ErrorMessage = "Phone number must be 10 characters long.")]
    [Phone(ErrorMessage = "Invalid Format Phone Number")]
    public string Phone { get; set; }

    public string Password { get; set; }
    public string Role { get; set; }

    [Column("profile_image")]
    public string ProfileImage { get; set; }
    public string Gender { get; set; }

    [Column("date_of_birth")]
    public DateTime? DateOfBirth { get; set; }
    public string School { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;

    //[Column("refresh_token")]
    //public string RefreshToken { get; set; } = string.Empty;

    //[Column("refresh_token_expiry")]
    //public DateTime? RefreshTokenExpiry { get; set; }
    public bool Verified { get; set; } = false;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    [Column("is_deleted")]
    public bool isDeleted { get; set; } = false;
    // public virtual Student Student { get; set; }
    // public virtual Tutor Tutor { get; set; }
    public ICollection<Complaint>? Complaints { get; set; }
    public ICollection<Notification>? Notifications { get; set; }
}
