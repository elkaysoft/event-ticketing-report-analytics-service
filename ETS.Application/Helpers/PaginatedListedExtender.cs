using Microsoft.EntityFrameworkCore;

namespace ETS.Application.Helpers
{
    public class PaginatedListedExtender<T> : PaginatedList<T>
    {
        public PaginatedListedExtender(List<T> items, int count, int pageIndex, int pageSize) : base(items, count, pageIndex, pageSize)
        {
        }

        public static async Task<PaginatedList<T>> CreateAsync(IQueryable<T> source, int pageIndex, int pageSize,
            CancellationToken cancellationToken = default)
        {
            var count = await source.CountAsync(cancellationToken);
            var items = await source.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

            return new PaginatedList<T>(items, count, pageIndex, pageSize);
        }
    }
}
