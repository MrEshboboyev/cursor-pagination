using System.Text.Json.Serialization;

namespace CursorPagination.Models.DTOs;

public class UserNoteDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CreatedTimeAgo { get; set; }

    public static UserNoteDto FromModel(UserNote note, string username)
    {
        return new UserNoteDto
        {
            Id = note.Id,
            UserId = note.UserId,
            UserName = username,
            Title = note.Title,
            Content = note.Content,
            CreatedAt = note.CreatedAt,
            UpdatedAt = note.UpdatedAt,
            CreatedTimeAgo = GetTimeAgo(note.CreatedAt)
        };
    }

    private static string GetTimeAgo(DateTime dateTime)
    {
        var span = DateTime.UtcNow - dateTime;

        if (span.TotalDays > 365)
        {
            int years = (int)(span.TotalDays / 365);
            return $"{years} {(years == 1 ? "year" : "years")} ago";
        }
        if (span.TotalDays > 30)
        {
            int months = (int)(span.TotalDays / 30);
            return $"{months} {(months == 1 ? "month" : "months")} ago";
        }
        if (span.TotalDays > 1)
        {
            return $"{(int)span.TotalDays} {((int)span.TotalDays == 1 ? "day" : "days")} ago";
        }
        if (span.TotalHours > 1)
        {
            return $"{(int)span.TotalHours} {((int)span.TotalHours == 1 ? "hour" : "hours")} ago";
        }
        if (span.TotalMinutes > 1)
        {
            return $"{(int)span.TotalMinutes} {((int)span.TotalMinutes == 1 ? "minute" : "minutes")} ago";
        }

        return "just now";
    }
}
