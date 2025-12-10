using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Paging;
public static class PagedExtensions
{
    public static async Task<List<T>> ToPagedAsync<T>(this IQueryable<T> source, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await source.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
    }
}