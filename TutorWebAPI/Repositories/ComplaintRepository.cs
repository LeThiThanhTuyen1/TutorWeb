using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using TutorWebAPI.Data;
using TutorWebAPI.Filter;
using TutorWebAPI.Helper;
using TutorWebAPI.DTOs;
using TutorWebAPI.Models;
using TutorWebAPI.Wrapper;
using Microsoft.Extensions.Logging;

namespace TutorWebAPI.Repositories
{
    public class ComplaintRepository : IComplaintRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IUriRepository _uriRepository;
        private readonly ILogger<ComplaintRepository> _logger;

        public ComplaintRepository(ApplicationDbContext context, IUriRepository uriRepository, ILogger<ComplaintRepository> logger)
        {
            _context = context;
            _uriRepository = uriRepository;
            _logger = logger;
        }

        public async Task<PagedResponse<List<Complaint>>> GetAllComplaintsAsync(PaginationFilter filter, string route)
        {
            try
            {
                return await _context.Complaints
                    .OrderByDescending(c => c.CreatedAt)
                    .ToPagedResponseAsync(filter, _uriRepository, route);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving all complaints.");
                throw new Exception("An error occurred while retrieving complaints.");
            }
        }

        public async Task<ComplaintDTO?> GetComplaintByIdAsync(int id)
        {
            try
            {
                var complaint = await _context.Complaints
                    .Include(c => c.Contract)
                        .ThenInclude(contract => contract.Tutor)
                            .ThenInclude(tutor => tutor.User)
                    .Include(c => c.Contract)
                        .ThenInclude(contract => contract.Student)
                            .ThenInclude(student => student.User)
                    .Include(c => c.Contract)
                        .ThenInclude(contract => contract.Course)
                    .Where(c => c.Id == id)
                    .Select(c => new ComplaintDTO
                    {
                        Id = c.Id,
                        ContractId = c.ContractId,
                        UserId = c.UserId,
                        Description = c.Description,
                        CreatedAt = c.CreatedAt,
                        Status = c.Status,
                        Contract = new ContractDTO
                        {
                            Id = c.Contract.Id,
                            TutorName = c.Contract.Tutor.User.Name,
                            StudentName = c.Contract.Student.User.Name,
                            CourseName = c.Contract.Course.CourseName,
                            Terms = c.Contract.Terms,
                            Fee = c.Contract.Fee,
                            StartDate = c.Contract.StartDate,
                            EndDate = c.Contract.EndDate,
                            Status = c.Contract.Status
                        }
                    })
                    .FirstOrDefaultAsync();

                return complaint;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching complaint with ID {ComplaintId}", id);
                return null;
            }
        }

        public async Task<int> CountComplaintsAsync()
        {
            try
            {
                return await _context.Complaints.CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while counting complaints.");
                throw new Exception("An error occurred while counting complaints.");
            }
        }

        public async Task<ComplaintDTO> CreateComplaintAsync(Complaint complaint)
        {
            await _context.Complaints.AddAsync(complaint);

            var contract = await _context.Contracts
                .FirstOrDefaultAsync(c => c.Id == complaint.ContractId);
            if (contract == null)
            {
                _logger.LogWarning("Contract with ID {ContractId} not found for complaint.", complaint.ContractId);
                throw new Exception($"Contract with ID {complaint.ContractId} not found.");
            }

            contract.Status = "pending";
            _context.Contracts.Update(contract);

            await _context.SaveChangesAsync();
            _logger.LogInformation("Created a new complaint with ID {ComplaintId}.", complaint.Id);

            return new ComplaintDTO
            {
                Id = complaint.Id,
                Description = complaint.Description,
                ContractId = complaint.ContractId,
                Status = complaint.Status,
                UserId = complaint.UserId
            };
        }

        public async Task UpdateComplaintAsync(Complaint complaint)
        {
            try
            {
                var existingComplaint = await _context.Complaints.FindAsync(complaint.Id);
                if (existingComplaint == null)
                {
                    _logger.LogWarning("Complaint with ID {ComplaintId} not found.", complaint.Id);
                    throw new Exception("Complaint not found.");
                }

                _context.Entry(existingComplaint).CurrentValues.SetValues(complaint);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Updated complaint with ID {ComplaintId}.", complaint.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating complaint {ComplaintId}.", complaint.Id);
                throw new Exception("An error occurred while updating the complaint.");
            }
        }
    }
}
