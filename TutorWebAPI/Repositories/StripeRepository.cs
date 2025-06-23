using Stripe;
using Stripe.Checkout;
using TutorWebAPI.Data;
using TutorWebAPI.Filter;
using TutorWebAPI.Models.Entities;
using TutorWebAPI.Wrapper;
using Microsoft.EntityFrameworkCore;
using TutorWebAPI.Models.DTOs;
using TutorWebAPI.DTOs;
using Microsoft.Extensions.Options;

namespace TutorWebAPI.Repositories
{
    public class StripeRepository : IStripeRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly string _webhookSecret;
        private readonly ILogger<StripeRepository> _logger;

        public StripeRepository(
         ApplicationDbContext context,
         IOptions<StripeModel> stripeOptions,
         ILogger<StripeRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var stripeConfig = stripeOptions.Value;
            _webhookSecret = stripeConfig.WebhookSecret
                ?? throw new ArgumentNullException("Stripe:WebhookSecret is missing in configuration");
            StripeConfiguration.ApiKey = stripeConfig.SecretKey
                ?? throw new ArgumentNullException("Stripe:SecretKey is missing in configuration");
        }

        public async Task<Session> GetCheckoutSessionAsync(string sessionId)
        {
            try
            {
                var service = new SessionService();
                return await service.GetAsync(sessionId);
            }
            catch (StripeException ex)
            {
                // Log the error and return null if the session cannot be retrieved
                Console.WriteLine($"Error retrieving Stripe session: {ex.Message}");
                return null;
            }
        }

        public async Task<string> CreateCheckoutSessionAsync(EnrollmentDTO enrollment, string successUrl, string cancelUrl)
        {
            _logger.LogInformation("Creating Stripe checkout session for enrollment {EnrollmentId}", enrollment.Id);
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = (long)(enrollment.Fee * 100),
                            Currency = "usd", 
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = enrollment.CourseName,
                                Description = $"Course enrollment for {enrollment.CourseName}"
                            }
                        },
                        Quantity = 1
                    }
                },
                Metadata = new Dictionary<string, string>
                {
                    { "enrollmentId", enrollment.Id.ToString() },
                    { "userId", enrollment.UserId.ToString() }
                },
                Mode = "payment",
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options);
            _logger.LogInformation("Created Stripe session {SessionId} for enrollment {EnrollmentId}", session.Id, enrollment.Id);
            return session.Url;
        }

        public async Task<StripePaymentResponse> ProcessWebhookEventAsync(string json, string signature)
        {
            try
            {
                _logger.LogInformation("Processing Stripe webhook: {Json}", json);
                var stripeEvent = EventUtility.ConstructEvent(json, signature, _webhookSecret);

                string checkoutSessionCompleted = "checkout.session.completed"; 
                if (stripeEvent.Type == checkoutSessionCompleted) 
                {
                    var session = stripeEvent.Data.Object as Session;
                    if (session == null)
                    {
                        _logger.LogWarning("Invalid session data in webhook");
                        return new StripePaymentResponse
                        {
                            Success = false,
                            ErrorMessage = "Invalid session data"
                        };
                    }

                    _logger.LogInformation("Processed checkout.session.completed for session {SessionId}", session.Id);
                    return new StripePaymentResponse
                    {
                        Success = true,
                        SessionId = session.Id,
                        PaymentIntentId = session.PaymentIntentId,
                        Status = session.PaymentStatus
                    };
                }

                _logger.LogWarning("Unhandled Stripe event type: {EventType}", stripeEvent.Type);
                return new StripePaymentResponse
                {
                    Success = false,
                    ErrorMessage = $"Unhandled event type: {stripeEvent.Type}"
                };
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe error processing webhook");
                return new StripePaymentResponse
                {
                    Success = false,
                    ErrorMessage = $"Stripe error: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error processing webhook");
                return new StripePaymentResponse
                {
                    Success = false,
                    ErrorMessage = $"Unexpected error: {ex.Message}"
                };
            }
        }

        public async Task<PagedResponse<List<BillHistoryDTO>>> GetBillHistoryAsync(PaginationFilter filter, string route, int userId)
        {
            _logger.LogInformation("Fetching bill history for user {UserId}", userId);
            var validFilter = new PaginationFilter(filter.PageNumber, filter.PageSize);
            var query = _context.Payments
                .Where(p => p.Enrollment.Student.UserId == userId && p.PaymentMethod == "Stripe")
                .Include(p => p.Enrollment)
                .ThenInclude(e => e.Course)
                .Select(p => new BillHistoryDTO
                {
                    PaymentId = p.Id,
                    CourseName = p.Enrollment.Course.CourseName,
                    Amount = p.Amount,
                    CreatedAt = p.CreatedAt,
                    Status = p.Status,
                    TransactionId = p.TransactionId,
                    PaymentMethod = p.PaymentMethod
                });

            var totalRecords = await query.CountAsync();
            var pagedData = await query
                .Skip((validFilter.PageNumber - 1) * validFilter.PageSize)
                .Take(validFilter.PageSize)
                .ToListAsync();

            return new PagedResponse<List<BillHistoryDTO>>(
                pagedData,
                validFilter.PageNumber,
                validFilter.PageSize)
            {
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)validFilter.PageSize),
                Succeeded = true
            };
        }
    }
}