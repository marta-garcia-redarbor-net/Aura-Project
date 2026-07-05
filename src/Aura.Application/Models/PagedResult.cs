namespace Aura.Application.Models;

/// <summary>
/// Generic paginated result wrapper for API responses.
/// </summary>
/// <typeparam name="T">The type of items in the result set.</typeparam>
public sealed record PagedResult<T>
{
    /// <summary>
    /// The items for the current page.
    /// </summary>
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();

    /// <summary>
    /// Total number of items across all pages.
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// Current page number (1-based).
    /// </summary>
    public int Page { get; init; }

    /// <summary>
    /// Number of items per page.
    /// </summary>
    public int PageSize { get; init; }

    /// <summary>
    /// Total number of pages.
    /// </summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
}
