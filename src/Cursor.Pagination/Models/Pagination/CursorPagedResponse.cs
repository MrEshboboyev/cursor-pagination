using System.Text.Json.Serialization;

namespace CursorPagination.Models.Pagination;

public class CursorPagedResponse<T>
{
    /// <summary>
    /// The list of items for the current page
    /// </summary>
    public List<T> Items { get; set; } = [];

    /// <summary>
    /// The cursor to use for the next page (null if no more pages)
    /// </summary>
    public string? NextCursor { get; set; }

    /// <summary>
    /// Whether there are more items available
    /// </summary>
    public bool HasMore => !string.IsNullOrEmpty(NextCursor);

    /// <summary>
    /// The total count of items (if requested)
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? TotalCount { get; set; }

    /// <summary>
    /// Any additional metadata that might be useful
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object>? Metadata { get; set; }
}