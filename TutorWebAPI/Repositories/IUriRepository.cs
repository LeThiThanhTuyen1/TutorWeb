using TutorWebAPI.Filter;

namespace TutorWebAPI.Repositories
{
    public interface IUriRepository
    {
        public Uri GetPageUri(PaginationFilter filter, string route);
    }
}
