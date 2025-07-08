namespace CursorPagination.Milan.Models;

public class UserNote
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string? Note { get; set; }
    public DateOnly Date { get; set; }
}
