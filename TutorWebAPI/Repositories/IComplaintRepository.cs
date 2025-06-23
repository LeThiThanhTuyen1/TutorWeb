using System.Collections.Generic;
using System.Threading.Tasks;
using TutorWebAPI.Filter;
using TutorWebAPI.Models;
using TutorWebAPI.Wrapper;

namespace TutorWebAPI.Repositories
{
    public interface IComplaintRepository
    {
        Task<PagedResponse<List<Complaint>>> GetAllComplaintsAsync(PaginationFilter filter, string route);
        Task<ComplaintDTO?> GetComplaintByIdAsync(int id); 
        Task<int> CountComplaintsAsync();
        Task UpdateComplaintAsync(Complaint complaint);
        Task<ComplaintDTO> CreateComplaintAsync(Complaint complaint);
    }
}
