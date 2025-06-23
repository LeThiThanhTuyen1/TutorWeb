using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TutorWebAPI.Data;
using TutorWebAPI.Filter;
using TutorWebAPI.Models;
using TutorWebAPI.Repositories;
using TutorWebAPI.Wrapper;

namespace TutorWebAPI.Services
{
    public class ComplaintService : IComplaintService
    {
        private readonly IComplaintRepository _complaintRepo;
        private readonly INotificationRepository _notificationRepo;
        private readonly IMemoryCache _cache;
        private readonly ILogger<ComplaintService> _logger;
        private readonly ApplicationDbContext _context;
        private static readonly string ComplaintListCacheKeys = "ComplaintListCacheKeys";
        private static readonly object _cacheLock = new object();

        public ComplaintService(
            IComplaintRepository complaintRepo,
            INotificationRepository notificationRepo,
            IMemoryCache cache,
            ILogger<ComplaintService> logger,
            ApplicationDbContext context)
        {
            _complaintRepo = complaintRepo;
            _notificationRepo = notificationRepo;
            _cache = cache;
            _logger = logger;
            _context = context;
        }

        public interface IComplaintService
        {
            Task<ComplaintDTO> CreateComplaintAsync(Complaint complaint);
        }


        public async Task<ComplaintDTO> CreateComplaintAsync(Complaint complaint)
        {
            try
            {
                var complaintDto = await _complaintRepo.CreateComplaintAsync(complaint);
                InvalidateComplaintListCache();
                await SendNotificationAsync(complaint.UserId, "Your complaint has been submitted and is under review.");
                return complaintDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating complaint for contract ID {ContractId}", complaint.ContractId);
                throw;
            }
        }

        public async Task<PagedResponse<List<Complaint>>> GetAllComplaintsAsync(PaginationFilter filter, string route)
        {
            string cacheKey = $"Complaints_Page_{filter.PageNumber}_Size_{filter.PageSize}";

            if (_cache.TryGetValue(cacheKey, out PagedResponse<List<Complaint>> cachedComplaints))
            {
                _logger.LogInformation("Returning cached complaints for key {CacheKey}", cacheKey);
                return cachedComplaints;
            }

            lock (_cacheLock)
            {
                if (_cache.TryGetValue(cacheKey, out cachedComplaints))
                {
                    return cachedComplaints;
                }

                var resultTask = _complaintRepo.GetAllComplaintsAsync(filter, route);
                var result = resultTask.GetAwaiter().GetResult();
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
                    SlidingExpiration = TimeSpan.FromMinutes(5),

                    PostEvictionCallbacks = { new PostEvictionCallbackRegistration
                    {
                        EvictionCallback = (key, value, reason, state) =>
                        {
                            _logger.LogInformation("Cache key {Key} evicted due to {Reason}", key, reason);
                        }
                    }}
                };

                var keys = _cache.GetOrCreate(ComplaintListCacheKeys, entry =>
                {
                    entry.SetAbsoluteExpiration(TimeSpan.FromHours(1));
                    return new HashSet<string>();
                });
                keys.Add(cacheKey);
                _cache.Set(ComplaintListCacheKeys, keys, TimeSpan.FromHours(1));

                _cache.Set(cacheKey, result, cacheOptions);
                _logger.LogInformation("Cached complaints for key {CacheKey}", cacheKey);

                return result;
            }
        }

        public async Task<ComplaintDTO?> GetComplaintByIdAsync(int id)
        {
            string cacheKey = $"Complaint_{id}";

            if (_cache.TryGetValue(cacheKey, out ComplaintDTO cachedComplaint))
            {
                _logger.LogInformation("Returning cached complaint for ID {ComplaintId}", id);
                return cachedComplaint;
            }

            var complaint = await _complaintRepo.GetComplaintByIdAsync(id);
            if (complaint != null)
            {
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
                    PostEvictionCallbacks = { new PostEvictionCallbackRegistration
                    {
                        EvictionCallback = (key, value, reason, state) =>
                        {
                            _logger.LogInformation("Cache key {Key} evicted due to {Reason}", key, reason);
                        }
                    }}
                };
                _cache.Set(cacheKey, complaint, cacheOptions);
                _logger.LogInformation("Cached complaint for ID {ComplaintId}", id);
            }

            return complaint;
        }

        public async Task<bool> ProcessComplaintAsync(int complaintId, string action)
        {
            var complaintDto = await GetComplaintByIdAsync(complaintId);
            if (complaintDto == null || complaintDto.Contract == null)
            {
                _logger.LogWarning("Complaint ID {ComplaintId} not found or has no valid contract", complaintId);
                return false;
            }

            var complaint = await _context.Complaints
                .Include(c => c.Contract)
                    .ThenInclude(contract => contract.Course)
                .FirstOrDefaultAsync(c => c.Id == complaintId);

            if (complaint == null)
            {
                _logger.LogWarning("Complaint entity not found for ID {ComplaintId}", complaintId);
                return false;
            }

            try
            {
                if (action == "approve")
                {
                    complaint.Status = "approved";
                    complaint.Contract.Status = "canceled";
                    complaint.Contract.Course.Status = "canceled";

                    string notificationMessage = $"Khóa học '{complaint.Contract.Course.CourseName}' đã bị hủy vì khiếu nại: {complaint.Description}";
                    await SendNotificationAsync(complaint.UserId, notificationMessage);
                }
                else if (action == "reject")
                {
                    complaint.Status = "canceled";
                    await SendNotificationAsync(complaint.UserId, "Khiếu nại của bạn đã bị từ chối.");
                }
                else
                {
                    return false;
                }

                await _complaintRepo.UpdateComplaintAsync(complaint);

                _cache.Remove($"Complaint_{complaintId}");
                InvalidateComplaintListCache();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý khiếu nại với ID {ComplaintId} và hành động {Action}", complaintId, action);
                return false;
            }
        }

        private async Task SendNotificationAsync(int userId, string message)
        {
            try
            {
                await _notificationRepo.CreateNotification(userId, message, "contract_update");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi gửi thông báo đến người dùng vói ID {UserId}", userId);
            }
        }

        private void InvalidateComplaintListCache()
        {
            lock (_cacheLock)
            {
                if (_cache.TryGetValue(ComplaintListCacheKeys, out HashSet<string> keys))
                {
                    foreach (var key in keys)
                    {
                        _cache.Remove(key);
                        _logger.LogInformation("Removed cache key {CacheKey}", key);
                    }
                    _cache.Remove(ComplaintListCacheKeys);
                }
            }
        }
    }
}