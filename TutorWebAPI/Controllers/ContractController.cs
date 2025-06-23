using Microsoft.AspNetCore.Mvc;
using TutorWebAPI.Services;
using Microsoft.AspNetCore.Authorization;
using TutorWebAPI.Wrapper;
using TutorWebAPI.DTOs;
using TutorWebAPI.Filter;
using System.Security.Claims;

namespace TutorWebAPI.Controllers
{
    [Route("api")]
    [ApiController]
    [Authorize]
    public class ContractController : ControllerBase
    {
        private readonly IContractService _contractService;
        private readonly ILogger<ContractController> _logger;

        public ContractController(IContractService contractService, ILogger<ContractController> logger)
        {
            _contractService = contractService;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves a contract by its ID.
        /// </summary>
        /// <param name="id">The ID of the contract.</param>
        /// <returns>The contract details if found, or a 404 status if not.</returns>
        [HttpGet("Contract/{id}")]
        public async Task<IActionResult> GetContractById(int id)
        {
            _logger.LogInformation("Lấy thông tin hợp đồng với ID: {ContractId}.", id);

            var contract = await _contractService.GetContractByIdAsync(id);
            if (contract == null)
            {
                return NotFound(new { message = "Hợp đồng không tồn tại." });
            }

            return Ok(contract);
        }

        /// <summary>
        /// Get paginated list of all contracts
        /// </summary>
        /// <param name="filter">Pagination parameters (pageNumber, pageSize)</param>
        /// <returns>Paginated list of contracts</returns>
        [HttpGet("Contracts")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllContracts([FromQuery] PaginationFilter filter)
        {
            try
            {
                var route = Request.Path.Value;
                var response = await _contractService.GetAllContractsAsync(filter, route);
                if (!response.Succeeded)
                {
                    _logger.LogWarning("Lỗi khi lấy danh sách hợp đồng: {Message}", response.Message);
                    return StatusCode(500, response);
                }
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy hợp đồng.");
                return StatusCode(500, new Response<string>("Lỗi server."));
            }
        }

        /// <summary>
        /// Get paginated list of contracts by user ID
        /// </summary>
        /// <param name="filter">Pagination parameters (pageNumber, pageSize)</param>
        /// <returns>Paginated list of contracts for the user</returns>
        [HttpGet("Contracts/User")]
        public async Task<IActionResult> GetContractsByUserId([FromQuery] PaginationFilter filter)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            try
            {
                var route = Request.Path.Value;
                var response = await _contractService.GetAllContractsByUserIdAsync(userId, filter, route);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy hợp đồng của người dùng với ID {UserId}.", userId);
                return StatusCode(500, new Response<string>("Lỗi server."));
            }
        }
    }
}
