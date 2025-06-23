using TutorWebAPI.DTOs;

public class ComplaintDTO
{
    public int Id { get; set; }
    public int ContractId { get; set; }
    public int UserId { get; set; }
    public string Description { get; set; }
    public DateTime? CreatedAt { get; set; }
    public string Status { get; set; }
    public ContractDTO Contract { get; set; } 
}
public class ComplaintActionRequest
{
    public string Action { get; set; }
}