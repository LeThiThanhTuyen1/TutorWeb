using Microsoft.EntityFrameworkCore;
using TutorWebAPI.Data;
using TutorWebAPI.DTOs;
using TutorWebAPI.Filter;
using TutorWebAPI.Helper;
using TutorWebAPI.Wrapper;

namespace TutorWebAPI.Repositories
{
    public class ContractRepository : IContractRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ContractRepository> _logger;
        private readonly IUriRepository _uriRepository;

        public ContractRepository(ApplicationDbContext context,IUriRepository uriRepository, ILogger<ContractRepository> logger)
        {
            _uriRepository = uriRepository;
            _context = context;
            _logger = logger;
        }

        public async Task CreateContractAsync(Contract contract)
        {
            try
            {
                await _context.Contracts.AddAsync(contract);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "An error occurred while adding a new contract.");
                throw new Exception("An error occurred while saving the contract. Please try again later.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred.");
                throw new Exception("An unexpected error occurred. Please try again later.");
            }
        }

        public async Task<ContractDTO?> GetContractByIdAsync(int id)
        {
            try
            {
                return await _context.Contracts
                    .Include(c => c.Tutor)
                        .ThenInclude(t => t.User)
                    .Include(c => c.Student)
                        .ThenInclude(s => s.User)
                    .Include(c => c.Course)
                    .Where(c => c.Id == id)
                    .Select(c => new ContractDTO
                    {
                        Id = c.Id,
                        TutorName = c.Tutor != null && c.Tutor.User != null ? c.Tutor.User.Name : "N/A",
                        StudentName = c.Student != null && c.Student.User != null ? c.Student.User.Name : "N/A",
                        CourseName = c.Course != null ? c.Course.CourseName : "N/A",
                        Terms = c.Terms,
                        Fee = c.Fee,
                        StartDate = c.StartDate,
                        EndDate = c.EndDate,
                        Status = c.Status
                    })
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving contract with id {ContractId}", id);
                throw new Exception("An error occurred while retrieving the contract. Please try again later.");
            }
        }

        public async Task<PagedResponse<List<ContractDTO>>> GetAllContractsAsync(PaginationFilter filter, string route)
        {
            try
            {
                var contractsQuery = _context.Contracts
                    .Include(c => c.Tutor)
                        .ThenInclude(t => t.User)
                    .Include(c => c.Student)
                        .ThenInclude(s => s.User)
                    .Include(c => c.Course)
                    .Select(c => new ContractDTO
                    {
                        Id = c.Id,
                        TutorName = c.Tutor != null && c.Tutor.User != null ? c.Tutor.User.Name : "N/A",
                        StudentName = c.Student != null && c.Student.User != null ? c.Student.User.Name : "N/A",
                        CourseName = c.Course != null ? c.Course.CourseName : "N/A",
                        Terms = c.Terms,
                        Fee = c.Fee,
                        StartDate = c.StartDate,
                        EndDate = c.EndDate,
                        Status = c.Status
                    });

                return await contractsQuery.ToPagedResponseAsync(filter, _uriRepository, route);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving all contracts.");
                throw new Exception("An error occurred while retrieving contracts. Please try again later.", ex);
            }
        }

        public async Task<PagedResponse<List<ContractDTO>>> GetAllContractsByUserIdAsync(int userId, PaginationFilter filter, string route)
        {
            try
            {
                var contractsQuery = _context.Contracts
                    .Include(c => c.Tutor)
                        .ThenInclude(t => t.User)
                    .Include(c => c.Student)
                        .ThenInclude(s => s.User)
                    .Include(c => c.Course)
                    .Where(c => (c.Tutor != null && c.Tutor.UserId == userId) ||
                                (c.Student != null && c.Student.UserId == userId))
                    .Select(c => new ContractDTO
                    {
                        Id = c.Id,
                        TutorName = c.Tutor != null && c.Tutor.User != null ? c.Tutor.User.Name : "N/A",
                        StudentName = c.Student != null && c.Student.User != null ? c.Student.User.Name : "N/A",
                        CourseName = c.Course != null ? c.Course.CourseName : "N/A",
                        Terms = c.Terms,
                        Fee = c.Fee,
                        StartDate = c.StartDate,
                        EndDate = c.EndDate,
                        Status = c.Status
                    });

                return await contractsQuery.ToPagedResponseAsync(filter, _uriRepository, route);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving contracts for user ID {UserId}", userId);
                throw new Exception("An error occurred while retrieving contracts for the user. Please try again later.", ex);
            }
        }
    }
}
