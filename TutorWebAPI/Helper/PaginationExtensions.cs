using Microsoft.EntityFrameworkCore;
using TutorWebAPI.Filter;
using TutorWebAPI.Wrapper;
using TutorWebAPI.Repositories;

namespace TutorWebAPI.Helper
{
    public static class PaginationExtensions
    {
        public static async Task<PagedResponse<List<T>>> ToPagedResponseAsync<T>(
            this IQueryable<T> query, PaginationFilter filter, IUriRepository uriRepo, string route)
        {
            int totalRecords = await query.CountAsync();
            int totalPages = (int)Math.Ceiling((double)totalRecords / filter.PageSize);

            var pagedData = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return new PagedResponse<List<T>>(pagedData, filter.PageNumber, filter.PageSize)
            {
                TotalPages = totalPages,
                TotalRecords = totalRecords,
                FirstPage = uriRepo.GetPageUri(new PaginationFilter(1, filter.PageSize), route),
                LastPage = uriRepo.GetPageUri(new PaginationFilter(totalPages, filter.PageSize), route),
                NextPage = filter.PageNumber < totalPages
                    ? uriRepo.GetPageUri(new PaginationFilter(filter.PageNumber + 1, filter.PageSize), route)
                    : null,
                PreviousPage = filter.PageNumber > 1
                    ? uriRepo.GetPageUri(new PaginationFilter(filter.PageNumber - 1, filter.PageSize), route)
                    : null
            };
        }
    }
}
