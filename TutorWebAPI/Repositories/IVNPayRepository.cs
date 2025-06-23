using TutorWebAPI.Filter;
using TutorWebAPI.Models.DTOs;
using TutorWebAPI.Wrapper;

namespace TutorWebAPI.Repositories
{
    public interface IVNPayRepository
    {
        string CreatePaymentUrl(HttpContext context, VnPaymentRequestModel response);
        VnPaymentResponseModel PaymentExecute(IQueryCollection collection);
        Task<PagedResponse<List<BillHistoryDTO>>> GetBillHistory(PaginationFilter filter, string route, int userId);
    }
}