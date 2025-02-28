using System.Text.Json;
using System.Text;

namespace CursorPagination.Models.Pagination;

public class Cursor
{
    public DateTime CreatedAt { get; set; }
    public Guid Id { get; set; }

    // Convert cursor to string
    public string Encode()
    {
        var json = JsonSerializer.Serialize(this);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    }

    // Try to decode cursor from string
    public static bool TryDecode(string? encodedCursor, out Cursor? result)
    {
        result = null;

        if (string.IsNullOrEmpty(encodedCursor))
            return false;

        try
        {
            var bytes = Convert.FromBase64String(encodedCursor);
            var json = Encoding.UTF8.GetString(bytes);
            result = JsonSerializer.Deserialize<Cursor>(json);
            return result != null;
        }
        catch
        {
            return false;
        }
    }
}
