using System.ComponentModel.DataAnnotations.Schema;

namespace TutorWebAPI.Models
{
    public class Notification
    {
        public int Id { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }
        public string Message { get; set; }
        public string Type { get; set; }

        [Column("sent_at")]
        public DateTime SentAt { get; set; } = DateTime.Now;

        public User? User { get; set; }

        [Column("is_read")]
        public bool IsRead { get; internal set; }
    }
}
