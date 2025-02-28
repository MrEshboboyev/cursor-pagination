using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CursorPagination.Models.Pagination;

public class CursorPagedRequest
{
    private int _limit = 50;

    /// <summary>
    /// The maximum number of items to return (1-100)
    /// </summary>
    [Range(1, 100)]
    public int Limit
    {
        get => _limit;
        set => _limit = Math.Clamp(value, 1, 100);
    }

    /// <summary>
    /// The cursor value for pagination (encoded string)
    /// </summary>
    public string? Cursor { get; set; }

    /// <summary>
    /// Optional filter by user ID
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Optional search term for note title or content
    /// </summary>
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Optional date range filter - start date
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Optional date range filter - end date
    /// </summary>
    public DateTime? EndDate { get; set; }
}
