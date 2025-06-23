using Microsoft.EntityFrameworkCore;
using TutorWebAPI.Data;
using TutorWebAPI.Filter;
using TutorWebAPI.Helper;
using TutorWebAPI.Models.DTOs;
using TutorWebAPI.Wrapper;

namespace TutorWebAPI.Repositories
{
    public class VNPayRepository : IVNPayRepository
    {
        private readonly IConfiguration _config;
        private readonly ApplicationDbContext _context;
        private readonly IUriRepository _uriRepo;
        private readonly ILogger<ScheduleRepository> _logger;

        public VNPayRepository(IConfiguration config, IUriRepository uriRepo, ApplicationDbContext context, ILogger<ScheduleRepository> logger)
        {
            _uriRepo = uriRepo;
            _config = config;
            _context = context;
            _logger = logger;
        }

        public string CreatePaymentUrl(HttpContext context, VnPaymentRequestModel response)
        {
            var tick = DateTime.Now.Ticks.ToString();
            var vnpay = new VNPayLibrary();

            vnpay.AddRequestData("vnp_Version", _config["VNPay:Version"]);
            vnpay.AddRequestData("vnp_Command", _config["VNPay:Command"]);
            vnpay.AddRequestData("vnp_TmnCode", _config["VNPay:TmaCode"]);
            vnpay.AddRequestData("vnp_Amount", (response.Amount * 100).ToString());
            vnpay.AddRequestData("vnp_CreateDate", response.CreatedDate.ToString("yyyyMMddHHmmss"));
            vnpay.AddRequestData("vnp_CurrCode", _config["VNPay:CurrCode"]);
            vnpay.AddRequestData("vnp_IpAddr", Utils.GetIpAddress(context));
            vnpay.AddRequestData("vnp_Locale", _config["VNPay:Locale"]);
            vnpay.AddRequestData("vnp_OrderInfo", "Payment:" + response.OrderId);
            vnpay.AddRequestData("vnp_OrderType", "other");
            vnpay.AddRequestData("vnp_ReturnUrl", _config["VNPay:PaymentCallbackUrl"]);
            vnpay.AddRequestData("vnp_TxnRef", response.OrderId.ToString());

            var paymentUrl = vnpay.CreateRequestUrl(_config["VNPay:BaseUrl"], _config["VNPay:HashSecret"]);
            return paymentUrl;
        }

        public async Task<PagedResponse<List<BillHistoryDTO>>> GetBillHistory(PaginationFilter filter, string route, int userId)
        {
            try
            {
                var query = _context.Payments
                    .Include(p => p.Enrollment)
                        .ThenInclude(e => e.Course)
                    .Where(p => p.Enrollment.Student.UserId == userId);

                var totalRecords = await query.CountAsync();
                if (totalRecords == 0)
                {
                    return new PagedResponse<List<BillHistoryDTO>>(null, filter.PageNumber, filter.PageSize)
                    {
                        Succeeded = false,
                        Message = $"No payment history found for user ID {userId}."
                    };
                }

                 var payments = await query
                    .OrderByDescending(p => p.CreatedAt)
                    .Skip((filter.PageNumber - 1) * filter.PageSize)
                    .Take(filter.PageSize)
                    .Select(p => new BillHistoryDTO
                    {
                        PaymentId = p.Id,
                        EnrollmentId = p.EnrollmentId,
                        CourseId = p.Enrollment.CourseId,
                        CourseName = p.Enrollment.Course.CourseName,
                        Amount = p.Amount,
                        PaymentMethod = p.PaymentMethod,
                        TransactionId = p.TransactionId,
                        OrderId = p.OrderId,
                        Status = p.Status,
                        CreatedAt = p.CreatedAt,
                        VnPayResponseCode = p.VnPayResponseCode
                    })
                    .ToPagedResponseAsync(filter, _uriRepo, route);

                return payments;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching bill history for user ID {userId}: {ex.Message}");
                throw;
            }
        }

        public VnPaymentResponseModel PaymentExecute(IQueryCollection collection)
        {
            var vnpay = new VNPayLibrary();
            foreach (var (key, value) in collection)
            {
                if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
                {
                    vnpay.AddResponseData(key, value.ToString());
                }
            }

            var vnp_orderId = Convert.ToInt64(vnpay.GetResponseData("vnp_TxnRef"));
            var vnp_TransactionId = Convert.ToInt64(vnpay.GetResponseData("vnp_TransactionNo"));
            var vnp_SecureHash = collection.FirstOrDefault(p => p.Key == "vnp_SecureHash").Value;
            var vnp_ResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
            var vnp_OrderInfo = vnpay.GetResponseData("vnp_OrderInfo");

            bool checkSignature = vnpay.ValidateSignature(vnp_SecureHash, _config["VnPay:HashSecret"]);
            if (!checkSignature)
            {
                return new VnPaymentResponseModel
                {
                    Success = false
                };
            }

            return new VnPaymentResponseModel
            {
                Success = true,
                PaymentMethod = "VnPay",
                OrderDescription = vnp_OrderInfo,
                OrderId = vnp_orderId,
                TransactionId = vnp_TransactionId.ToString(),
                Token = vnp_SecureHash,
                VnPayResponseCode = vnp_ResponseCode
            };
        }
    }
}
