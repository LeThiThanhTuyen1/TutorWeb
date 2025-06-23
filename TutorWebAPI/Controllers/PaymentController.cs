using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TutorWebAPI.Data;
using TutorWebAPI.Filter;
using TutorWebAPI.Models;
using TutorWebAPI.Models.DTOs;
using TutorWebAPI.Repositories;
using TutorWebAPI.Services;
using TutorWebAPI.Wrapper;
using System.Security.Claims;

namespace TutorWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly IEnrollmentService _enrollmentService;
        private readonly ILogger<PaymentController> _logger;
        private const string COURSES_REDIRECT_BASE_URL = "http://localhost:3000/student/courses";
        private const string SUCCESS_PARAM = "success";
        private const string ERROR_MESSAGE_PARAM = "message";
        private const string ENROLLMENT_ID_PARAM = "enrollmentId";
        private const string SESSION_ID_PARAM = "sessionId";

        public PaymentController(
            IPaymentService paymentService,
            IEnrollmentService enrollmentService,
            ILogger<PaymentController> logger)
        {
            _paymentService = paymentService ?? throw new ArgumentNullException(nameof(paymentService));
            _enrollmentService = enrollmentService ?? throw new ArgumentNullException(nameof(enrollmentService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpPost("enroll-and-pay")]
        public async Task<IActionResult> EnrollAndPay([FromBody] EnrollAndPayRequest request)
        {
            try
            {
                int userId = GetUserId();
                if (userId == 0)
                    return Unauthorized(new Response<string> { Message = "Invalid user authentication" });

                var enrollment = await _enrollmentService.GetEnrollmentById(request.EnrollmentId);
                if (enrollment == null)
                    return NotFound(new Response<string> { Message = "Enrollment not found" });
                if (enrollment.UserId != userId)
                    return Forbid("You are not authorized to pay for this enrollment");

                var successUrl = request.PaymentMethod.ToLower() == "stripe"
                    ? $"{COURSES_REDIRECT_BASE_URL}?{SUCCESS_PARAM}=true&{ENROLLMENT_ID_PARAM}={request.EnrollmentId}&{SESSION_ID_PARAM}={{CHECKOUT_SESSION_ID}}"
                    : $"{COURSES_REDIRECT_BASE_URL}?{SUCCESS_PARAM}=true&{ENROLLMENT_ID_PARAM}={request.EnrollmentId}&{SESSION_ID_PARAM}={{TRANSACTION_ID}}";
                var cancelUrl = $"{COURSES_REDIRECT_BASE_URL}?{SUCCESS_PARAM}=false&{ERROR_MESSAGE_PARAM}=cancelled";

                string paymentUrl;
                if (request.PaymentMethod.ToLower() == "stripe")
                {
                    paymentUrl = await _paymentService.CreateCheckoutSessionAsync(enrollment, successUrl, cancelUrl);
                }
                else if (request.PaymentMethod.ToLower() == "vnpay")
                {
                    paymentUrl = await _paymentService.CreateVNPayPaymentUrlAsync(enrollment, successUrl, cancelUrl);
                }
                else
                {
                    return BadRequest(new Response<string> { Message = "Invalid payment method. Use 'stripe' or 'vnpay'." });
                }

                return Ok(new { PaymentUrl = paymentUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment for enrollment {EnrollmentId} with method {PaymentMethod}", request.EnrollmentId, request.PaymentMethod);
                return StatusCode(500, new Response<string> { Message = "An error occurred while processing payment" });
            }
        }

        public class EnrollAndPayRequest
        {
            public int EnrollmentId { get; set; }
            public string PaymentMethod { get; set; } 
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            var signature = Request.Headers["Stripe-Signature"];

            var response = await _paymentService.ProcessWebhookEventAsync(json, signature);
            return response.Succeeded
                ? Ok(response)
                : BadRequest(response);
        }

        [HttpGet("PaymentCallback")]
        [AllowAnonymous]
        public async Task<IActionResult> PaymentCallback([FromQuery] bool? isBack = false)
        {
            return await _paymentService.ProcessPaymentCallbackAsync(Request.Query, isBack);
        }

        [HttpGet("billhistory")]
        public async Task<ActionResult<PagedResponse<List<BillHistoryDTO>>>> GetBillHistory([FromQuery] PaginationFilter filter)
        {
            int userId = GetUserId();
            if (userId == 0)
            {
                return Unauthorized(new PagedResponse<List<BillHistoryDTO>>(
                    null, filter.PageNumber, filter.PageSize)
                {
                    Succeeded = false,
                    Message = "Invalid user authentication"
                });
            }

            var route = Request.Path.Value;
            var billHistory = await _paymentService.GetBillHistoryAsync(filter, route, userId);
            return Ok(billHistory);
        }

        [HttpPost("confirm-payment")]
        public async Task<IActionResult> ConfirmPayment([FromBody] ConfirmPaymentRequest request)
        {
            int userId = GetUserId();
            if (userId == 0)
                return Unauthorized(new Response<string> { Message = "Invalid user authentication" });

            var response = await _paymentService.ConfirmPaymentAsync(userId, request.SessionId);
            return response.Succeeded
                ? Ok(response)
                : BadRequest(response);
        }

        public class ConfirmPaymentRequest
        {
            public string SessionId { get; set; }
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        }
    }
}