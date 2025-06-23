using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace TutorWebAPI.Models
{
    public class Complaint
    {
        public int Id { get; set; }

        [Column("contract_id")]
        public int ContractId { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }
        public string Description { get; set; }
        public string Status { get; set; } = "pending";

        [Column("create_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public Contract? Contract { get; set; }

        public User? User { get; set; }
    }
}