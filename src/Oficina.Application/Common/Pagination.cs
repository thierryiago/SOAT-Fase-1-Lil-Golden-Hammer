namespace Oficina.Application.Common;

public sealed record PageRequest(
    string? Search = null,
    int Page = 1,
    int PageSize = 20);

public sealed record PagedResponse<T>(
    IReadOnlyCollection<T> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages);

public static class Pagination
{
    public static PagedResponse<T> Create<T>(
        IEnumerable<T> source,
        PageRequest request)
    {
        if (request.Page < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(request.Page), "Page must be greater than zero.");
        }

        if (request.PageSize is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request.PageSize),
                "Page size must be between 1 and 100.");
        }

        var items = source.ToList();
        var pagedItems = items
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();
        var totalPages = items.Count == 0
            ? 0
            : (int)Math.Ceiling(items.Count / (double)request.PageSize);

        return new PagedResponse<T>(
            pagedItems,
            request.Page,
            request.PageSize,
            items.Count,
            totalPages);
    }
}
