namespace Inventory.Application.Common.Models;

public sealed class GridResponse<T>
{
    public int TotalCount { get; set; }
    public List<T> Items { get; set; } = new();

    public GridResponse(List<T> items, int totalCount)
    {
        Items = items;
        TotalCount = totalCount;
    }
}
