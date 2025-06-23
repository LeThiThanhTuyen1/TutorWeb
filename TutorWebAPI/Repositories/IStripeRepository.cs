using TutorWebAPI.Filter;
using TutorWebAPI.DTOs;
using TutorWebAPI.Models.Entities;
using TutorWebAPI.Wrapper;
using TutorWebAPI.Models.DTOs;

namespace TutorWebAPI.Repositories
{
    public interface IStripeRepository
    {
        Task<string> CreateCheckoutSessionAsync(EnrollmentDTO enrollment, string successUrl, string cancelUrl);
        Task<StripePaymentResponse> ProcessWebhookEventAsync(string json, string signature);
        Task<PagedResponse<List<BillHistoryDTO>>> GetBillHistoryAsync(PaginationFilter filter, string route, int userId);
        Task<Stripe.Checkout.Session> GetCheckoutSessionAsync(string sessionId);
    }
}
