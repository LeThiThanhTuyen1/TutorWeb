using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TutorWebAPI.DTOs;
using TutorWebAPI.Filter;
using TutorWebAPI.Models;
using TutorWebAPI.Models.DTOs;
using TutorWebAPI.Wrapper;

namespace TutorWebAPI.Services
{
    public interface IPaymentService
    {
        Task<string> CreateCheckoutSessionAsync(EnrollmentDTO enrollment, string successUrl, string cancelUrl);
        Task<string> CreateVNPayPaymentUrlAsync(EnrollmentDTO enrollment, string successUrl, string cancelUrl);
        Task<Response<string>> ProcessWebhookEventAsync(string json, string signature);
        Task<IActionResult> ProcessPaymentCallbackAsync(IQueryCollection query, bool? isBack);
        Task<PagedResponse<List<BillHistoryDTO>>> GetBillHistoryAsync(PaginationFilter filter, string route, int userId);
        Task<Response<string>> ConfirmPaymentAsync(int userId, string sessionId);
    }
}