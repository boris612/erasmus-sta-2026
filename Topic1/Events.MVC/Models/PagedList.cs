using Microsoft.EntityFrameworkCore;

namespace Events.MVC.Models;

public class PagedList<T>(List<T> data, PagingInfo pagingInfo)
{
    public List<T> Data { get; init; } = data;

    public PagingInfo PagingInfo { get; init; } = pagingInfo;

    public static PagedList<T> Create(IQueryable<T> source, PagingInfo pagingInfo)
    {
        var items = source
            .Skip((pagingInfo.CurrentPage - 1) * pagingInfo.ItemsPerPage)
            .Take(pagingInfo.ItemsPerPage)
            .ToList();

        return new PagedList<T>(items, pagingInfo);
    }

    public static async Task<PagedList<T>> CreateAsync(IQueryable<T> source, PagingInfo pagingInfo, CancellationToken cancellationToken = default)
    {
        var items = await source
            .Skip((pagingInfo.CurrentPage - 1) * pagingInfo.ItemsPerPage)
            .Take(pagingInfo.ItemsPerPage)
            .ToListAsync(cancellationToken);

        return new PagedList<T>(items, pagingInfo);
    }
}
