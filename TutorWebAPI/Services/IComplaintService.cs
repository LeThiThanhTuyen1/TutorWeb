using TutorWebAPI.Wrapper;
using TutorWebAPI.Filter;
using TutorWebAPI.Models;

namespace TutorWebAPI.Services
{
    public interface IComplaintService
    {
        Task<ComplaintDTO> CreateComplaintAsync(Complaint complaint);
        Task<PagedResponse<List<Complaint>>> GetAllComplaintsAsync(PaginationFilter filter, string route);
        Task<ComplaintDTO?> GetComplaintByIdAsync(int id);
        Task<bool> ProcessComplaintAsync(int complaintId, string action);
    }
}