using System.Threading.Tasks;
using TutorWebAPI.DTOs;
using TutorWebAPI.Filter;
using TutorWebAPI.Models;
using TutorWebAPI.Wrapper;

namespace TutorWebAPI.Repositories
{
    public interface IContractRepository
    {
        Task CreateContractAsync(Contract contract);
        Task<ContractDTO?> GetContractByIdAsync(int id);
        //Task<List<Contract>> GetActiveContractsAsync();
        //Task UpdateContractStatusAsync(int contractId, string status, DateTime? completedAt = null);
        Task<PagedResponse<List<ContractDTO>>> GetAllContractsAsync(PaginationFilter filter, string route);
        Task<PagedResponse<List<ContractDTO>>> GetAllContractsByUserIdAsync(int userId, PaginationFilter filter, string route);
    }
}
