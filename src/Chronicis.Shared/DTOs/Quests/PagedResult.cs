namespace Chronicis.Shared.DTOs.Quests;

/// <summary>
/// Generic paginated result container.
/// </summary>
public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Skip { get; set; }
    public int Take { get; set; }
}
