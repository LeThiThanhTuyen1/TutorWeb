using Microsoft.Extensions.Caching.Memory;
using TutorWebAPI.DTOs;
using TutorWebAPI.Filter;
using TutorWebAPI.Repositories;
using TutorWebAPI.Wrapper;

namespace TutorWebAPI.Services
{
    public class ContractService : IContractService
    {
        private readonly IContractRepository _contractRepository;
        private readonly ILogger<ContractService> _logger;
        private readonly IMemoryCache _cache;

        public ContractService(IContractRepository contractRepository, ILogger<ContractService> logger, IMemoryCache cache)
        {
            _contractRepository = contractRepository;
            _logger = logger;
            _cache = cache;
        }

        public async Task<ContractDTO?> GetContractByIdAsync(int id)
        {
            string cacheKey = $"contract_{id}";

            if (_cache.TryGetValue(cacheKey, out ContractDTO? cachedContract))
            {
                _logger.LogInformation("Fetched contract {ContractId} from cache.", id);
                return cachedContract;
            }

            _logger.LogInformation("Fetching contract {ContractId} from database.", id);
            var contract = await _contractRepository.GetContractByIdAsync(id);

            if (contract == null)
            {
                _logger.LogWarning("Contract {ContractId} not found.", id);
                return null;
            }

            _cache.Set(cacheKey, contract, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
                SlidingExpiration = TimeSpan.FromMinutes(10)
            });

            return contract;
        }

        public async Task<PagedResponse<List<ContractDTO>>> GetAllContractsAsync(PaginationFilter filter, string route)
        {
            var contracts = await _contractRepository.GetAllContractsAsync(filter, route);
            return contracts;
        }

        public async Task<PagedResponse<List<ContractDTO>>> GetAllContractsByUserIdAsync(int userId, PaginationFilter filter, string route)
        {
            var contracts = await _contractRepository.GetAllContractsByUserIdAsync(userId, filter, route);
            return contracts;
        }
    }
}
