using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using TutorWebAPI.Data;
using TutorWebAPI.Filter;
using TutorWebAPI.Models;
using TutorWebAPI.Models.DTOs;
using TutorWebAPI.Repositories;
using TutorWebAPI.Wrapper;
using Microsoft.AspNetCore.Mvc;
using TutorWebAPI.Models.Entities;
using TutorWebAPI.DTOs;

namespace TutorWebAPI.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IVNPayRepository _vnPayRepository;
        private readonly ILogger<PaymentService> _logger;
        private readonly IEnrollmentService _enrollmentService;
        private readonly IStripeRepository _stripeRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private const string COURSES_REDIRECT_BASE_URL = "http://localhost:3000/student/courses";
        private const string SUCCESS_PARAM = "success";
        private const string ENROLLMENT_ID_PARAM = "enrollmentId";
        private const string SESSION_ID_PARAM = "sessionId";
        private const string ERROR_CODE_PARAM = "code";
        private const string ERROR_MESSAGE_PARAM = "message";

        public PaymentService(
            ApplicationDbContext context,
            ILogger<PaymentService> logger,
            IVNPayRepository vnPayRepository,
            IEnrollmentService enrollmentService,
            IStripeRepository stripeRepository,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _vnPayRepository = vnPayRepository ?? throw new ArgumentNullException(nameof(vnPayRepository));
            _enrollmentService = enrollmentService ?? throw new ArgumentNullException(nameof(enrollmentService));
            _stripeRepository = stripeRepository ?? throw new ArgumentNullException(nameof(stripeRepository));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        public async Task<string> CreateCheckoutSessionAsync(EnrollmentDTO enrollment, string successUrl, string cancelUrl)
        {
            try
            {
                var paymentUrl = await _stripeRepository.CreateCheckoutSessionAsync(enrollment, successUrl, cancelUrl);

                var payment = new Payment
                {
                    EnrollmentId = enrollment.Id,
                    Amount = enrollment.Fee,
                    PaymentMethod = "Stripe",
                    TransactionId = paymentUrl.Split('/').Last(), 
                    Status = "pending",
                    CreatedAt = DateTime.UtcNow,
                    OrderId = "",
                    VnPayResponseCode = ""
                };
                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                return paymentUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Stripe checkout session for enrollment {EnrollmentId}", enrollment.Id);
                throw;
            }
        }

        public async Task<string> CreateVNPayPaymentUrlAsync(EnrollmentDTO enrollment, string successUrl, string cancelUrl)
        {
            try
            {
                var orderId = enrollment.Id;
                var vnPayRequest = new VnPaymentRequestModel
                {
                    Amount = (double)enrollment.Fee,
                    CreatedDate = DateTime.Now,
                    OrderId = orderId,
                };

                var paymentUrl = _vnPayRepository.CreatePaymentUrl(_httpContextAccessor.HttpContext, vnPayRequest);

                var payment = new Payment
                {
                    EnrollmentId = enrollment.Id,
                    Amount = enrollment.Fee,
                    PaymentMethod = "VNPay",
                    TransactionId = orderId.ToString(),
                    Status = "pending",
                    CreatedAt = DateTime.UtcNow,
                    OrderId = orderId.ToString(),
                    VnPayResponseCode = ""
                };
                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                return paymentUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating VNPay payment URL for enrollment {EnrollmentId}", enrollment.Id);
                throw;
            }
        }

        public async Task<Response<string>> ProcessWebhookEventAsync(string json, string signature)
        {
            try
            {
                var response = await _stripeRepository.ProcessWebhookEventAsync(json, signature);
                if (!response.Success)
                {
                    _logger.LogWarning("Webhook processing failed: {ErrorMessage}", response.ErrorMessage);
                    return new Response<string> { Succeeded = false, Message = response.ErrorMessage };
                }

                var enrollmentId = int.Parse(response.Metadata["enrollmentId"]);
                var enrollment = await _enrollmentService.GetEnrollmentById(enrollmentId);
                if (enrollment == null)
                {
                    _logger.LogWarning("Enrollment {EnrollmentId} not found for webhook", enrollmentId);
                    return new Response<string> { Succeeded = false, Message = "Enrollment not found" };
                }

                var payment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.EnrollmentId == enrollmentId && p.TransactionId == response.PaymentIntentId);
                if (payment == null)
                {
                    payment = new Payment
                    {
                        EnrollmentId = enrollmentId,
                        Amount = enrollment.Fee,
                        PaymentMethod = "Stripe",
                        TransactionId = response.PaymentIntentId,
                        Status = response.Status,
                        CreatedAt = DateTime.UtcNow,
                        OrderId = "",
                        VnPayResponseCode = ""
                    };
                    _context.Payments.Add(payment);
                }
                else
                {
                    payment.Status = response.Status;
                }

                enrollment.Status = "completed";
                await _context.SaveChangesAsync();

                return new Response<string> { Succeeded = true, Message = "Webhook processed successfully" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Stripe webhook");
                return new Response<string> { Succeeded = false, Message = "Webhook processing failed" };
            }
        }

        public async Task<Response<string>> ConfirmPaymentAsync(int userId, string sessionId)
        {
            try
            {
                var session = await _stripeRepository.GetCheckoutSessionAsync(sessionId);
                if (session == null)
                    return new Response<string> { Succeeded = false, Message = "Invalid session ID" };

                if (session.PaymentStatus != "paid" || session.Status != "complete")
                {
                    _logger.LogWarning("Payment not completed for session {SessionId}", sessionId);
                    return new Response<string> { Succeeded = false, Message = "Payment not completed" };
                }

                if (!session.Metadata.ContainsKey("enrollmentId"))
                {
                    _logger.LogError("Missing enrollmentId in session metadata for session {SessionId}", sessionId);
                    return new Response<string> { Succeeded = false, Message = "Missing enrollmentId in metadata" };
                }

                var enrollmentId = int.Parse(session.Metadata["enrollmentId"]);
                var enrollment = await _context.Enrollments
                    .Include(e => e.Student)
                    .Include(e => e.Course)
                    .FirstOrDefaultAsync(e => e.Id == enrollmentId);

                if (enrollment == null)
                {
                    _logger.LogWarning("Enrollment {EnrollmentId} not found for session {SessionId}", enrollmentId, sessionId);
                    return new Response<string> { Succeeded = false, Message = "Enrollment not found" };
                }

                if (enrollment.Student.UserId != userId)
                    return new Response<string> { Succeeded = false, Message = "You are not authorized to confirm this payment" };

                var payment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.EnrollmentId == enrollmentId && p.TransactionId == sessionId);
                if (payment == null)
                {
                    payment = new Payment
                    {
                        EnrollmentId = enrollmentId,
                        Amount = enrollment.Course.Fee,
                        PaymentMethod = "Stripe",
                        TransactionId = session.PaymentIntentId,
                        Status = "paid",
                        CreatedAt = DateTime.UtcNow,
                        OrderId = "",
                        VnPayResponseCode = ""
                    };
                    _context.Payments.Add(payment);
                }
                else
                {
                    payment.Status = "paid";
                }

                if (enrollment.Status != "completed")
                {
                    _logger.LogInformation("Updating enrollment {EnrollmentId} status to 'completed'", enrollmentId);
                    enrollment.Status = "completed";
                    await _context.SaveChangesAsync();
                }
                else
                {
                    _logger.LogInformation("Enrollment {EnrollmentId} is already completed, skipping update", enrollmentId);
                }

                return new Response<string> { Succeeded = true, Message = "Payment confirmed successfully" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming payment for session {SessionId}", sessionId);
                return new Response<string> { Succeeded = false, Message = "An error occurred while confirming payment" };
            }
        }

        public async Task<IActionResult> ProcessPaymentCallbackAsync(IQueryCollection query, bool? isBack = false)
        {
            try
            {
                if (isBack == true)
                {
                    _logger.LogInformation("Back button pressed in payment process");
                    return new RedirectResult(BuildRedirectUrl(false, null, "Payment cancelled", null, null, "vnpay"));
                }

                var response = _vnPayRepository.PaymentExecute(query);
                if (!response.Success || response.VnPayResponseCode != "00")
                {
                    var payments = await _context.Payments
                        .FirstOrDefaultAsync(p => p.OrderId == response.OrderId.ToString());
                    if (payments != null)
                    {
                        payments.Status = "failed";
                        payments.VnPayResponseCode = response.VnPayResponseCode;
                        await _context.SaveChangesAsync();
                    }

                    return new RedirectResult(BuildRedirectUrl(false, null, "Payment failed", response.VnPayResponseCode, null, "vnpay"));
                }

                var enrollmentId = response.OrderId; 
                if (enrollmentId > int.MaxValue)
                {
                    _logger.LogError("Enrollment ID {EnrollmentId} exceeds int.MaxValue", enrollmentId);
                    return new RedirectResult(BuildRedirectUrl(false, null, "Invalid enrollment ID", null, null, "vnpay"));
                }

                var enrollment = await _context.Enrollments
                    .Include(e => e.Course)
                    .FirstOrDefaultAsync(e => e.Id == (int)enrollmentId); 

                if (enrollment == null)
                    return new RedirectResult(BuildRedirectUrl(false, null, "Enrollment not found", null, null, "vnpay"));

                var payment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.EnrollmentId == (int)enrollmentId && p.OrderId == response.OrderId.ToString());
                if (payment == null)
                {
                    payment = new Payment
                    {
                        EnrollmentId = (int)enrollmentId, 
                        Amount = (decimal)enrollment.Course.Fee,
                        PaymentMethod = "VNPay",
                        TransactionId = response.TransactionId,
                        OrderId = response.OrderId.ToString(),
                        Status = "paid",
                        CreatedAt = DateTime.Now,
                        VnPayResponseCode = response.VnPayResponseCode
                    };
                    _context.Payments.Add(payment);
                }
                else
                {
                    payment.Status = "paid";
                    payment.TransactionId = response.TransactionId;
                    payment.VnPayResponseCode = response.VnPayResponseCode;
                }

                enrollment.Status = "completed";
                _context.Enrollments.Update(enrollment);

                await _context.SaveChangesAsync();
                return new RedirectResult(BuildRedirectUrl(true, enrollment.Id, null, null, response.TransactionId, "vnpay"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in payment callback");
                return new RedirectResult(BuildRedirectUrl(false, null, "Payment processing failed", null, null, "vnpay"));
            }
        }

        public async Task<PagedResponse<List<BillHistoryDTO>>> GetBillHistoryAsync(PaginationFilter filter, string route, int userId)
        {
            try
            {
                var billHistory = await _vnPayRepository.GetBillHistory(filter, route, userId);
                return billHistory;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching bill history for user {UserId}", userId);
                return new PagedResponse<List<BillHistoryDTO>>(null, filter.PageNumber, filter.PageSize)
                {
                    Succeeded = false,
                    Message = "An error occurred while fetching bill history"
                };
            }
        }

        private string BuildRedirectUrl(
            bool isSuccess,
            long? enrollmentId = null,
            string errorMessage = null,
            string errorCode = null,
            string sessionId = null,
            string paymentMethod = null)
        {
            var parameters = new Dictionary<string, string>
            {
                { SUCCESS_PARAM, isSuccess.ToString().ToLower() }
            };

            if (enrollmentId.HasValue)
                parameters.Add(ENROLLMENT_ID_PARAM, enrollmentId.ToString());

            if (!string.IsNullOrEmpty(sessionId))
                parameters.Add(SESSION_ID_PARAM, sessionId);

            if (!string.IsNullOrEmpty(errorMessage))
                parameters.Add(ERROR_MESSAGE_PARAM, errorMessage);

            if (!string.IsNullOrEmpty(errorCode))
                parameters.Add(ERROR_CODE_PARAM, errorCode);

            if (!string.IsNullOrEmpty(paymentMethod))
                parameters.Add("paymentMethod", paymentMethod);

            var queryString = string.Join("&", parameters.Select(p => $"{p.Key}={p.Value}"));
            return $"{COURSES_REDIRECT_BASE_URL}?{queryString}";
        }
    }
}