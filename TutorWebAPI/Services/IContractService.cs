using TutorWebAPI.Wrapper;
using TutorWebAPI.Filter;
using TutorWebAPI.DTOs;

namespace TutorWebAPI.Services
{
    public interface IContractService
    {
        Task<ContractDTO?> GetContractByIdAsync(int id);
        Task<PagedResponse<List<ContractDTO>>> GetAllContractsAsync(PaginationFilter filter, string route);
        Task<PagedResponse<List<ContractDTO>>> GetAllContractsByUserIdAsync(int userId, PaginationFilter filter, string route);
    }
}